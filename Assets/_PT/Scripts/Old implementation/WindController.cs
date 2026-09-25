using UnityEngine;

public class WindController : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float windSpeed = 0.5f; // Controls how fast wind changes
    [SerializeField] private string blendParameter = "WindBlend";   // Name of your blend tree parameter
    private float timeOffset;

    void Start()
    {
        // Randomize offset so multiple grass objects don't sway identically
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Simulate wind using Perlin noise for smooth randomness
        float windValue = Mathf.PerlinNoise(Time.time * windSpeed + timeOffset, 0f);
        
        // Optionally smooth it further or clamp
        windValue = Mathf.Clamp01(windValue);

        // Apply to animator
        var animator = GetComponent<Animator>();
        animator.SetFloat(blendParameter, windValue);
    }
}