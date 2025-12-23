using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor helper to refresh ProjectIndexer and save its output to a text file.
/// </summary>
public static class ProjectIndexerDebuggerEditor
{
    private const string outputFileName = "ProjectIndex.txt";

    [MenuItem("AI Tools/Save Project Index")]
    public static void SaveProjectIndex()
    {
        // Refresh the index
        ProjectIndexer.Refresh();

        // Get the index
        var index = ProjectIndexer.GetIndex();

        // Save path in Assets folder
        string outputPath = Path.Combine(Application.dataPath, outputFileName);

        try
        {
            File.WriteAllLines(outputPath, index);
            Debug.Log($"[AI Assistant] Project index saved to: {outputPath}");
            AssetDatabase.Refresh(); // Make Unity notice the new file
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AI Assistant] Failed to save project index: {e.Message}");
        }
    }
}
