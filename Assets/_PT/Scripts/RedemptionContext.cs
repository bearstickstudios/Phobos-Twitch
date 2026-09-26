using UnityEngine;

namespace PhobosTwitch
{
    public class RedemptionContext
    {
        public string Username { get; set; }
        public ChatAvatarManager AvatarManager { get; set; }
        public Transform VipRock { get; set; }
        public Animator FightAnimator { get; set; }
    }
}