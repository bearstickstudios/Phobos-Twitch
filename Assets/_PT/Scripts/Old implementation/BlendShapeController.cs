using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlendShapeController : MonoBehaviour
{
    [Header("Blend Shape Names")]
    public string mouthBlendShapeName = "mouth";
    public string earsBlendShapeName = "ears";

    [Header("Components")]
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    private Mesh characterMesh;

    // Blend shape indices
    private int mouthBlendShapeIndex;
    private int earsBlendShapeIndex;

    // Current blend shape strengths (0-1 range)
    private float currentMouthStrength = 0;  // 0 = open, 1 = closed
    private float currentEarsStrength = 0;   // 0 = normal position, 1 = pulled back

    void Start()
    {
        InitializeBlendShapeSystem();
    }

    private void InitializeBlendShapeSystem()
    {
        // If not assigned in inspector, try to get it from this GameObject
        if (skinnedMeshRenderer == null)
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

        if (skinnedMeshRenderer != null)
        {
            // Get mesh
            characterMesh = skinnedMeshRenderer.sharedMesh;
            if (characterMesh != null)
            {
                // Cache blend shape indices for performance
                if (mouthBlendShapeName != null)
                {
                    mouthBlendShapeIndex = characterMesh.GetBlendShapeIndex(mouthBlendShapeName);
                    // Validate blend shapes exist
                    if (mouthBlendShapeIndex != null && mouthBlendShapeIndex < 0)
                        Debug.Log($"Blend shape '{mouthBlendShapeName}' not found on mesh!");
                }
                if (earsBlendShapeName != null)
                {
                    earsBlendShapeIndex = characterMesh.GetBlendShapeIndex(earsBlendShapeName);
                    if (earsBlendShapeIndex != null && earsBlendShapeIndex < 0)
                        Debug.Log($"Blend shape '{earsBlendShapeName}' not found on mesh!");
                }
            }
        }
        else
        {
            Debug.LogError("No SkinnedMeshRenderer found! Please assign one in the inspector.");
        }
    }

    /// <summary>
    /// Controls mouth opening/closing
    /// </summary>
    /// <param name="targetValue">0 = fully open, 100 = fully closed</param>
    /// <param name="duration">Animation duration in seconds</param>
    public void SetMouthPosition(float targetValue, float duration = 0.5f)
    {
        targetValue = Mathf.Clamp(targetValue, 0f, 100f);
        StartCoroutine(AnimateBlendShape(mouthBlendShapeIndex, targetValue, duration));
    }

    /// <summary>
    /// Controls ear positioning
    /// </summary>
    /// <param name="targetValue">0 = normal position, 100 = pulled back</param>
    /// <param name="duration">Animation duration in seconds</param>
    public void SetEarPosition(float targetValue, float duration = 0.5f)
    {
        targetValue = Mathf.Clamp(targetValue, 0f, 100f);
        StartCoroutine(AnimateBlendShape(earsBlendShapeIndex, targetValue, duration));
    }

    /// <summary>
    /// Sets both mouth and ear positions simultaneously
    /// </summary>
    /// <param name="mouthValue">Mouth position (0-100)</param>
    /// <param name="earsValue">Ears position (0-100)</param>
    /// <param name="duration">Animation duration in seconds</param>
    public void SetBothBlendShapes(float mouthValue, float earsValue, float duration = 0.5f)
    {
        StartCoroutine(AnimateBothBlendShapes(mouthValue, earsValue, duration));
    }

    /// <summary>
    /// Quick preset animations
    /// </summary>
    public void OpenMouth() => SetMouthPosition(0f);
    public void CloseMouth() => SetMouthPosition(100f);
    public void WiggleEarsBack() => SetEarPosition(100f);
    public void ResetEars() => SetEarPosition(0f);

    /// <summary>
    /// Creates an ear wiggle animation
    /// </summary>
    /// <param name="wiggleIntensity">How far back to pull ears (0-100)</param>
    /// <param name="wiggleSpeed">Speed of the wiggle</param>
    public void WiggleEars(float wiggleIntensity = 50f, float wiggleSpeed = 0.3f)
    {
        StartCoroutine(EarWiggleAnimation(wiggleIntensity, wiggleSpeed));
    }

    /// <summary>
    /// Performs eating animation with specified number of chews
    /// </summary>
    /// <param name="chewCount">Number of times to chew</param>
    /// <param name="chewSpeed">Speed of each chew cycle</param>
    /// <returns>Total duration of eating animation</returns>
    public IEnumerator EatAnimation(int chewCount = 3, float chewSpeed = 0.3f)
    {
        if (mouthBlendShapeIndex < 0) yield break;

        float originalPosition = skinnedMeshRenderer.GetBlendShapeWeight(mouthBlendShapeIndex);
        
        // Perform chewing cycles
        for (int i = 0; i < chewCount; i++)
        {
            // Close mouth (chew down)
            yield return StartCoroutine(AnimateBlendShape(mouthBlendShapeIndex, 100f, chewSpeed));
            // Open mouth (chew up)  
            yield return StartCoroutine(AnimateBlendShape(mouthBlendShapeIndex, 0f, chewSpeed));
        }
        
        // Return to original position
        yield return StartCoroutine(AnimateBlendShape(mouthBlendShapeIndex, originalPosition, chewSpeed));
    }

    /// <summary>
    /// Gets the total duration for eating animation
    /// </summary>
    /// <param name="chewCount">Number of chews</param>
    /// <param name="chewSpeed">Speed per chew cycle</param>
    /// <returns>Total eating duration in seconds</returns>
    public float GetEatingDuration(int chewCount = 3, float chewSpeed = 0.3f)
    {
        // Each chew cycle = close + open (2 animations)
        // Plus final return to original position
        return (chewCount * 2 + 1) * chewSpeed;
    }

    /// <summary>
    /// Animates a single blend shape to target value
    /// </summary>
    private IEnumerator AnimateBlendShape(int blendShapeIndex, float targetValue, float duration)
    {
        if (blendShapeIndex < 0) yield break;

        float elapsedTime = 0;
        float startingValue = skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex);

        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            float currentValue = Mathf.Lerp(startingValue, targetValue, progress);
            
            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, currentValue);

            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        // Ensure final value is set exactly
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, targetValue);
    }

    /// <summary>
    /// Animates both blend shapes simultaneously
    /// </summary>
    private IEnumerator AnimateBothBlendShapes(float targetMouth, float targetEars, float duration)
    {
        float elapsedTime = 0;
        
        float startingMouth = mouthBlendShapeIndex >= 0 ? skinnedMeshRenderer.GetBlendShapeWeight(mouthBlendShapeIndex) : 0;
        float startingEars = earsBlendShapeIndex >= 0 ? skinnedMeshRenderer.GetBlendShapeWeight(earsBlendShapeIndex) : 0;

        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            
            if (mouthBlendShapeIndex >= 0)
            {
                float currentMouth = Mathf.Lerp(startingMouth, targetMouth, progress);
                skinnedMeshRenderer.SetBlendShapeWeight(mouthBlendShapeIndex, currentMouth);
            }
            
            if (earsBlendShapeIndex >= 0)
            {
                float currentEars = Mathf.Lerp(startingEars, targetEars, progress);
                skinnedMeshRenderer.SetBlendShapeWeight(earsBlendShapeIndex, currentEars);
            }

            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        // Set final values
        if (mouthBlendShapeIndex >= 0)
            skinnedMeshRenderer.SetBlendShapeWeight(mouthBlendShapeIndex, targetMouth);
        if (earsBlendShapeIndex >= 0)
            skinnedMeshRenderer.SetBlendShapeWeight(earsBlendShapeIndex, targetEars);
    }

    /// <summary>
    /// Creates a wiggle animation for ears
    /// </summary>
    private IEnumerator EarWiggleAnimation(float intensity, float speed)
    {
        if (earsBlendShapeIndex < 0) yield break;

        float originalPosition = skinnedMeshRenderer.GetBlendShapeWeight(earsBlendShapeIndex);
        
        // Wiggle back and forth a few times
        for (int i = 0; i < 3; i++)
        {
            // Move to wiggle position
            yield return StartCoroutine(AnimateBlendShape(earsBlendShapeIndex, intensity, speed));
            // Return to original
            yield return StartCoroutine(AnimateBlendShape(earsBlendShapeIndex, originalPosition, speed));
        }
    }

    void Update()
    {
        UpdateCurrentStrengths();
    }

    /// <summary>
    /// Updates the current strength values for easy access
    /// </summary>
    private void UpdateCurrentStrengths()
    {
        if (skinnedMeshRenderer != null)
        {
            // Convert blend shape weights (0-100) to normalized values (0-1)
            if (mouthBlendShapeIndex >= 0)
                currentMouthStrength = skinnedMeshRenderer.GetBlendShapeWeight(mouthBlendShapeIndex) / 100f;

            if (earsBlendShapeIndex >= 0)
                currentEarsStrength = skinnedMeshRenderer.GetBlendShapeWeight(earsBlendShapeIndex) / 100f;
        }
    }

    /// <summary>
    /// Get the current strength of a specific blend shape (0-1 range)
    /// </summary>
    public float GetBlendShapeStrength(string blendShapeType)
    {
        switch (blendShapeType.ToLower())
        {
            case "mouth": return currentMouthStrength;
            case "ears": return currentEarsStrength;
            default: return 0f;
        }
    }

    /// <summary>
    /// Get current mouth openness (0 = open, 1 = closed)
    /// </summary>
    public float GetMouthOpenness() => currentMouthStrength;

    /// <summary>
    /// Get current ear position (0 = normal, 1 = pulled back)
    /// </summary>
    public float GetEarPosition() => currentEarsStrength;

    /// <summary>
    /// Check if mouth is open (less than 10% closed)
    /// </summary>
    public bool IsMouthOpen() => currentMouthStrength < 0.1f;

    /// <summary>
    /// Check if mouth is closed (more than 90% closed)
    /// </summary>
    public bool IsMouthClosed() => currentMouthStrength > 0.9f;
}