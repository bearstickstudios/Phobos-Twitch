using System.Collections.Generic;
using TwitchSDK;
using TwitchSDK.Interop;
using UnityEngine;

public class TwitchRedemptionManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TwitchAuthManager authManager;
    [SerializeField] private ChatAvatarManager avatarManager;
    [SerializeField] private Transform vipRock;
    [SerializeField] private Animator fightAnimator;

    [Header("Actions Configuration")]
    [SerializeField] private List<RedemptionActionSO> availableRedemptions;

    private Dictionary<string, RedemptionActionSO> redemptionLookup;
    
    // Core GameTask for pulling events as documented in Unity SDK reference
    private GameTask<EventStream<CustomRewardEvent>> customRewardEvents;
    private bool isSubscribed = false;

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

    private void Update()
    {
        // Wait until the plugin confirms authentication before subscribing
        if (authManager == null || !authManager.IsAuthenticated) return;

        if (!isSubscribed)
        {
            customRewardEvents = Twitch.API.SubscribeToCustomRewardEvents();
            isSubscribed = true;
            Debug.Log("Twitch Plugin: Subscribed to Custom Reward Events.");
        }
        
        // Poll the event stream using TryGetNextEvent
        if (customRewardEvents != null && customRewardEvents.MaybeResult != null)
        {
            CustomRewardEvent curRewardEvent;
            
            // Extracts the event if one is available in the buffer
            customRewardEvents.MaybeResult.TryGetNextEvent(out curRewardEvent);
            
            if (curRewardEvent != null)
            {
                Debug.Log($"[Twitch] {curRewardEvent.RedeemerName} redeemed {curRewardEvent.CustomRewardTitle} for {curRewardEvent.CustomRewardCost}!");
                HandleRedemption(curRewardEvent);
            }
        }
    }

    private void HandleRedemption(CustomRewardEvent e)
    {
        // Map the API's CustomRewardTitle directly to your ScriptableObjects
        if (redemptionLookup.TryGetValue(e.CustomRewardTitle, out var action))
        {
            var context = new RedemptionContext
            {
                Username = e.RedeemerName,
                AvatarManager = avatarManager,
                VipRock = vipRock,
                FightAnimator = fightAnimator,
                CoroutineRunner = this
            };

            action.Execute(context);
        }
    }
}