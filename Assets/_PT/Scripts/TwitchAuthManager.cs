using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using TwitchSDK;
using TwitchSDK.Interop;

public class TwitchAuthManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject loginCanvas;
    [SerializeField] private TextMeshProUGUI userCodeText;
    [SerializeField] private TextMeshProUGUI authUrlText;
    
    [Header("Broadcaster Data")]
    public string BroadcasterId { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public event Action<string> OnStreamTitleFetched;

    private async Task InitiateDeviceAuthAsync()
    {
        loginCanvas.SetActive(true);

        // Request device flow authorization through Twitch.Unity
        var deviceFlow = await TwitchAuth.StartDeviceAuthFlowAsync(new[]
        {
            "chat:read",
            "chat:edit",
            "channel:read:redemptions",
            "channel:manage:redemptions",
            "channel:read:goals"
        });

        userCodeText.text = deviceFlow.UserCode;
        authUrlText.text = deviceFlow.VerificationUri;

        // Wait for broadcaster to approve on browser/phone
        var tokenInfo = await deviceFlow.WaitForAuthorizationAsync();
        
        if (tokenInfo != null)
        {
            IsAuthenticated = true;
            BroadcasterId = tokenInfo.UserId;
            loginCanvas.SetActive(false);

            await FetchStreamInformationAsync();
        }
    }

    public async Task FetchStreamInformationAsync()
    {
        var channelInfo = await TwitchAPI.Helix.Channels.GetChannelInformationAsync(BroadcasterId);
        if (channelInfo != null)
        {
            OnStreamTitleFetched?.Invoke(channelInfo.Title);
        }
    }
    

    private void Start()
    {
        await InitiateDeviceAuthAsync();
        
        // Initialize login flow through the official plugin
        Twitch.API.GetAuthenticationInfo(new TwitchOAuthScope(TwitchOAuthScope.Channel.ManageRedemptions.Scope));
    }

    private void Update()
    {
        // Continuously poll the auth state to ensure connection validity
        var authStateTask = Twitch.API?.GetAuthState();

        if (authStateTask == null || !authStateTask.IsCompleted) return;
        
        var authStatus = authStateTask.MaybeResult;
        if (authStatus == null) return;
        
        switch (authStatus.Status)
        {
            case AuthStatus.LoggedIn:
            {
                if (!IsAuthenticated)
                {
                    Debug.Log("Twitch Plugin: Successfully logged in!");
                    IsAuthenticated = true;
                }

                break;
            }
            case AuthStatus.LoggedOut:
            {
                if (IsAuthenticated)
                {
                    Debug.LogWarning("Twitch Plugin: Logged out.");
                    IsAuthenticated = false;
                }

                break;
            }
        }
    }
}