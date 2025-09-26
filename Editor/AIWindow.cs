using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// Unity Editor window for interacting with the AI Assistant plugin.
/// Allows sending prompts, receiving responses, and viewing project context.
/// </summary>
public class AIWindow : EditorWindow
{
    // Current prompt text input by the user
    private string prompt = "";

    // Scroll position for the response scroll view
    private Vector2 scrollPos;

    // AI-generated response text
    private string response = "";

    // Any error messages from the AI request
    private string error = "";

    /// <summary>
    /// Adds a menu item under "Window/AI Assistant" to open this editor window.
    /// </summary>
    [MenuItem("Window/AI Assistant")]
    public static void ShowWindow()
    {
        GetWindow<AIWindow>("AI Assistant");
    }

    /// <summary>
    /// Draws the GUI for the AI assistant window.
    /// Handles input fields, buttons, response display, and error messages.
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Label("AI Assistant", EditorStyles.boldLabel);

        // Button to refresh the project index for AI context
        if (GUILayout.Button("Refresh Project Index"))
        {
            ProjectIndexer.Refresh();
        }

        GUILayout.Space(10);

        // Display an error box if an error occurred during the last request
        if (!string.IsNullOrEmpty(error))
        {
            GUI.color = Color.red;
            GUILayout.Box("Error: " + error, GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(5);
        }

        // Response output in a scrollable text area
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(600)); // Adjustable height
        response = EditorGUILayout.TextArea(response, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        // Button to copy the AI response to the system clipboard
        if (!string.IsNullOrEmpty(response))
        {
            if (GUILayout.Button("Copy Response to Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = response;
                Debug.Log("AI response copied to clipboard.");
            }
        }

        GUILayout.Space(10);

        // Text field for entering the AI prompt
        prompt = EditorGUILayout.TextField("Prompt:", prompt);

        // Button to send the prompt to the AI
        if (GUILayout.Button("Send"))
        {
            _ = HandlePrompt(prompt);
        }
    }

    /// <summary>
    /// Handles sending the user's prompt to the AI and updating the response and error fields.
    /// </summary>
    /// <param name="userPrompt">The prompt entered by the user.</param>
    private async Task HandlePrompt(string userPrompt)
    {
        // Show sending state
        response = "Sending...";
        error = "";
        Repaint();

        // Combine project index into a context string for AI awareness
        string context = string.Join("\n", ProjectIndexer.GetIndex());

        // Send the prompt asynchronously to the AI
        string rawResult = await RequestManager.SendPromptAsync(userPrompt, context);

        // Check if the response indicates an error
        if (rawResult.StartsWith("[Error]"))
        {
            error = rawResult;
            response = "";
        }
        else
        {
            response = rawResult;
        }

        // Refresh the window to display updated response or error
        Repaint();
    }
}
