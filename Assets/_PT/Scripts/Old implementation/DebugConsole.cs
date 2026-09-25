using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DebugConsole : MonoBehaviour
{
    private readonly List<string> logs = new();
    private Vector2 scrollPosition;
    private Vector2 avatarScrollPosition;
    // Removed showConsole variable - console will always be shown
    
    [Header("Avatar Management")]
    [SerializeField] private ChatAvatarManager avatarManager;
    [SerializeField] private int maxAvatarButtons = 20;
    
    [Header("UI Layout Parameters")]
    [SerializeField] private float panelMargin = 10f;
    [SerializeField] private float panelSpacing = 10f;
    [SerializeField] private float avatarPanelWidth = 170f;
    [SerializeField] private float consoleHeightRatio = 0.33f; // Fraction of screen height
    
    [Header("Avatar Panel Settings")]
    [SerializeField] private float buttonWidth = 150f;
    [SerializeField] private float buttonHeight = 25f;
    [SerializeField] private float buttonSpacing = 2f;
    [SerializeField] private int buttonFontSize = 16;
    [SerializeField] private float headerHeight = 20f;
    [SerializeField] private float infoSpacing = 15f;
    
    [Header("Avatar Options Popup")]
    [SerializeField] private float optionsWidth = 150f;
    [SerializeField] private float optionsButtonSpacing = 5f;
    [SerializeField] private float optionsPopupOffset = 10f;
    
    [Header("Console Panel Settings")]
    [SerializeField] private float logLineHeight = 20f;
    [SerializeField] private int logFontSize = 16;
    [SerializeField] private int maxLogEntries = 1000;
    [SerializeField] public bool showDebugConsole = false;

    // State for avatar options
    private ChatAvatar selectedAvatarObject = null;
    private bool showAvatarOptions = false;

    // Custom GUIStyle for the console logs
    private GUIStyle logStyle;

    private void Start()
    {
        // Find avatar manager if not assigned
        if (avatarManager == null)
        {
            avatarManager = FindObjectOfType<ChatAvatarManager>();
        }
       
    }

    public void ToggleConsole()
    {
        showDebugConsole = !showDebugConsole;
    }
    
    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }
    
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        logs.Add(logString);
        if (logs.Count > maxLogEntries) logs.RemoveAt(0);
    }

    private void OnGUI()
    {
        // Initialize the style only once for efficiency.
        if (logStyle == null)
        {
            logStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = logFontSize,
                wordWrap = true // Helps with long log messages
            };
        }
        
        float consolePanelX = avatarPanelWidth + panelMargin + panelSpacing;
        float consolePanelWidth = Screen.width - consolePanelX - panelMargin;

        // Avatar Management Panel
        

        if (showDebugConsole)
        {
            // Debug Console Panel
            
            DrawAvatarPanel();
            //DrawConsolePanel(consolePanelX, consolePanelWidth);
        }
       
        
        // Avatar Options Popup
        if (showAvatarOptions)
        {
            DrawAvatarOptions();
        }
    }
    
    private void DrawAvatarPanel()
    {
        float panelHeight = Screen.height * consoleHeightRatio;
        GUI.Box(new Rect(panelMargin, panelMargin, avatarPanelWidth, panelHeight), "Avatar Manager");
        
        // Get ALL avatar objects (including buggy ones)
        ChatAvatar[] allAvatars = GetAllAvatarObjects();
        
        // Count valid vs invalid avatars
        int validCount = allAvatars.Count(a => !string.IsNullOrEmpty(a.Username));
        int invalidCount = allAvatars.Length - validCount;
        
        // Info header with counts
        string headerText = $"Total: {allAvatars.Length} | Valid: {validCount}";
        if (invalidCount > 0)
        {
            headerText += $" | BUGGY: {invalidCount}";
        }
        
        GUI.Label(new Rect(panelMargin + 5, panelMargin + headerHeight + 5, avatarPanelWidth - 10, headerHeight), headerText);
        
        // Scrollable avatar list
        float scrollViewY = panelMargin + headerHeight + infoSpacing + headerHeight;
        float scrollViewHeight = panelHeight - (headerHeight + infoSpacing + headerHeight + 10);
        float contentHeight = Mathf.Max(allAvatars.Length * (buttonHeight + buttonSpacing), scrollViewHeight);
        
        avatarScrollPosition = GUI.BeginScrollView(
            new Rect(panelMargin, scrollViewY, avatarPanelWidth, scrollViewHeight),
            avatarScrollPosition,
            new Rect(0, 0, avatarPanelWidth - 20, contentHeight)
        );

        // Draw avatar buttons (limit to maxAvatarButtons)
        int buttonCount = Mathf.Min(allAvatars.Length, maxAvatarButtons);
        for (int i = 0; i < buttonCount; i++)
        {
            if (allAvatars[i] != null)
            {
                ChatAvatar avatar = allAvatars[i];
                string displayName = GetAvatarDisplayName(avatar);
                float buttonY = i * (buttonHeight + buttonSpacing);
                
                // Highlight selected avatar
                Color originalColor = GUI.backgroundColor;
                if (selectedAvatarObject == avatar)
                {
                    GUI.backgroundColor = Color.yellow;
                }
                
                // Color buggy avatars red
                bool isBuggy = string.IsNullOrEmpty(avatar.Username);
                if (isBuggy && selectedAvatarObject != avatar)
                {
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f); // Light red
                }
                
                GUIStyle avatarButtonStyle = new GUIStyle(GUI.skin.button);
                avatarButtonStyle.fontSize = buttonFontSize;
                
                if (GUI.Button(new Rect(5, buttonY, buttonWidth, buttonHeight), displayName, avatarButtonStyle))
                {
                    if (selectedAvatarObject == avatar)
                    {
                        // Toggle options if same avatar clicked
                        showAvatarOptions = !showAvatarOptions;
                    }
                    else
                    {
                        // Select new avatar
                        selectedAvatarObject = avatar;
                        showAvatarOptions = true;
                    }
                }
                
                GUI.backgroundColor = originalColor;
            }
        }
        
        if (allAvatars.Length > maxAvatarButtons)
        {
            float warningY = buttonCount * (buttonHeight + buttonSpacing);
            GUI.Label(new Rect(5, warningY, buttonWidth, buttonHeight), 
                     $"... +{allAvatars.Length - maxAvatarButtons} more", 
                     GUI.skin.box);
        }

        GUI.EndScrollView();
    }
    
    private ChatAvatar[] GetAllAvatarObjects()
    {
        // Find all ChatAvatar objects in the scene, regardless of their state
        ChatAvatar[] allAvatars = FindObjectsOfType<ChatAvatar>();
        
        // Sort them: buggy ones first, then by username
        return allAvatars.OrderBy(avatar => 
        {
            if (string.IsNullOrEmpty(avatar.Username))
                return "0_BUGGY_" + avatar.GetInstanceID(); // Buggy avatars first
            return "1_" + avatar.Username; // Then valid ones
        }).ToArray();
    }
    
    private string GetAvatarDisplayName(ChatAvatar avatar)
    {
        if (string.IsNullOrEmpty(avatar.Username))
        {
            return $"[BUGGY] ID:{avatar.GetInstanceID()}";
        }
        return avatar.Username;
    }
    
    private void DrawConsolePanel(float panelX, float panelWidth)
    {
        float panelHeight = Screen.height * consoleHeightRatio;
        GUI.Box(new Rect(panelX, panelMargin, panelWidth, panelHeight), "Debug Console");
            
        scrollPosition = GUI.BeginScrollView(
            new Rect(panelX, panelMargin + headerHeight + 5, panelWidth, panelHeight - headerHeight - 15),
            scrollPosition,
            new Rect(0, 0, panelWidth - 20, logs.Count * logLineHeight)
        );

        for (int i = 0; i < logs.Count; i++)
        {
            // Use the custom logStyle with the larger font size
            GUI.Label(new Rect(0, i * logLineHeight, panelWidth - 20, logLineHeight), logs[i], logStyle);
        }

        GUI.EndScrollView();
    }
    
    private void DrawAvatarOptions()
    {
        if (selectedAvatarObject == null) return;
        
        float optionsX = avatarPanelWidth + panelMargin + optionsPopupOffset;
        float optionsY = panelMargin + headerHeight + infoSpacing;
        float optionsHeight = buttonHeight * 3 + optionsButtonSpacing * 4 + 10;
        
        // Background box
        GUI.Box(new Rect(optionsX, optionsY, optionsWidth, optionsHeight), "");
        
        GUIStyle avatarButtonStyle = new GUIStyle(GUI.skin.button);
        avatarButtonStyle.fontSize = buttonFontSize;
        
        // Kill Avatar Button
        if (GUI.Button(new Rect(optionsX + optionsButtonSpacing, optionsY + optionsButtonSpacing, 
                               optionsWidth - optionsButtonSpacing * 2, buttonHeight), "Kill Avatar", avatarButtonStyle))
        {
            KillAvatarObject(selectedAvatarObject);
            showAvatarOptions = false;
            selectedAvatarObject = null;
        }
        
        // Reroll Avatar Button (only for valid avatars)
        bool canReroll = !string.IsNullOrEmpty(selectedAvatarObject.Username);
        GUI.enabled = canReroll;
        
        if (GUI.Button(new Rect(optionsX + optionsButtonSpacing, 
                               optionsY + buttonHeight + optionsButtonSpacing * 2, 
                               optionsWidth - optionsButtonSpacing * 2, buttonHeight), 
                      canReroll ? "Reroll Avatar" : "Can't Reroll Buggy",avatarButtonStyle))
        {
            RerollAvatarObject(selectedAvatarObject);
            showAvatarOptions = false;
            selectedAvatarObject = null;
        }
        GUI.enabled = true;
        
        // Force Destroy Button (for really stuck objects)
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f); // Red background
        if (GUI.Button(new Rect(optionsX + optionsButtonSpacing, 
                               optionsY + (buttonHeight * 2) + optionsButtonSpacing * 3, 
                               optionsWidth - optionsButtonSpacing * 2, buttonHeight), "Force Destroy", avatarButtonStyle))
        {
            ForceDestroyAvatar(selectedAvatarObject);
            showAvatarOptions = false;
            selectedAvatarObject = null;
        }
        GUI.backgroundColor = originalColor;
    }
    
    private void KillAvatarObject(ChatAvatar avatar)
    {
        if (avatarManager == null)
        {
            Debug.LogError("AvatarManager not found!");
            return;
        }
        
        string avatarName = string.IsNullOrEmpty(avatar.Username) ? $"Buggy Avatar ID:{avatar.GetInstanceID()}" : avatar.Username;
        
        // Try to use the manager's RemoveAvatar method for valid avatars
        if (!string.IsNullOrEmpty(avatar.Username))
        {
            avatarManager.RemoveAvatar(avatar.Username);
        }
        else
        {
            // For buggy avatars, try to destroy directly
            Debug.LogWarning($"Attempting to destroy buggy avatar directly: {avatarName}");
            if (avatar != null)
            {
                DestroyImmediate(avatar.gameObject);
            }
        }
        
        Debug.Log($"Killed avatar: {avatarName}");
    }
    
    private void RerollAvatarObject(ChatAvatar avatar)
    {
        if (avatarManager == null || string.IsNullOrEmpty(avatar.Username))
        {
            Debug.LogError("Cannot reroll: AvatarManager not found or avatar has no username!");
            return;
        }
        
        // Use the existing RerollAvatar method from ChatAvatarManager
        avatarManager.RerollAvatar(avatar.Username);
        Debug.Log($"Rerolled avatar for {avatar.Username}");
    }
    
    private void ForceDestroyAvatar(ChatAvatar avatar)
    {
        string avatarName = string.IsNullOrEmpty(avatar.Username) ? $"Buggy Avatar ID:{avatar.GetInstanceID()}" : avatar.Username;
        
        Debug.LogWarning($"Force destroying avatar: {avatarName}");
        
        if (avatar != null && avatar.gameObject != null)
        {
            DestroyImmediate(avatar.gameObject);
        }
        
        Debug.Log($"Force destroyed avatar: {avatarName}");
    }
}