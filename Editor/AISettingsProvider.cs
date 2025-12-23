using UnityEditor;

/// <summary>
/// Provides a Unity Editor Settings page for the AI Assistant plugin.
/// This allows users to store and edit their OpenAI API key directly in the Editor preferences.
/// </summary>
public class AISettingsProvider
{
    /// <summary>
    /// Creates a SettingsProvider that appears in Unity's Preferences window under "Preferences/AI Assistant".
    /// This SettingsProvider allows the user to input and save their OpenAI API key.
    /// </summary>
    /// <returns>A SettingsProvider instance configured for the AI Assistant settings.</returns>
    [SettingsProvider]
    public static SettingsProvider CreateAISettingsProvider()
    {
        // Create a new SettingsProvider in the "Preferences" section under "AI Assistant"
        var provider = new SettingsProvider("Preferences/AI Assistant", SettingsScope.User)
        {
            // The label shown at the top of the preferences panel
            label = "AI Assistant",

            // GUI handler that draws the custom settings UI
            guiHandler = (searchContext) =>
            {
                // Retrieve the stored API key from EditorPrefs; default to empty string if not set
                string apiKey = EditorPrefs.GetString("AI_API_KEY", "");

                // Begin checking for changes in the GUI
                EditorGUI.BeginChangeCheck();

                // Draw a text field for the API key input
                apiKey = EditorGUILayout.TextField("API Key", apiKey);

                // If the value was changed by the user, save it back to EditorPrefs
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetString("AI_API_KEY", apiKey);
                }
            }
        };

        return provider;
    }
}
