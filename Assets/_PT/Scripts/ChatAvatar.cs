using TMPro;
using UnityEngine;

namespace PhobosTwitch
{
    public class ChatAvatar : MonoBehaviour
    {
        [Header("Suit Systems")]
        public float currentCharge = 0f;
        public float overloadThreshold = 25f;

        [Header("References")]
        public GameObject maintenanceToolPrefab; // Formerly handObject
        public TextMeshPro toastText;
        public Renderer suitRenderer;
        public ParticleSystem coolantVFX;

        private MaterialPropertyBlock propBlock;
        private readonly int emissionColorID = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            propBlock = new MaterialPropertyBlock();
        }

        // Formerly EatingSequence
        public void DataExtractionSequence(float dataAmount)
        {
            currentCharge += dataAmount;
        
            // Trigger Scan/Extract animation via BlendShapeController here

            if (currentCharge >= overloadThreshold)
            {
                OnSystemOverload();
            }
        }

        // Formerly OnVomited
        private void OnSystemOverload()
        {
            currentCharge = 0f; // Reset core capacity
            ShowToast("Overheat! Core Reset");
        
            if (coolantVFX != null)
            {
                coolantVFX.Play(); // Venting particles
            }
        }

        // Formerly OnPetted
        public void OnMaintained()
        {
            // Lower charge to prevent overload
            currentCharge = Mathf.Max(0, currentCharge - 3f);
            ShowToast("Venting Pressure -3");
        
            // Spawn robotic arm or wrench above the avatar
            if (maintenanceToolPrefab != null)
            {
                Instantiate(maintenanceToolPrefab, transform.position + (Vector3.up * 1.5f), Quaternion.identity, transform);
            }
        }

        private void ShowToast(string message)
        {
            if (toastText != null)
            {
                toastText.text = message;
                toastText.gameObject.SetActive(true);
                // Include your existing text fade-out/pooling logic here
            }
        }

        // Replaces full body tinting with targeted URP Emissive adjustments
        public void ApplyUniqueColor(Color baseColor)
        {
            suitRenderer.GetPropertyBlock(propBlock);
        
            // Multiply color for HDR emission intensity
            propBlock.SetColor(emissionColorID, baseColor * 2f); 
            suitRenderer.SetPropertyBlock(propBlock);
        }

        // Applies Twitch role-based LED colors
        public void ApplyBadgeEffects(bool isBroadcaster, bool isMod, bool isVIP)
        {
            suitRenderer.GetPropertyBlock(propBlock);
            Color ledColor = Color.white; // Default Viewer

            if (isBroadcaster) ledColor = Color.yellow; // Gold
            else if (isMod) ledColor = Color.green;
            else if (isVIP) ledColor = Color.magenta;

            // Higher intensity for badged users
            propBlock.SetColor(emissionColorID, ledColor * 3.5f); 
            suitRenderer.SetPropertyBlock(propBlock);
        }
    }
}