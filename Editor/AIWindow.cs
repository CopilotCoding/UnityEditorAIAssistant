using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;

public class AIWindow : EditorWindow
{
    private string prompt = "";
    private Vector2 scrollPos;
    private string response = "";
    private string error = "";

    [MenuItem("Window/AI Assistant")]
    public static void ShowWindow()
    {
        GetWindow<AIWindow>("AI Assistant");
    }

    private void OnGUI()
    {
        GUILayout.Label("AI Assistant", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh Project Index"))
        {
            ProjectIndexer.Refresh();
        }

        GUILayout.Space(10);

        // Error panel (red box)
        if (!string.IsNullOrEmpty(error))
        {
            GUI.color = Color.red;
            GUILayout.Box("Error: " + error, GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(5);
        }

        // Response output (selectable)
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(600));
        response = EditorGUILayout.TextArea(response, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        // Copy to clipboard button
        if (!string.IsNullOrEmpty(response))
        {
            if (GUILayout.Button("Copy Response to Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = response;
                Debug.Log("AI response copied to clipboard.");
            }
        }

        GUILayout.Space(10);

        // Prompt input
        prompt = EditorGUILayout.TextField("Prompt:", prompt);

        if (GUILayout.Button("Send"))
        {
            _ = HandlePrompt(prompt);
        }
    }

    private async Task HandlePrompt(string userPrompt)
    {
        response = "Sending...";
        error = "";
        Repaint();

        string context = string.Join("\n", ProjectIndexer.GetIndex());
        string rawResult = await RequestManager.SendPromptAsync(userPrompt, context);

        if (rawResult.StartsWith("[Error]"))
        {
            error = rawResult;
            response = "";
        }
        else
        {
            response = rawResult;
        }

        Repaint();
    }
}
