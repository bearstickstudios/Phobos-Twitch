using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CommandsManager : MonoBehaviour
{
    [Header("Channel Point Reward IDs")]
    [Tooltip("The Custom Reward ID from your Twitch dashboard for the 'VIP' redemption.")]
    [SerializeField] private string vipRewardId = "your-vip-reward-id-here";
    [Tooltip("The Custom Reward ID for the 'Reroll' redemption.")]
    [SerializeField] private string rerollRewardId = "your-reroll-reward-id-here";
    [Tooltip("The Custom Reward ID for the 'Duel' redemption.")]
    [SerializeField] private string duelRewardId = "your-duel-reward-id-here";
    [Tooltip("The Custom Reward ID for the 'Colorify' redemption.")]
    [SerializeField] private string colorifyRewardId = "your-colorify-reward-id-here";
    
    [Header("VIP Rock Settings")]
    [SerializeField] private Transform vipRock;

    [SerializeField] private Animator fightAnimator;

    // [SerializeField] private float minimumScaleRequired = 2f;
   
    [Header("References")]
    private TwitchChatClient chatClient;
    private ChatAvatarManager avatarManager;
    
    // private bool someoneHeadingToRock = false;
    private string usernameHeadingToRock = "";
    private void Start()
    {
        // Find required components
        chatClient = FindObjectOfType<TwitchChatClient>();
        avatarManager = FindObjectOfType<ChatAvatarManager>();
        
        if (chatClient == null)
        {
            Debug.LogError("TwitchChatClient not found!");
            return;
        }
        
        if (avatarManager == null)
        {
            Debug.LogError("ChatAvatarManager not found!");
            return;
        }
        
        if (vipRock == null)
        {
            Debug.LogError("VIP Rock Transform not assigned!");
            return;
        }
        
        chatClient.OnMessageReceived += OnChatMessage;
    }
    
    private void OnDestroy()
    {
        if (chatClient != null)
        {
            chatClient.OnMessageReceived -= OnChatMessage;
        }
    }
    
    private void OnChatMessage(ChatMessage message)
    {
        // Route channel point redemptions to their own handler
        if (message.type == MessageType.ChannelPointRedemption && !string.IsNullOrEmpty(message.customRewardId))
        {
            ProcessRedemptionCommand(message);
            return;
        }
        
        // Check if message starts with !
        if (!message.message.StartsWith("!")) return;
        
        ProcessAvatarCommand(message);
    }
    
    private void ProcessRedemptionCommand(ChatMessage message)
    {
        string rewardId = message.customRewardId;

        if (rewardId == vipRewardId)
        {
            HandleVipRedemption(message.username);
        }
        else if (rewardId == rerollRewardId)
        {
            HandleRerollRedeem(message.username);
        }
        else if (rewardId == duelRewardId)
        {
            HandleDuelRedeem(message.username);
        }
        else if (rewardId == colorifyRewardId)
        {
            HandleColorifyRedeem(message.username);
        }
    }
    
    private void HandleVipRedemption(string username)
    {
        ChatAvatar userAvatar = FindAvatarByUsername(username);
        if (userAvatar == null)
        {
            // Avatar might not exist yet, but redemption happened. We can ignore or queue.
            // For now, we'll just log it.
            Debug.LogWarning($"VIP redemption from {username}, but their avatar isn't active.");
            return;
        }

        // If the rock is occupied, start a fight. Otherwise, claim it.
        if (vipRock.childCount > 0)
        {
            Debug.Log($"@{username} redeemed VIP and is challenging for the rock!");
            HandleFightCommand(username); // Reuse existing fight logic
        }
        else
        {
            Debug.Log($"@{username} redeemed VIP and is claiming the rock!");
            HandleVipCommand(username); // Reuse existing VIP logic
        }
    }
    
    private void HandleRerollRedeem(string username)
    {
        avatarManager.RerollAvatar(username);
        Debug.Log($"@{username} has rerolled their avatar!");
    }

    private void HandleDuelRedeem(string username)
    {
        ChatAvatar challenger = FindAvatarByUsername(username);
        if (challenger == null) return;

        // Find all other avatars that are not on the VIP rock
        List<ChatAvatar> potentialOpponents = avatarManager.GetActiveAvatars()
            .Where(avatar => avatar != challenger && avatar.transform.parent != vipRock)
            .ToList();

        if (potentialOpponents.Count == 0)
        {
            Debug.Log($"@{username} wants to duel, but there are no opponents available!");
            return;
        }

        // Pick a random opponent
        ChatAvatar opponent = potentialOpponents[Random.Range(0, potentialOpponents.Count)];

        // Compare sizes
        float challengerSize = challenger.avatarTransform.localScale.x;
        float opponentSize = opponent.avatarTransform.localScale.x;
        
        Debug.Log($"DUEL! @{challenger.Username} (size: {challengerSize:F1}) challenges @{opponent.Username} (size: {opponentSize:F1})!");

        ChatAvatar winner, loser;
        if (challengerSize >= opponentSize)
        {
            winner = challenger;
            loser = opponent;
        }
        else
        {
            winner = opponent;
            loser = challenger;
        }

        Debug.Log($"@{winner.Username} has won the duel! @{loser.Username} has been defeated. 💥");
        avatarManager.RemoveAvatar(loser.Username); // Remove the loser
    }

    private void HandleColorifyRedeem(string username)
    {
        ChatAvatar avatar = FindAvatarByUsername(username);
        if (avatar != null)
        {
            // NOTE: This assumes you have a public method on your ChatAvatar.cs script
            // that handles changing the avatar's color.
            // Example:
            // avatar.RandomizeColor();
            
            Debug.Log($"@{username} has colorified their avatar!");
            Debug.Log($"Colorify command called for {username}. Implement the color change logic on your ChatAvatar script.");
        }
    }
    
    private void ProcessAvatarCommand(ChatMessage message)
    {
        string command = message.message.ToLower();
        
        if (command.StartsWith("!vip"))
        {
            HandleVipCommand(message.username);
        }
        else if (command.StartsWith("!fight"))
        {
            HandleFightCommand(message.username);
        }
        else if (command.StartsWith("!pet"))
        {
            HandlePetCommand(message.username,message.ToString());
        }
    }

    private void HandlePetCommand(string username, string command)
    {
        // Check if the user's avatar is active
        ChatAvatar userAvatar = FindAvatarByUsername(username);
        if (userAvatar == null)
        {
            Debug.Log($"@{username} Your avatar is not currently active!");
            return;
        }
    
        if (vipRock.childCount == 0)
        {
            Debug.Log($"@{username} The VIP rock is currently NOT occupied!");
            return;
        }
        
        // // Parse the target username from the command
        // string[] commandParts = command.Split(' ');
        // if (commandParts.Length < 2)
        // {
        //     Debug.Log($"@{username} Please specify who you want to pet! Usage: !pet username");
        //     return;
        // }
        //
        // // Get the target username and clean it up
        // string targetUsername = commandParts[1].Trim();
        //
        // // Remove @ symbol if present
        // if (targetUsername.StartsWith("@"))
        // {
        //     targetUsername = targetUsername.Substring(1);
        // }
        //
        // // Check if trying to pet themselves
        // if (targetUsername.Equals(username, System.StringComparison.OrdinalIgnoreCase))
        // {
        //     Debug.Log($"@{username} You cannot pet yourself!");
        //     return;
        // }
        //
        // // Find the target avatar
        // ChatAvatar targetAvatar = FindAvatarByUsername(targetUsername);
        // if (targetAvatar == null)
        // {
        //     Debug.Log($"@{username} Could not find an active avatar for @{targetUsername}!");
        //     return;
        // }
        
        // if (targetAvatar.transform.parent != vipRock)
        // {
        //     Debug.Log($"@{username} You cannot pet someone not on the rock!");
        //     return;   
        // }

        var targetAvatar = vipRock.GetChild(0).gameObject;
        var targetUsername = targetAvatar.GetComponent<ChatAvatar>().Username;
        
        // Execute the pet action
        avatarManager.PetAvatar(targetUsername);
        Debug.Log($"@{username} pets @{targetUsername}!");
    }

    private void HandleVipCommand(string username)
    {
        // Find the user's avatar
        ChatAvatar userAvatar = FindAvatarByUsername(username);
        if (userAvatar == null)
        {
            Debug.Log($"@{username} Your avatar is not currently active!");
            return;
        }
    
        // Check if rock is occupied OR someone is heading there
        if (vipRock.childCount > 0)
        {
            ChatAvatar currentVip = vipRock.GetChild(0).GetComponent<ChatAvatar>();
            Debug.Log($"@{username} The VIP rock is currently occupied by @{currentVip.Username}! Use !fight to challenge them!");
            return;
        }
        
        usernameHeadingToRock = username;
    
        // Move avatar to VIP rock
        MoveAvatarToVipRock(userAvatar);
    }
    
    /// <summary>
    /// Called when an avatar successfully mounts the VIP rock
    /// </summary>
    /// <param name="username">Username of avatar that mounted the rock</param>
    public void OnAvatarMountedVipRock(string username)
    {
        // someoneHeadingToRock = false;
        usernameHeadingToRock = "";
        Debug.Log($"@{username} has successfully claimed the VIP rock! 👑");
    }

    /// <summary>
    /// Called if an avatar fails to reach the VIP rock for any reason
    /// </summary>
    public void OnAvatarFailedToReachVipRock()
    {
        // someoneHeadingToRock = false;
        usernameHeadingToRock = "";
    }
    
    private void HandleFightCommand(string username)
    {
        // Find the user's avatar
        ChatAvatar challengerAvatar = FindAvatarByUsername(username);
        if (challengerAvatar == null)
        {
            Debug.Log($"@{username} Your avatar is not currently active!");
            return;
        }

        // Check if rock is vacant
        if (vipRock.childCount == 0)
        {
            Debug.Log($"@{username} The VIP rock is empty! Use !vip to claim it!");
            return;
        }

        // Find current VIP
        ChatAvatar currentVip = vipRock.GetChild(0).GetComponent<ChatAvatar>();
        if (currentVip == null)
        {
            Debug.LogError("VIP rock has a child but no ChatAvatar component!");
            return;
        }

        // Compare strength
        float challengerStrength = challengerAvatar.currentStrength;
        float currentVipStrength = currentVip.currentStrength;

        // Hide avatars before animation
        currentVip.gameObject.SetActive(false);
        challengerAvatar.gameObject.SetActive(false);

        // Start animation and delay logic
        StartCoroutine(PlayFightAnimationThen(() =>
        {
            fightAnimator.SetBool("Fight", false);
            currentVip.gameObject.SetActive(true);
            challengerAvatar.gameObject.SetActive(true);

            if (challengerStrength > currentVipStrength)
            {
                // Challenger wins
                RemoveAvatarFromVipRock(currentVip);
                MoveAvatarToVipRock(challengerAvatar);

                Debug.Log($"@{username} (strength: {challengerStrength:F1}) has defeated @{currentVip.Username} (strength: {currentVipStrength:F1}) and claimed the VIP rock! 🥊👑");
            }
            else
            {
                // Challenger loses
                Debug.Log($"@{username} (strength: {challengerStrength:F1}) challenged @{currentVip.Username} (strength: {currentVipStrength:F1}) but was too small to win! 💪");
            }
        }));
    }
    
    private System.Collections.IEnumerator PlayFightAnimationThen(System.Action onComplete)
    {
        fightAnimator.SetBool("Fight",true);

        // Wait for the animation to finish
       // var clipInfo = fightAnimator.GetCurrentAnimatorClipInfo(0);
      //  float animationLength = clipInfo[1].clip.length;

        yield return new WaitForSeconds(6f);

        onComplete?.Invoke();
    }

    
    private ChatAvatar FindAvatarByUsername(string username)
    {
        // Get all active avatars from the avatar manager
        var activeAvatars = avatarManager.GetActiveAvatars();
        return activeAvatars.FirstOrDefault(avatar => avatar.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase));
    }
    
    private void MoveAvatarToVipRock(ChatAvatar avatar)
    {
        // Tell avatar to move to VIP rock
        avatar.MoveToVipRock(vipRock);
        
        Debug.Log($"{avatar.Username} walking to VIP rock");
    }
    
    private void RemoveAvatarFromVipRock(ChatAvatar avatar)
    {
        // Unparent from rock
        avatar.transform.SetParent(null);
        
        // Move to a random position near the rock
        Vector3 randomOffset = new Vector3(
            Random.Range(-3f, 3f),
            0f,
            Random.Range(-3f, 3f)
        );
        avatar.transform.position = vipRock.position + randomOffset;
        
        // Restart walking behavior
        WalkBehavior walkBehavior = avatar.GetComponent<WalkBehavior>();
        if (walkBehavior != null)
        {
            walkBehavior.StartWalking();
        }
        
        Debug.Log($"{avatar.Username} removed from VIP rock");
    }
    
    private void SendAutoMessage(string message)
    { 
        chatClient.SendMessage(message);
    }
}