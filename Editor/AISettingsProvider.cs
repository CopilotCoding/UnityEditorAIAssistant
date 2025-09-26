using UnityEditor;
using UnityEngine;

public class AISettingsProvider
{
    [SettingsProvider]
    public static SettingsProvider CreateAISettingsProvider()
    {
        var provider = new SettingsProvider("Preferences/AI Assistant", SettingsScope.User)
        {
            label = "AI Assistant",
            guiHandler = (searchContext) =>
            {
                string apiKey = EditorPrefs.GetString("AI_API_KEY", "");
                EditorGUI.BeginChangeCheck();
                apiKey = EditorGUILayout.TextField("API Key", apiKey);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetString("AI_API_KEY", apiKey);
                }
            }
        };
        return provider;
    }
}
