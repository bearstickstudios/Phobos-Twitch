using UnityEngine;

[System.Serializable]
public class AvatarVariant
{
    public string variantName;
    public GameObject prefab;
    [Range(0f, 100f)]
    public float variantChance;
}