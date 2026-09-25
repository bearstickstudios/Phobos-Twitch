using UnityEngine;

public class ShinboVIP : MonoBehaviour
{
    private ChatAvatar chatAvatar;
    private WalkBehavior walkBehavior;
    private bool doOnce;

    private void Awake()
    {
        chatAvatar = GetComponent<ChatAvatar>();
        walkBehavior = GetComponent<WalkBehavior>();
        if (chatAvatar != null && walkBehavior != null)
        {
            if (chatAvatar.OnVIP)
            {
                walkBehavior.animator.SetBool(walkBehavior.IS_VIP_PARAM, true);
            }
        }
        
    }

    private void Update()
    {
        if (doOnce)
        {
            if (chatAvatar != null)
            {
                if (chatAvatar.OnVIP)
                {
                    doOnce = false;
                    walkBehavior.animator.SetBool(walkBehavior.IS_VIP_PARAM, true);
                }
            }
        }
        
    }
}