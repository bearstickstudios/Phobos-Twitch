using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using TwitchSDK;
using TwitchSDK.Interop;
using UnityEngine;

/// <summary>
/// Handles Twitch SDK authentication and exposes the logged-in user's stream title.
///
/// Rewritten against the actual official Unity plugin surface
/// (dev.twitch.tv/docs/game-engine-plugins/unity-reference):
///
///  - There is no TwitchAuth.StartDeviceAuthFlowAsync / WaitForAuthorizationAsync.
///    Auth is driven by polling Twitch.API.GetAuthState() and, while the status is
///    AuthStatus.WaitingForCode, reading Twitch.API.GetAuthenticationInfo(scope)
///    for a UserCode/Uri to show the broadcaster.
///  - There is no TwitchAPI.Helix.Channels.GetChannelInformationAsync(...).
///    The logged-in user's stream title comes from Twitch.API.GetMyStreamInfo().
///  - "chat:read" / "chat:edit" / "channel:read:goals" are not scopes this SDK
///    exposes (chat isn't handled by this SDK at all - see TwitchChatAdapter.cs).
///    Only the scope actually needed here (manage:redemptions) is requested.
/// </summary>
public class TwitchAuthManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject loginCanvas;
    [SerializeField] private TextMeshProUGUI userCodeText;
    [SerializeField] private TextMeshProUGUI authUrlText;

    [Header("Polling")]
    [SerializeField] private int authPollIntervalMs = 500;

    public bool IsAuthenticated { get; private set; }

    public event Action<string> OnStreamTitleFetched;

    // Only request what we actually use. Extend this if other SDK features
    // (polls, predictions, clips, hype train...) get added later.
    private static readonly TwitchOAuthScope RequiredScope =
        new TwitchOAuthScope(TwitchOAuthScope.Channel.ManageRedemptions.Scope);

    private CancellationTokenSource _cts;

    private async void Start()
    {
        _cts = new CancellationTokenSource();

        try
        {
            await RunAuthLoopAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown, nothing to do.
        }
        catch (Exception ex)
        {
            Debug.LogError($"Twitch auth loop failed: {ex}");
        }
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private async Task RunAuthLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            AuthState state = await Twitch.API.GetAuthState();

            switch (state.Status)
            {
                case AuthStatus.LoggedIn:
                    if (!IsAuthenticated)
                    {
                        IsAuthenticated = true;
                        if (loginCanvas != null) loginCanvas.SetActive(false);
                        Debug.Log("Twitch Plugin: Successfully logged in!");
                        await FetchStreamInformationAsync();
                    }
                    break;

                case AuthStatus.LoggedOut:
                    if (IsAuthenticated)
                    {
                        Debug.LogWarning("Twitch Plugin: Logged out.");
                    }
                    IsAuthenticated = false;
                    // Kick off (or re-kick off) the login flow. Per the docs this is a
                    // no-op if a login is already in progress.
                    Twitch.API.GetAuthenticationInfo(RequiredScope);
                    break;

                case AuthStatus.WaitingForCode:
                    IsAuthenticated = false;
                    var authInfo = Twitch.API.GetAuthenticationInfo(RequiredScope).MaybeResult;
                    if (authInfo != null)
                    {
                        if (loginCanvas != null) loginCanvas.SetActive(true);
                        if (userCodeText != null) userCodeText.text = authInfo.UserCode;
                        if (authUrlText != null) authUrlText.text = authInfo.Uri;
                    }
                    break;

                case AuthStatus.Loading:
                default:
                    break;
            }

            await Task.Delay(authPollIntervalMs, token);
        }
    }

    /// <summary>
    /// Lets other scripts (e.g. TwitchRedemptionManager) await until login completes
    /// instead of re-implementing their own "wait for IsAuthenticated" polling loop.
    /// </summary>
    public async Task WaitForAuthenticationAsync(CancellationToken token = default)
    {
        while (!IsAuthenticated)
        {
            token.ThrowIfCancellationRequested();
            await Task.Delay(200, token);
        }
    }

    public async Task FetchStreamInformationAsync()
    {
        StreamInfo info = await Twitch.API.GetMyStreamInfo();
        if (info != null)
        {
            OnStreamTitleFetched?.Invoke(info.Title);
        }
    }
}
