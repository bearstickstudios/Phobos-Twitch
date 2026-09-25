using UnityEngine;

[CreateAssetMenu(fileName = "New UserData", menuName = "Chat/User Data")]
public class UserData : ScriptableObject
{
    public string username;
    public Color avatarColor;
}