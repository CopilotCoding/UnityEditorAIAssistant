using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor helper to refresh ProjectCodeHierarchyIndex
/// and save its hierarchical output to a JSON file.
/// Intended for AI inspection and tooling.
/// </summary>
public static class ProjectCodeHierarchyIndexDebuggerEditor
{
    private const string OutputFileName = "ProjectCodeHierarchyIndex.json";

    [MenuItem("AI Tools/Save Project Code Hierarchy Index")]
    public static void SaveProjectCodeHierarchyIndex()
    {
        // Refresh the hierarchy
        ProjectCodeHierarchyIndex.Refresh();

        // Get the root node
        CodeFolderNode root = ProjectCodeHierarchyIndex.GetIndex();

        if (root == null)
        {
            Debug.LogError("[AI Assistant] Code hierarchy index is null. Refresh failed.");
            return;
        }

        // Serialize to JSON (pretty-printed)
        string json = JsonUtility.ToJson(root, true);

        // Save path in Assets folder
        string outputPath = Path.Combine(Application.dataPath, OutputFileName);

        try
        {
            File.WriteAllText(outputPath, json);
            Debug.Log($"[AI Assistant] Project code hierarchy index saved to: {outputPath}");
            AssetDatabase.Refresh(); // Let Unity notice the file
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AI Assistant] Failed to save code hierarchy index: {e.Message}");
        }
    }
}