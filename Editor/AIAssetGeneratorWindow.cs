using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class AIAssetGeneratorWindow : EditorWindow
{
    private string textPrompt = "";
    private string optionalParameters = "{}";
    private string manualJsonInput = "{}";

    private Vector2 promptScroll;
    private Vector2 paramScroll;
    private Vector2 jsonScroll;
    private Vector2 logScroll;

    private string generationLog = "";
    private bool showLog = true;
    private bool useManualJson = false;

    private enum GenerationMode { Asset, Scene, FullGame }
    private GenerationMode selectedMode = GenerationMode.Asset;

    [MenuItem("AI Tools/AI Asset Generator")]
    public static void ShowWindow() => GetWindow<AIAssetGeneratorWindow>("AI Asset Generator");

    private void OnGUI()
    {
        try
        {
            GUILayout.Space(10);
            GUILayout.Label("AI Asset Generator", new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(15);

            // Mode selection
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Generation Type:", GUILayout.Width(120));
            selectedMode = (GenerationMode)EditorGUILayout.EnumPopup(selectedMode);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Toggle manual JSON input
            useManualJson = EditorGUILayout.Toggle("Use Manual JSON Input", useManualJson);
            GUILayout.Space(10);

            if (useManualJson)
            {
                // Manual JSON input
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("Manual JSON", EditorStyles.boldLabel);
                jsonScroll = EditorGUILayout.BeginScrollView(jsonScroll, GUILayout.Height(200));
                manualJsonInput = EditorGUILayout.TextArea(manualJsonInput ?? "{}", GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
            else
            {
                // Prompt and parameters
                EditorGUILayout.BeginHorizontal();
                {
                    // Prompt
                    EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.6f));
                    GUILayout.Label("Prompt", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Describe the asset, scene, or full game you want to generate.", MessageType.None);
                    promptScroll = EditorGUILayout.BeginScrollView(promptScroll, GUILayout.Height(140));
                    textPrompt = EditorGUILayout.TextArea(textPrompt ?? "", GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();

                    GUILayout.Space(10);

                    // Optional parameters
                    EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.35f));
                    GUILayout.Label("Optional Parameters (JSON)", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Style, scale, quantity, difficulty, themes, etc.", MessageType.None);
                    paramScroll = EditorGUILayout.BeginScrollView(paramScroll, GUILayout.Height(140));
                    optionalParameters = EditorGUILayout.TextArea(optionalParameters ?? "{}", GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(15);

            // Generate button
            if (GUILayout.Button("Generate", GUILayout.Height(35)))
            {
                if (useManualJson)
                    GenerateFromManualJson();
                else
                    _ = GeneratePromptAsync();
            }

            GUILayout.Space(15);

            // Log
            showLog = EditorGUILayout.Foldout(showLog, "Generation Log / Output", true);
            if (showLog)
            {
                EditorGUILayout.BeginVertical("box");
                logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.Height(200));
                EditorGUILayout.TextArea(generationLog ?? "", GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Copy Log to Clipboard"))
                {
                    EditorGUIUtility.systemCopyBuffer = generationLog ?? "";
                }
                EditorGUILayout.EndVertical();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AIAssetGeneratorWindow] GUI Exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void GenerateFromManualJson()
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        try
        {
            if (string.IsNullOrWhiteSpace(manualJsonInput))
            {
                EditorUtility.DisplayDialog("AI Generator", "Please enter JSON.", "OK");
                LogWithTimestamp(timestamp, "Manual JSON validation failed: input is empty.");
                return;
            }

            LogWithTimestamp(timestamp, $"Using manual JSON:\n{manualJsonInput}");

            try
            {
                UnityAIBuilder.BuildFromJSON(manualJsonInput, selectedMode.ToString());
                LogWithTimestamp(timestamp, "Manual JSON generation completed successfully.");
            }
            catch (Exception ex)
            {
                LogWithTimestamp(timestamp, $"Error during Unity asset/scene/game building from manual JSON: {ex.Message}");
                Debug.LogError($"[AIAssetGeneratorWindow] BuildFromJSON exception: {ex}");
            }
        }
        catch (Exception ex)
        {
            LogWithTimestamp(timestamp, $"Unexpected exception during manual JSON generation: {ex.Message}");
            Debug.LogError($"[AIAssetGeneratorWindow] Unexpected manual JSON generation exception: {ex}");
        }
        finally
        {
            logScroll.y = float.MaxValue;
            Repaint();
        }
    }

    private async Task GeneratePromptAsync()
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        try
        {
            if (string.IsNullOrWhiteSpace(textPrompt))
            {
                EditorUtility.DisplayDialog("AI Generator", "Please enter a prompt.", "OK");
                LogWithTimestamp(timestamp, "Prompt validation failed: prompt is empty.");
                return;
            }

            LogWithTimestamp(timestamp, $"Prompt submitted: {textPrompt}\nParameters: {optionalParameters}");

            generationLog += $"[{timestamp}] Sending prompt to OpenAI...\n";
            logScroll.y = float.MaxValue;
            Repaint();

            string aiJson = null;
            try
            {
                aiJson = await OpenAIClient.GetAIJSONFromPrompt(textPrompt, optionalParameters ?? "{}", selectedMode.ToString());
                if (string.IsNullOrWhiteSpace(aiJson))
                {
                    throw new Exception("Received empty response from AI.");
                }
                LogWithTimestamp(timestamp, $"AI JSON Output:\n{aiJson}");
            }
            catch (Exception ex)
            {
                LogWithTimestamp(timestamp, $"Error fetching AI JSON: {ex.Message}");
                Debug.LogError($"[AIAssetGeneratorWindow] AI JSON fetch exception: {ex}");
                return;
            }

            try
            {
                if (aiJson != null)
                {
                    UnityAIBuilder.BuildFromJSON(aiJson, selectedMode.ToString());
                    LogWithTimestamp(timestamp, "AI generation completed successfully.");
                }
            }
            catch (Exception ex)
            {
                LogWithTimestamp(timestamp, $"Error during Unity asset/scene/game building: {ex.Message}");
                Debug.LogError($"[AIAssetGeneratorWindow] BuildFromJSON exception: {ex}");
            }
        }
        catch (Exception ex)
        {
            LogWithTimestamp(timestamp, $"Unexpected exception during generation: {ex.Message}");
            Debug.LogError($"[AIAssetGeneratorWindow] Unexpected generation exception: {ex}");
        }
        finally
        {
            logScroll.y = float.MaxValue;
            Repaint();
        }
    }

    private void LogWithTimestamp(string timestamp, string message)
    {
        generationLog += $"[{timestamp}] {message}\n";
        Debug.Log($"[AIAssetGeneratorWindow] {message}");
    }
}
