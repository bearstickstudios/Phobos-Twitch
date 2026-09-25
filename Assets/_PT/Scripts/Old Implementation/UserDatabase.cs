// UserDatabase.cs
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "UserDatabase", menuName = "Chat/User Database")]
public class UserDatabase : ScriptableObject
{
    // We use a List instead of a Dictionary for serialization.
    public List<UserData> users = new List<UserData>();

    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, "userDatabase.json");
    }

    public UserData GetUserData(string username)
    {
        // Find a user in the list by their username (case-insensitive).
        return users.FirstOrDefault(u => u.username.Equals(username, System.StringComparison.InvariantCultureIgnoreCase));
    }

    public UserData CreateNewUser(string username)
    {
        var existingUser = GetUserData(username);
        if (existingUser != null)
        {
            return existingUser; // Don't create duplicates.
        }

        // Create a new instance of the UserData ScriptableObject.
        UserData newUser = CreateInstance<UserData>();
        newUser.name = username; // name is an inherited property from ScriptableObject
        newUser.username = username;

        // Generate and save the persistent random color.
        Random.InitState(username.GetHashCode());
        newUser.avatarColor = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.9f, 1f);

        users.Add(newUser);
        Debug.Log($"Created new data for user '{username}' with color {newUser.avatarColor}");
        
        // Save the updated database to a file.
        SaveDatabase();

        return newUser;
    }

    public void SaveDatabase()
    {
        // JsonUtility can't serialize a list directly, so we use a wrapper.
        UserListWrapper wrapper = new UserListWrapper { userList = this.users };
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(GetSavePath(), json);
        Debug.Log("User database saved to file.");
    }

    public void LoadDatabase()
    {
        string path = GetSavePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            UserListWrapper wrapper = JsonUtility.FromJson<UserListWrapper>(json);
            
            // Clear current users and load from the file.
            users.Clear();
            foreach (var userData in wrapper.userList)
            {
                UserData user = CreateInstance<UserData>();
                user.username = userData.username;
                user.avatarColor = userData.avatarColor;
                users.Add(user);
            }
            Debug.Log($"Loaded {users.Count} users from database.");
        }
    }
}

// A helper class to allow JsonUtility to serialize our list.
[System.Serializable]
public class UserListWrapper
{
    public List<UserData> userList;
}