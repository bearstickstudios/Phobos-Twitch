using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChatAvatarManager : MonoBehaviour
{
    [Header("Avatar Prefabs")]
    [SerializeField] private GameObject fallbackPrefab; 
    [SerializeField] private List<AvatarFamily> avatarFamilies = new List<AvatarFamily>();
    
    [Header("Bounds Settings")]
    [SerializeField] private Collider spawnBounds;
    [SerializeField] private List<Collider> walkBoundsList = new List<Collider>();
    
    [Header("Avatar Settings")]
    [SerializeField] private GameObject cameraToLook;
    
    [Header("Despawn Management")]
    [SerializeField] private float despawnCheckInterval = 30f; // Check every 30 seconds
    
    private Dictionary<string, ChatAvatar> activeAvatars = new Dictionary<string, ChatAvatar>();
    private TwitchChatClient chatClient;
    private AvatarPoolManager poolManager;
    private AvatarFamily selectedFamily;
    
    void Start()
    {
        chatClient = FindObjectOfType<TwitchChatClient>();
        if (chatClient == null)
        {
            Debug.LogError("TwitchChatClient not found!");
            return;
        }
        
        poolManager = FindObjectOfType<AvatarPoolManager>();
        if (poolManager == null)
        {
            Debug.LogError("AvatarPoolManager not found!");
            return;
        }
        
        // Use walkBounds as spawnBounds if not set
        if (spawnBounds == null)
        {
            spawnBounds = walkBoundsList[0];
        }
        
        chatClient.OnMessageReceived += OnChatMessage;
        
        // Start despawn management coroutine
        StartCoroutine(DespawnManagementCoroutine());
    }
    
    void OnDestroy()
    {
        chatClient.OnMessageReceived -= OnChatMessage;
    }
    
    void OnChatMessage(ChatMessage message)
    {
        string username = message.username.ToLower();
        
        // Check if avatar already exists
        if (activeAvatars.ContainsKey(username))
        {
            // Update existing avatar activity
            activeAvatars[username].UpdateActivity(message);
            Debug.Log($"Updated activity for existing avatar: {username}");
            return;
        }
        
        // Handle different message types
        switch (message.type)
        {
            case MessageType.RegularChat:
                SpawnAvatar(username, message);
                break;
                
            case MessageType.EmoteOnly:
                // TODO: Spawn avatar with special emote-focused appearance
                SpawnAvatar(username, message);
                break;
                
            case MessageType.BitsCheer:
                // TODO: Spawn avatar with bits celebration effects
                SpawnAvatar(username, message);
                Debug.Log($"{username} cheered {message.bitsAmount} bits!");
                break;
                
            case MessageType.UserNotice:
                HandleUserNotice(message);
                break;
                
            case MessageType.System:
                // TODO: System messages might not need avatars
                Debug.Log($"System message: {message.message}");
                break;
        }
    }
    
    void HandleUserNotice(ChatMessage message)
    {
        string username = message.username.ToLower();
        
        switch (message.noticeType)
        {
            case UserNoticeType.Sub:
                SpawnAvatar(username, message);
                Debug.Log($"{username} just subscribed!");
                break;
                
            case UserNoticeType.Resub:
                SpawnAvatar(username, message);
                Debug.Log($"{username} resubscribed for {message.subMonths} months!");
                break;
                
            case UserNoticeType.SubGift:
                SpawnAvatar(username, message);
                Debug.Log($"{username} gifted a subscription!");
                break;
                
            case UserNoticeType.Raid:
                SpawnAvatar(username, message);
                Debug.Log($"Raid from {message.raidFrom} with {message.raidViewers} viewers!");
                break;
                
            case UserNoticeType.BitsBadgeTier:
                SpawnAvatar(username, message);
                Debug.Log($"{username} earned a new bits badge!");
                break;
                
            case UserNoticeType.Other:
                SpawnAvatar(username, message);
                break;
        }
    }
    
    void SpawnAvatar(string username, ChatMessage message)
    {
        if (spawnBounds == null || walkBoundsList == null)
        {
            Debug.LogError("Spawn bounds or walk bounds not set!");
            return;
        }
    
        // Get the appropriate prefab for this user
        GameObject prefabToUse = GetAvatarPrefab(message);
        if (prefabToUse == null)
        {
            Debug.LogWarning($"No prefab available for user {username}");
            return;
        }
    
        // Get avatar from pool using the selected prefab
        GameObject avatarObj = poolManager.GetAvatar(prefabToUse);
        if (avatarObj == null)
        {
            Debug.LogWarning($"Could not get avatar from pool for prefab: {prefabToUse.name}");
            return;
        }
    
        // Position avatar within spawn bounds
        Vector3 spawnPosition = GetRandomPointInCompositeBounds();
        
        avatarObj.transform.position = spawnPosition;
        avatarObj.transform.SetParent(transform);
        avatarObj.name = $"Avatar_{username}_{prefabToUse.name}";
    
        // Initialize avatar component
        ChatAvatar avatarScript = avatarObj.GetComponent<ChatAvatar>();
        if (avatarScript == null)
        {
            avatarScript = avatarObj.AddComponent<ChatAvatar>();
        }
    
        avatarScript.Initialize(username, message, walkBoundsList, cameraToLook, selectedFamily);
    
        // Store reference
        activeAvatars[username] = avatarScript;
    
        Debug.Log($"Spawned {prefabToUse.name} avatar for {username} at {spawnPosition}. Active avatars: {activeAvatars.Count}");
    }
    
    private GameObject GetAvatarPrefab(ChatMessage message)
    {
        string username = message.username.ToLower();
        
        // Check if we have any families available
        if (avatarFamilies.Count == 0)
        {
            Debug.LogWarning("No avatar families assigned!");
            return fallbackPrefab; // Fallback
        }
    
        // Select family based on weighted random
        selectedFamily = SelectWeightedFamily();
        
        if (selectedFamily == null || selectedFamily.variants.Count == 0)
        {
            Debug.LogWarning("Selected family has no variants!");
            return fallbackPrefab; // Fallback
        }
    
        // Select variant within the family
        AvatarVariant selectedVariant = SelectWeightedVariant(selectedFamily);
        
        return selectedVariant?.prefab ?? fallbackPrefab; // Fallback if null
    }
    
    private AvatarFamily SelectWeightedFamily()
    {
        float totalWeight = 0f;
        foreach (var family in avatarFamilies)
        {
            totalWeight += family.spawnChance;
        }
    
        if (totalWeight <= 0f) return null;
    
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
    
        foreach (var family in avatarFamilies)
        {
            currentWeight += family.spawnChance;
            if (randomValue <= currentWeight)
            {
                return family;
            }
        }
    
        return avatarFamilies[avatarFamilies.Count - 1]; // Fallback to last family
    }
    
    private AvatarVariant SelectWeightedVariant(AvatarFamily family)
    {
        float totalWeight = 0f;
        foreach (var variant in family.variants)
        {
            totalWeight += variant.variantChance;
        }
    
        if (totalWeight <= 0f) return family.variants[0]; // Return first variant as fallback
    
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
    
        foreach (var variant in family.variants)
        {
            currentWeight += variant.variantChance;
            if (randomValue <= currentWeight)
            {
                return variant;
            }
        }
    
        return family.variants[family.variants.Count - 1]; // Fallback to last variant
    }
    
    private Vector3 GetRandomPointInCompositeBounds()
    {
        if (walkBoundsList == null || walkBoundsList.Count == 0) return transform.position;

        Collider selected = walkBoundsList[Random.Range(0, walkBoundsList.Count)];
        Bounds bounds = selected.bounds;

        Vector3 randomPoint = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.center.y,
            Random.Range(bounds.min.z, bounds.max.z)
        );

        return selected.ClosestPoint(randomPoint);
    }
    
    private IEnumerator DespawnManagementCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(despawnCheckInterval);
            
            // Check for avatars that should be despawned
            List<string> avatarsToRemove = new List<string>();
            
            foreach (var kvp in activeAvatars)
            {
                if (kvp.Value == null || kvp.Value.ShouldDespawn())
                {
                    avatarsToRemove.Add(kvp.Key);
                }
            }
            
            // Remove avatars that should be despawned
            foreach (string username in avatarsToRemove)
            {
                RemoveAvatar(username);
            }
            
            if (avatarsToRemove.Count > 0)
            {
                Debug.Log($"Despawned {avatarsToRemove.Count} inactive avatars. Active avatars: {activeAvatars.Count}");
            }
        }
    }
    
    public void RemoveAvatar(string username)
    {
        username = username.ToLower();
        if (activeAvatars.ContainsKey(username))
        {
            ChatAvatar avatar = activeAvatars[username];
            if (avatar != null)
            {
                // Return to pool instead of destroying
               poolManager.ReturnAvatar(avatar.gameObject);
               //Destroy(avatar.gameObject);
            }
            
            activeAvatars.Remove(username);
        }
    }
    
    public ChatAvatar[] GetActiveAvatars()
    {
        return activeAvatars.Values.ToArray();
    }
    
    public void ClearAllAvatars()
    {
        List<string> allUsernames = new List<string>(activeAvatars.Keys);
        foreach (string username in allUsernames)
        {
            RemoveAvatar(username);
        }
        
        Debug.Log("Cleared all avatars");
    }
    
    public int GetActiveAvatarCount()
    {
        return activeAvatars.Count;
    }
    
    // Gizmos for visualizing bounds in scene view
    private void OnDrawGizmosSelected()
    {
        if (spawnBounds != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(spawnBounds.bounds.center, spawnBounds.bounds.size);
        }
        
        if (walkBoundsList != null)
        {
            Gizmos.color = Color.blue;
            foreach (var col in walkBoundsList)
            {
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }

    /// <summary>
    /// Despawns a user's current avatar and spawns a new one, effectively "rerolling" it.
    /// </summary>
    /// <param name="username">The user to reroll.</param>
    public void RerollAvatar(string username)
    {
        string lowerUsername = username.ToLower();
        if (activeAvatars.TryGetValue(lowerUsername, out ChatAvatar avatar))
        {
            // To respawn, we need the original message.
            // This assumes you store the initial message on the ChatAvatar script as suggested.
            ChatMessage initialMessage = avatar.GetComponent<ChatAvatar>().messageData;

            Debug.Log($"Rerolling avatar for {username}...");
            RemoveAvatar(lowerUsername);
            SpawnAvatar(lowerUsername, initialMessage);
        }
    }

    public void PetAvatar(string username)
    {
        string lowerUsername = username.ToLower();
        if (activeAvatars.TryGetValue(lowerUsername, out ChatAvatar avatar))
        {
            avatar.OnPetted();
        }
    }

    public ChatAvatar FindAvatarByUsername(string contextUsername)
    {
        throw new System.NotImplementedException();
    }
}