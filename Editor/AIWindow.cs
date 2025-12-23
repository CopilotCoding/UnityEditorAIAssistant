using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Editor window for interacting with the AI Assistant plugin.
/// Fully preserves all existing functionality, scroll behavior, history, and script saving.
/// </summary>
public class AIWindow : EditorWindow
{
    private string prompt = "";
    private Vector2 promptScrollPos;

    private string response = "";
    private Vector2 responseScrollPos;

    private string error = "";

    private List<(string prompt, string response)> history = new();
    private Vector2 historyScrollPos;

    private List<string> selectedContext = new();
    private Vector2 contextScrollPos;

    private bool isLoading = false;
    private bool showHistory = true;

    [MenuItem("AI Tools/AI Assistant")]
    public static void ShowWindow()
    {
        GetWindow<AIWindow>("AI Assistant");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        // Title
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };
        GUILayout.Label("🤖 AI Assistant", titleStyle);
        GUILayout.Space(10);

        // Refresh Project Index
        if (GUILayout.Button("Refresh Project Index", GUILayout.Height(30)))
        {
            ProjectIndexer.Refresh();
        }

        GUILayout.Space(5);

        // Error display
        if (!string.IsNullOrEmpty(error))
        {
            GUI.color = Color.red;
            EditorGUILayout.HelpBox(error, MessageType.Error);
            GUI.color = Color.white;
            GUILayout.Space(5);
        }

        // Project Context
        // Project Context
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Project Context (select for AI awareness)", EditorStyles.boldLabel);

        // Select/Deselect All checkbox
        bool newSelectAll = EditorGUILayout.ToggleLeft("Select/Deselect All", selectedContext.Count == ProjectIndexer.GetIndex().Count);
        if (newSelectAll != (selectedContext.Count == ProjectIndexer.GetIndex().Count))
        {
            if (newSelectAll)
                selectedContext = new List<string>(ProjectIndexer.GetIndex()); // select all
            else
                selectedContext.Clear(); // deselect all
        }

        contextScrollPos = EditorGUILayout.BeginScrollView(contextScrollPos, GUILayout.Height(100));
        foreach (var entry in ProjectIndexer.GetIndex())
        {
            bool selected = selectedContext.Contains(entry);
            bool newSelected = EditorGUILayout.ToggleLeft(entry, selected);
            if (newSelected && !selected) selectedContext.Add(entry);
            else if (!newSelected && selected) selectedContext.Remove(entry);
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Prompt input
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Prompt", EditorStyles.boldLabel);
        promptScrollPos = EditorGUILayout.BeginScrollView(promptScrollPos, GUILayout.Height(120));
        prompt = EditorGUILayout.TextArea(prompt, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUI.BeginDisabledGroup(isLoading || string.IsNullOrWhiteSpace(prompt));
        if (GUILayout.Button(isLoading ? "Sending..." : "Send", GUILayout.Height(35)))
        {
            _ = HandlePrompt(prompt);
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // AI Response
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Response", EditorStyles.boldLabel);
        responseScrollPos = EditorGUILayout.BeginScrollView(responseScrollPos, GUILayout.Height(200));
        EditorGUILayout.TextArea(response, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        if (!string.IsNullOrEmpty(response))
        {
            if (GUILayout.Button("Save Response as Script"))
            {
                string path = EditorUtility.SaveFilePanel("Save Script", "Assets/Scripts", "NewScript.cs", "cs");
                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllText(path, response);
                    AssetDatabase.Refresh();
                }
            }
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // History
        showHistory = EditorGUILayout.Foldout(showHistory, "History", true);
        if (showHistory)
        {
            EditorGUILayout.BeginVertical("box");
            historyScrollPos = EditorGUILayout.BeginScrollView(historyScrollPos, GUILayout.Height(200));

            GUIStyle style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            foreach (var entry in history)
            {
                string entryText = $"Prompt:\n{entry.prompt}\n\nResponse:\n{entry.response}";
                EditorGUILayout.TextArea(entryText, style, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                GUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }

    private async Task HandlePrompt(string userPrompt)
    {
        isLoading = true;
        error = "";
        response = "Sending...";
        Repaint();

        try
        {
            string context = string.Join("\n", selectedContext);
            string rawResult = await RequestManager.SendPromptAsync(userPrompt, context);

            if (rawResult.StartsWith("[Error]"))
            {
                error = rawResult;
                response = "";
            }
            else
            {
                response = rawResult;
                history.Add((userPrompt, response));
            }
        }
        catch (Exception ex)
        {
            error = "[Error] Exception: " + ex.Message;
            response = "";
        }
        finally
        {
            isLoading = false;
            Repaint();
        }
    }

    public async Task SendExplainPrompt(string name, string content)
    {
        prompt = $"Explain the following script: {name}\n\n{content}";
        await HandlePrompt(prompt);
    }

    [MenuItem("Assets/AI Assistant/Explain Script", true)]
    private static bool ExplainScriptValidate()
    {
        return Selection.activeObject is MonoScript;
    }

    [MenuItem("Assets/AI Assistant/Explain Script")]
    private static void ExplainScript()
    {
        var script = Selection.activeObject as MonoScript;
        string content = File.ReadAllText(AssetDatabase.GetAssetPath(script));

        AIWindow.ShowWindow();
        var window = GetWindow<AIWindow>();
        _ = window.SendExplainPrompt(script.name, content);
    }
}
