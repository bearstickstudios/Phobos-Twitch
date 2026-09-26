using UnityEngine;
using TMPro;
namespace PhobosTwitch
{
    public class StreamTitleUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI streamTitleText;
        [SerializeField] private TwitchAuthManager authManager;

        private void Awake()
        {
            authManager.OnStreamTitleFetched += UpdateTitle;
        }

        private void OnDestroy()
        {
            authManager.OnStreamTitleFetched -= UpdateTitle;
        }

        private void UpdateTitle(string title)
        {
            streamTitleText.text = title;
        }
    }
}