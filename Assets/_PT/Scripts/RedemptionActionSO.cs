using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class RedemptionActionSO : ScriptableObject
{
    [Tooltip("The exact Custom Reward Title from your Twitch Dashboard")]
    public string rewardTitle;

    public abstract void Execute(RedemptionContext context);
}

[CreateAssetMenu(fileName = "VipRockAction", menuName = "Twitch/Redemptions/VIP Rock")]
public class VipRockActionSO : RedemptionActionSO
{
    public override void Execute(RedemptionContext context)
    {
        if (context.AvatarManager == null) return;
        
        ChatAvatar userAvatar = context.AvatarManager.FindAvatarByUsername(context.Username);
        if (userAvatar == null) return;

        if (context.VipRock.childCount > 0)
        {
            ChatAvatar currentVip = context.VipRock.GetChild(0).GetComponent<ChatAvatar>();
            if (currentVip == null || currentVip == userAvatar) return;

            context.CoroutineRunner.StartCoroutine(PlayFightSequence(context, userAvatar, currentVip));
        }
        else
        {
            userAvatar.MoveToVipRock(context.VipRock);
        }
    }

    private IEnumerator PlayFightSequence(RedemptionContext context, ChatAvatar challenger, ChatAvatar currentVip)
    {
        currentVip.gameObject.SetActive(false);
        challenger.gameObject.SetActive(false);
        
        if (context.FightAnimator != null)
        {
            context.FightAnimator.SetBool("Fight", true);
            yield return new WaitForSeconds(6f);
            context.FightAnimator.SetBool("Fight", false);
        }

        currentVip.gameObject.SetActive(true);
        challenger.gameObject.SetActive(true);

        if (challenger.currentStrength > currentVip.currentStrength)
        {
            currentVip.transform.SetParent(null);
            challenger.MoveToVipRock(context.VipRock);
        }
    }
}

[CreateAssetMenu(fileName = "DuelAction", menuName = "Twitch/Redemptions/Duel")]
public class DuelActionSO : RedemptionActionSO
{
    public override void Execute(RedemptionContext context)
    {
        if (context.AvatarManager == null) return;
        
        ChatAvatar challenger = context.AvatarManager.FindAvatarByUsername(context.Username);
        if (challenger == null) return;

        List<ChatAvatar> potentialOpponents = context.AvatarManager.GetActiveAvatars()
            .Where(avatar => avatar != challenger && avatar.transform.parent != context.VipRock)
            .ToList();

        if (potentialOpponents.Count == 0) return;
        
        ChatAvatar opponent = potentialOpponents[Random.Range(0, potentialOpponents.Count)];
        ChatAvatar loser = (challenger.currentStrength >= opponent.currentStrength) ? opponent : challenger;
        
        context.AvatarManager.RemoveAvatar(loser.Username);
    }
}