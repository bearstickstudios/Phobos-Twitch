using UnityEngine;

public class RedemptionContext
{
    public string Username { get; set; }
    public ChatAvatarManager AvatarManager { get; set; }
    public Transform VipRock { get; set; }
    public Animator FightAnimator { get; set; }
    public MonoBehaviour CoroutineRunner { get; set; }
}