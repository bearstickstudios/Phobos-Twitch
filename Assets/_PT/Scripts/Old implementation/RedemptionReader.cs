// using TwitchSDK.Interop;
// using TwitchSDK;
// using System;
// using UnityEngine;
// using System.Threading.Tasks;
// using System.Linq;
//
// namespace TwitchEventsubUnity
// {
//     public class RedemptionReader : EventsubModule
//     {
//         public bool clearRedeemsOnDisable;
//         public RedemptionDef[] redemptions;
//         GameTask<EventStream<CustomRewardEvent>> m_CustomRewardEvents;
//         public override void Authenticate(string accessToken, string channel)
//         {
//             base.Authenticate(accessToken, channel);
//             RegisterRedeems(true);
//             m_CustomRewardEvents = Twitch.API.SubscribeToCustomRewardEvents();
//         }
//
//         void Update()
//         {
//             if (!authenticated)
//                 return;
//
//             CustomRewardEvent CurRewardEvent;
//             if (m_CustomRewardEvents != null)
//             {
//                 m_CustomRewardEvents.MaybeResult.TryGetNextEvent(out CurRewardEvent);
//                 if (CurRewardEvent != null)
//                 {
//                     ProcessRedeem(CurRewardEvent);
//
//                 }
//             }
//         }
//
//         private void OnDisable()
//         {
//             if (clearRedeemsOnDisable)
//                 RegisterRedeems(false);
//
//         }
//
//
//         private static void ProcessRedeem(CustomRewardEvent CurRewardEvent)
//         {
//             // Do something
//             Debug.Log($"{CurRewardEvent.RedeemerName} has bought {CurRewardEvent.CustomRewardTitle} for {CurRewardEvent.CustomRewardCost}!");
//
//             switch (CurRewardEvent.CustomRewardTitle)
//             {
//
//                 default:
//                     break;
//             }
//         }
//
//         private async void RegisterRedeems(bool enabled = true)
//         {
//             try
//             {
//                 //usage example
//                 /*
//                 await Twitch.API.ReplaceCustomRewards(
//                             new CustomRewardDefinition()
//                             {
//                                 Cost = 1,
//                                 Title = "test",
//                                 Prompt = "",
//                                 ShouldRedemptionsSkipRequestQueue = true,
//                                 IsEnabled = enabled
//                             }
//                            );
//                 */
//
//                 await Twitch.API.ReplaceCustomRewards(
//                           redemptions.Select(x =>
//                           {
//                               return new CustomRewardDefinition()
//                               {
//                                   Title = x.Title,
//                                   Cost = x.Cost,
//                                   IsEnabled = enabled,
//                                   ShouldRedemptionsSkipRequestQueue = x.ShouldRedemptionsSkipRequestQueue,
//                                   Prompt = x.Prompt
//                               };
//                           }).ToArray()
//                           );
//
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError("failed to replace custom rewards");
//                 Debug.LogError(ex);
//             }
//
//
//
//         }
//
//     }
//     [Serializable]
//     public class RedemptionDef
//     {
//         //
//         // Summary:
//         //     The title of the reward.
//         public string Title = "Title";
//
//         //
//         // Summary:
//         //     The cost of the reward.
//         public long Cost = 1;
//
//         //
//         // Summary:
//         //     Optional. The prompt for the viewer when redeeming the reward.
//         public string Prompt = "Prompt";
//         public bool ShouldRedemptionsSkipRequestQueue = true;
//
//     }
// }
