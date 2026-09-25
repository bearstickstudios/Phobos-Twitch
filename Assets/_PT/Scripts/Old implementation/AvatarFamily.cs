using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AvatarFamily
{
    public string familyName;
    public float strength;
    [Range(0f, 100f)]
    public float spawnChance;
    public List<AvatarVariant> variants = new List<AvatarVariant>();
}