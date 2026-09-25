using System;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class ChatAvatar : MonoBehaviour
{
    public ChatMessage messageData;
    
    [Header("Visuals")]
    [SerializeField] private Renderer avatarRenderer;
    [SerializeField] public Transform avatarTransform;
    [SerializeField] private BlendShapeController avatarBlendShape;
    
    [Header("Despawn Settings")]
    [SerializeField] private float despawnTimeMinutes = 30f;
    [SerializeField] private GameObject nameTagObject;
    [SerializeField] private TMP_Text infoObject;
    [SerializeField] private TMP_Text toastObject;
    private GameObject handObject;
    
    private string username;
    private DateTime lastActivityTime;
    private FallingEmote detectedEmote;

    [SerializeField] private bool isDetectingEmote;
    
    private TMP_Text nameTag;
    
    private GameObject cameraToLook;
    private WalkBehavior walkBehavior;
    private Transform vipRockTarget;
    private AvatarFamily avatarFamily;
    public float currentStrength;
    public bool OnVIP;
    private bool isPetting = false;
    private bool isToasting = false;
    
    public string Username => username;
    public DateTime LastActivityTime => lastActivityTime;
    
    public void Initialize(string user, ChatMessage message, List<Collider> walkBounds, GameObject cameraToLook, AvatarFamily family)
    {
        username = user;
        messageData = message;
        lastActivityTime = DateTime.Now;
        this.cameraToLook = cameraToLook;
        avatarFamily = family;
        currentStrength = family.strength;

        infoObject.text = $"{currentStrength}";
        
        var petComponent = GetComponentInChildren<Pet>();
        if (petComponent == null)
        {
            Debug.LogWarning($"ChatAvatar '{username}' is missing a Pet child object.", this);
        }
        else
        {
            handObject = petComponent.gameObject;
            handObject.SetActive(false);
        }
        //- toastObject.gameObject.SetActive(false);
        
        ApplyUniqueColor();
        
        CreateNameTag();
        ApplyAvatarEffects();
        
        SetupWalkBehavior(walkBounds);
    }

    private void Update()
    {
        if (isPetting)
        {
            var animator = handObject.GetComponent<Animator>();
            var isPetDone = !animator.GetCurrentAnimatorStateInfo(0).IsName("Pet");

            if (isPetDone)
            {
                handObject.SetActive(false);
                isPetting = false;
            }
        }

        // if (!isToasting) return;
        // {
        //     var animator = toastObject.GetComponent<Animator>();
        //     var isToastDone = !animator.GetCurrentAnimatorStateInfo(0).IsName("Toast");
        //
        //     if (isToastDone)
        //     {
        //         toastObject.gameObject.SetActive(false);
        //         isToasting = false;
        //     }
        // }
    }

    public void UpdateActivity(ChatMessage newMessage)
    {
        messageData = newMessage;
        lastActivityTime = DateTime.Now;
        
        // Update visual effects based on new message
        ApplyAvatarEffects();
        
        Debug.Log($"Updated activity for {username}");
    }
    
    public bool ShouldDespawn()
    {
        TimeSpan timeSinceLastActivity = DateTime.Now - lastActivityTime;
        return timeSinceLastActivity.TotalMinutes >= despawnTimeMinutes;
    }
    
    public void ResetAvatar()
    {
        // Reset all avatar state for pooling
        username = "";
        lastActivityTime = DateTime.MinValue;
        
        // Reset walk behavior
        if (walkBehavior != null)
        {
            walkBehavior.StopWalking();
        }
    }
    
    public void MoveToEmote(FallingEmote emote)
    {
        if (walkBehavior != null)
        {
            // Set destination to emote position
            isDetectingEmote = true;
            detectedEmote = emote;
            walkBehavior.EnqueueTarget(emote.transform.position);
            Debug.Log($"{username} moving to collect emote: {emote.EmoteData.emoteName}");
        }
    }
    
    public void MoveToVipRock(Transform vipRock)
    {
        if (walkBehavior != null)
        {
            // Set destination to VIP rock position
            walkBehavior.StopWalking();
            transform.SetParent(vipRock);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            
            vipRockTarget = vipRock; // Store reference for when we arrive
            OnVIP = true;
            CommandsManager commandsManager = FindObjectOfType<CommandsManager>();
            if (commandsManager != null)
            {
                commandsManager.OnAvatarMountedVipRock(username);
            }
            
            vipRockTarget = null; // Clear the target
            
            Debug.Log($"{username} moving to VIP rock");
        }
    }
    
    public bool CheckIfReachedVipRock(Vector3 reachedPosition)
    {
        if (vipRockTarget != null)
        {
            float distance = Vector3.Distance(reachedPosition, vipRockTarget.position);
            if (distance < 1f) // Close enough to VIP rock
            {
                // We've reached the VIP rock - stop walking and mount it
                walkBehavior.StopWalking();
            
                // Parent to the rock and reset position
                transform.SetParent(vipRockTarget);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            
                Debug.Log($"{username} has mounted the VIP rock!");
            
                // Notify the command manager
                CommandsManager commandsManager = FindObjectOfType<CommandsManager>();
                if (commandsManager != null)
                {
                    commandsManager.OnAvatarMountedVipRock(username);
                }
            
                vipRockTarget = null; // Clear the target
                return true;
            }
        }
        return false;
    }
    
    private void SetupWalkBehavior(List<Collider> walkBounds)
    {
        walkBehavior = GetComponent<WalkBehavior>();
        if (walkBehavior == null)
        {
            walkBehavior = gameObject.AddComponent<WalkBehavior>();
        }
        
        walkBehavior.Initialize(walkBounds);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (isDetectingEmote && other.TryGetComponent(out FallingEmote emote))
        {
            if (emote == detectedEmote)
            {
                CollectEmote(detectedEmote);
            }
        }
    }

    private void CollectEmote(FallingEmote emote)
    {
        // Start eating sequence
        StartCoroutine(EatingSequence(emote));
    }
    
    private IEnumerator EatingSequence(FallingEmote emote)
    {
        int chewCount = Random.Range(2, 5); // Random number of chews
        float chewSpeed = 0.25f;
        
        // Get eating duration and pause walking for that long
        float eatingDuration = avatarBlendShape.GetEatingDuration(chewCount, chewSpeed);
        walkBehavior.PauseForDuration(eatingDuration);
        
        // Start eating animation
        yield return StartCoroutine(avatarBlendShape.EatAnimation(chewCount, chewSpeed));
        
        Debug.Log($"{username} ate emote: {emote.EmoteData.emoteName} with {chewCount} chews - New scale: {avatarTransform.localScale.x:F2}");
        
        // Update activity time and cleanup
        lastActivityTime = DateTime.Now;
        emote.OnCollected();
        OnEaten();
        
        isDetectingEmote = false;
    }

    private void OnEaten()
    {
        currentStrength += 5;
        
        infoObject.text = $"{currentStrength}";
        
        DoToast("+5");

        if (currentStrength >= avatarFamily.strength + 25f)
        {
            OnVomited();
        }
    }

    private void DoToast(string toastText)
    {
       // toastObject.gameObject.SetActive(true);
        toastObject.GetComponent<Animator>().SetTrigger("Toast");
        toastObject.text = toastText;
      //  isToasting = true;
    }

    public void OnPetted()
    {
        handObject.SetActive(true);
        handObject.GetComponent<Animator>().SetTrigger("Pet");

        if (currentStrength - 3 >= avatarFamily.strength)
        {
            currentStrength -= 3;
            infoObject.text = $"{currentStrength}";
            DoToast("-3");
        }
    }
    
    private void OnVomited()
    {
        DoToast("Base Strength -20");
        currentStrength = avatarFamily.strength * 0.6f;
        infoObject.text = $"{currentStrength}";
    }

    private void ApplyUniqueColor()
    {
        if (avatarRenderer == null)
        {
            Debug.LogWarning("Avatar Renderer is not assigned!", this);
            return;
        }
        
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        avatarRenderer.GetPropertyBlock(propBlock);
        
        Random.InitState(username.GetHashCode());
        Color randomColor = Random.ColorHSV(0f, 1f, 0.15f, 0.30f, 0.9f, 1f);
        
        propBlock.SetColor("_BaseColor", randomColor);
        avatarRenderer.SetPropertyBlock(propBlock);
    }

    
    private void ApplyAvatarEffects()
    {
        // Handle different message types
        switch (messageData.type)
        {
            case MessageType.RegularChat:
                // TODO: Standard avatar appearance
                break;
                
            case MessageType.EmoteOnly:
                // TODO: Add emote effects to avatar
                // Example: Floating emote particles, bounce animation
                break;
                
            case MessageType.BitsCheer:
                // TODO: Add bits celebration effects
                // Example: Golden glow, coin particles, celebration animation
                // Could scale effects based on messageData.bitsAmount
                break;
                
            case MessageType.UserNotice:
                HandleUserNoticeEffects();
                break;
        }
        
        // Apply badge-based effects
        ApplyBadgeEffects();
        
        // Handle emotes if present
        if (messageData.hasEmotes)
        {
            // TODO: Add emote-specific effects
            // Example: Display emotes above avatar, emote trail
            Debug.Log($"{username} used {messageData.emotes.Length} emotes");
        }
    }
    
    private void HandleUserNoticeEffects()
    {
        switch (messageData.noticeType)
        {
            case UserNoticeType.Sub:
            case UserNoticeType.Resub:
                // TODO: Add subscription celebration effects
                // Example: Confetti particles, crown effect, special animation
                // For resub, could show month count: messageData.subMonths
                break;
                
            case UserNoticeType.SubGift:
                // TODO: Add gift celebration effects
                // Example: Present box animation, gift particles
                break;
                
            case UserNoticeType.Raid:
                // TODO: Add raid effects
                // Example: Invasion particles, army banner
                // Could scale based on messageData.raidViewers
                break;
                
            case UserNoticeType.BitsBadgeTier:
                // TODO: Add bits badge tier celebration
                // Example: Badge upgrade animation, achievement effect
                break;
        }
    }
    
    private void ApplyBadgeEffects()
    {
        if (nameTag == null) return;
        
        if (messageData.isBroadcaster)
        {
            // TODO: Apply broadcaster effects
            // Example: Crown above nameTag, special color, larger size
            nameTag.color = Color.red; // Temporary broadcaster indicator
        }
        else if (messageData.isModerator)
        {
            // TODO: Apply moderator effects
            // Example: Sword icon, mod badge, green nameTag
            nameTag.color = Color.green; // Temporary moderator indicator
        }
        else if (messageData.isVip)
        {
            // TODO: Apply VIP effects
            // Example: Diamond icon, purple nameTag, special glow
            nameTag.color = Color.magenta; // Temporary VIP indicator
        }
        else if (messageData.isSubscriber)
        {
            // TODO: Apply subscriber effects
            // Example: Sub badge, special color, subscriber perks
            nameTag.color = Color.cyan; // Temporary subscriber indicator
        }
        else
        {
            // Regular user - random color based on username for consistency
            Random.InitState(username.GetHashCode());
            nameTag.color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
        }
        
        // TODO: Handle additional badges from messageData.badges array
        // Example: Parse custom badges, channel-specific badges, etc.
    }
    
    private void CreateNameTag()
    {
        // Add TextMeshPro component
        nameTag = nameTagObject.GetComponent<TMP_Text>();
        nameTag.text = username;
        // Color will be set in ApplyAvatarEffects() based on user status
        // Make name tag always face camera
        if (cameraToLook != null)
        {
            var lookAt = nameTagObject.GetComponent<LookAtConstraint>();
            lookAt.AddSource(new ConstraintSource { sourceTransform = cameraToLook.transform, weight = 1f });
            lookAt.constraintActive = true;
        }
    }
}