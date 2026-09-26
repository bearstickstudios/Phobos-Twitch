using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwitchSDK;
using TwitchSDK.Interop;
using UnityEngine;

namespace PhobosTwitch
{
    public class TwitchRedemptionManager : MonoBehaviour
    {
        [Header("Dependencies")] [SerializeField]
        private TwitchAuthManager authManager;

        [SerializeField] private ChatAvatarManager avatarManager;
        [SerializeField] private Transform vipRock;
        [SerializeField] private Animator fightAnimator;

        [Header("Actions Configuration")] [SerializeField]
        private List<RedemptionActionSO> availableRedemptions;

        private Dictionary<string, RedemptionActionSO> redemptionLookup;
        private EventStream<CustomRewardEvent> _rewardStream;
        private CancellationTokenSource _cts;

        private void Awake()
        {
            redemptionLookup = new Dictionary<string, RedemptionActionSO>();
            foreach (var action in availableRedemptions)
            {
                if (action != null && !string.IsNullOrEmpty(action.rewardTitle))
                {
                    redemptionLookup[action.rewardTitle] = action;
                }
            }
        }

        private async void Start()
        {
            _cts = new CancellationTokenSource();

            try
            {
                if (authManager != null)
                {
                    await authManager.WaitForAuthenticationAsync(_cts.Token);
                }

                _rewardStream = await Twitch.API.SubscribeToCustomRewardEvents();
                Debug.Log("Twitch Plugin: Subscribed to Custom Reward Events.");

                await ListenForRedemptionsAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                Debug.LogError($"Twitch redemption listener failed: {ex}");
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _rewardStream?.Dispose();
        }

        private async Task ListenForRedemptionsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                CustomRewardEvent rewardEvent = await _rewardStream.WaitForEvent();
                Debug.Log(
                    $"[Twitch] {rewardEvent.RedeemerName} redeemed {rewardEvent.CustomRewardTitle} for {rewardEvent.CustomRewardCost}!");
                HandleRedemption(rewardEvent);
            }
        }

        private void HandleRedemption(CustomRewardEvent e)
        {
            if (!redemptionLookup.TryGetValue(e.CustomRewardTitle, out var action)) return;

            var context = new RedemptionContext
            {
                Username = e.RedeemerName,
                AvatarManager = avatarManager,
                VipRock = vipRock,
                FightAnimator = fightAnimator
            };

            // Fire-and-forget: a long-running action (e.g. the VIP fight sequence)
            // must not block the next redemption from being picked up.
            _ = RunActionSafely(action, context);
        }

        private async Task RunActionSafely(RedemptionActionSO action, RedemptionContext context)
        {
            try
            {
                await action.Execute(context);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Redemption action '{action.rewardTitle}' threw: {ex}");
            }
        }
    }
}
