using UnityEngine;
using UnityEditor;

public static class EditorLogger
{
    // Static variable to control logging
    private static bool isLoggingEnabled = true;

    public static bool IsLoggingEnabled => isLoggingEnabled; // Property to access the logging state

    // Method to toggle logging
    public static void ToggleLogging(bool enable)
    {
        isLoggingEnabled = enable;
    }

    // Method to log an error message with optional color
    public static void LogError(string message, Color? color = null)
    {
#if UNITY_EDITOR
        if (!isLoggingEnabled) return;

        string coloredMessage = color.HasValue ? $"<color=#{ColorUtility.ToHtmlStringRGB(color.Value)}>{message}</color>" : message;
        Debug.LogError(coloredMessage);
#endif
    }

    // Method to log a warning message with optional color
    public static void LogWarning(string message, Color? color = null)
    {
#if UNITY_EDITOR
        if (!isLoggingEnabled) return;

        string coloredMessage = color.HasValue ? $"<color=#{ColorUtility.ToHtmlStringRGB(color.Value)}>{message}</color>" : message;
        Debug.LogWarning(coloredMessage);
#endif
    }

    // Method to log a regular message with optional color
    public static void Log(string message, Color? color = null)
    {
#if UNITY_EDITOR
        if (!isLoggingEnabled) return;

        string coloredMessage = color.HasValue ? $"<color=#{ColorUtility.ToHtmlStringRGB(color.Value)}>{message}</color>" : message;
        Debug.Log(coloredMessage);
#endif
    }
}
#if UNITY_EDITOR
[InitializeOnLoad]
public static class ToolbarButton
{
    static bool isLoggingEnabled = EditorLogger.IsLoggingEnabled;

    // Static constructor to add the toolbar button when Unity starts up
    static ToolbarButton()
    {
        // Register the callback to draw the toolbar button
        SceneView.duringSceneGui += AddToolbarButton;
    }

    // Method to add the toolbar button to the SceneView
    private static void AddToolbarButton(SceneView sceneView)
    {
        Handles.BeginGUI();

        // Add left padding to the button by adjusting the X position
        float buttonWidth = 200f;
        float buttonHeight = 40f;
        float paddingLeft = 40f;  // Padding from the left edge of the screen
        float buttonX = paddingLeft;  // Set the button's X position to the padding value
        float buttonY = 10f;  // Positioned 10 pixels from the top

        GUILayout.BeginArea(new Rect(buttonX, buttonY, buttonWidth, buttonHeight));

        // Toggle button to enable/disable logging
        bool newLoggingState = GUILayout.Toggle(isLoggingEnabled, "Enable Logging");
        if (newLoggingState != isLoggingEnabled)
        {
            isLoggingEnabled = newLoggingState;
            EditorLogger.ToggleLogging(isLoggingEnabled);
        }

        GUILayout.EndArea();
        Handles.EndGUI();
    }
}
#endif