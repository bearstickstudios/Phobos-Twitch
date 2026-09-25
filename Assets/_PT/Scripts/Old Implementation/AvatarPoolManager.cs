using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class AvatarPoolManager : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private int defaultCapacity = 10;
    [SerializeField] private int maxSize = 100;
    [SerializeField] private bool collectionCheck = true;
    [SerializeField] private Transform poolParent;
    
    // Dictionary to hold pools for different prefab types
    private Dictionary<GameObject, ObjectPool<GameObject>> avatarPools = new Dictionary<GameObject, ObjectPool<GameObject>>();
    private HashSet<GameObject> activeAvatars = new HashSet<GameObject>();
    
    private void Awake()
    {
        InitializePoolParent();
    }
    
    private void InitializePoolParent()
    {
        if (poolParent == null)
        {
            GameObject poolContainer = new GameObject("Avatar Pool");
            poolParent = poolContainer.transform;
            poolParent.SetParent(transform);
        }
    }
    
    // Get or create a pool for a specific prefab
    private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("Cannot create pool for null prefab!");
            return null;
        }
        
        if (!avatarPools.ContainsKey(prefab))
        {
            // Create new pool for this prefab type
            avatarPools[prefab] = new ObjectPool<GameObject>(
                createFunc: () => CreateAvatar(prefab),
                actionOnGet: OnGetAvatar,
                actionOnRelease: OnReleaseAvatar,
                actionOnDestroy: OnDestroyAvatar,
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
            
            Debug.Log($"Created new avatar pool for prefab: {prefab.name}");
        }
        
        return avatarPools[prefab];
    }
    
    // Pool callback: Create new avatar instance
    private GameObject CreateAvatar(GameObject prefab)
    {
        GameObject avatar = Instantiate(prefab, poolParent);
        avatar.SetActive(false);
        
        // Store reference to original prefab for pool identification
        AvatarPoolReference poolRef = avatar.GetComponent<AvatarPoolReference>();
        if (poolRef == null)
        {
            poolRef = avatar.AddComponent<AvatarPoolReference>();
        }
        poolRef.originalPrefab = prefab;
        
        return avatar;
    }
    
    // Pool callback: When avatar is retrieved from pool
    private void OnGetAvatar(GameObject avatar)
    {
        if (avatar != null)
        {
            avatar.SetActive(true);
            activeAvatars.Add(avatar);
        }
    }
    
    // Pool callback: When avatar is returned to pool
    private void OnReleaseAvatar(GameObject avatar)
    {
        if (avatar != null)
        {
            activeAvatars.Remove(avatar);
            ResetAvatarState(avatar);
            avatar.SetActive(false);
            avatar.transform.SetParent(poolParent);
        }
    }
    
    // Pool callback: When avatar is destroyed (pool overflow)
    private void OnDestroyAvatar(GameObject avatar)
    {
        if (avatar != null)
        {
            activeAvatars.Remove(avatar);
            Destroy(avatar);
        }
    }
    
    public GameObject GetAvatar(GameObject prefab)
    {
        ObjectPool<GameObject> pool = GetOrCreatePool(prefab);
        if (pool != null)
        {
            return pool.Get();
        }
        
        Debug.LogError($"Failed to get avatar from pool for prefab: {prefab?.name}");
        return null;
    }
    
    // Fallback method for backward compatibility
    public GameObject GetAvatar()
    {
        Debug.LogWarning("GetAvatar() called without prefab parameter. This is deprecated!");
        return null;
    }
    
    public void ReturnAvatar(GameObject avatar)
    {
        if (avatar == null) return;
        
        // Only release if it's actually from our pools
        if (!activeAvatars.Contains(avatar))
        {
            Debug.LogWarning("Trying to return avatar that's not from this pool!");
            return;
        }
        
        // Find which pool this avatar belongs to
        AvatarPoolReference poolRef = avatar.GetComponent<AvatarPoolReference>();
        if (poolRef == null || poolRef.originalPrefab == null)
        {
            Debug.LogError("Avatar missing pool reference! Cannot return to pool.");
            Destroy(avatar);
            return;
        }
        
        if (avatarPools.ContainsKey(poolRef.originalPrefab))
        {
            avatarPools[poolRef.originalPrefab].Release(avatar);
        }
        else
        {
            Debug.LogError($"Pool not found for prefab: {poolRef.originalPrefab.name}");
            Destroy(avatar);
        }
    }
    
    private void ResetAvatarState(GameObject avatar)
    {
        // Reset ChatAvatar component if it exists
        ChatAvatar chatAvatar = avatar.GetComponent<ChatAvatar>();
        if (chatAvatar != null)
        {
            chatAvatar.ResetAvatar();
        }
    }
    
    public int GetActiveCount()
    {
        return activeAvatars.Count;
    }
    
    public int GetInactiveCount()
    {
        int totalInactive = 0;
        foreach (var pool in avatarPools.Values)
        {
            totalInactive += pool.CountInactive;
        }
        return totalInactive;
    }
    
    public int GetTotalCount()
    {
        int totalCount = 0;
        foreach (var pool in avatarPools.Values)
        {
            totalCount += pool.CountAll;
        }
        return totalCount;
    }
    
    public void ClearPool()
    {
        // Return all active avatars to pool first
        var activeAvatarsCopy = new HashSet<GameObject>(activeAvatars);
        foreach (var avatar in activeAvatarsCopy)
        {
            ReturnAvatar(avatar);
        }
        
        // Clear all pools
        foreach (var pool in avatarPools.Values)
        {
            pool.Clear();
        }
        
        avatarPools.Clear();
        
        Debug.Log("All avatar pools cleared");
    }
    
    public Dictionary<string, int> GetPoolStats()
    {
        Dictionary<string, int> stats = new Dictionary<string, int>();
        
        foreach (var kvp in avatarPools)
        {
            string prefabName = kvp.Key.name;
            int count = kvp.Value.CountAll;
            stats[prefabName] = count;
        }
        
        return stats;
    }
    
    private void OnDestroy()
    {
        // Clean up pools when manager is destroyed
        foreach (var pool in avatarPools.Values)
        {
            pool?.Clear();
        }
        avatarPools.Clear();
    }
    
    // Debug info for inspector
    [System.Serializable]
    public struct PoolDebugInfo
    {
        public int activeCount;
        public int inactiveCount;
        public int totalCount;
        public int poolTypes;
    }
    
    [Header("Debug Info (Read Only)")]
    [SerializeField] private PoolDebugInfo debugInfo;
    
    private void Update()
    {
        // Update debug info in inspector
        debugInfo.activeCount = activeAvatars.Count;
        debugInfo.inactiveCount = GetInactiveCount();
        debugInfo.totalCount = GetTotalCount();
        debugInfo.poolTypes = avatarPools.Count;
    }
}

// Helper component to track which prefab an avatar instance came from
public class AvatarPoolReference : MonoBehaviour
{
    [HideInInspector]
    public GameObject originalPrefab;
}