using UnityEngine;

namespace PhobosTwitch
{
    public class ChatAvatarManager : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject astronautPrefab;

        /// <summary>
        /// Bypasses previous biological variant selection to always spawn the astronaut.
        /// Viewer data is retained for initializing specific loadouts or badges.
        /// </summary>
        public GameObject GetAvatarPrefab(string viewerName, string viewerTier)
        {
            // Spawns the standard astronaut chassis for all users
            return astronautPrefab;
        }
    }
}