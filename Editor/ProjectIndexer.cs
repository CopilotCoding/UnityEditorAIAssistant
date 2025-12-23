using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Scans all .cs files under Assets/Scripts and extracts
/// classes and methods along with their folder structure,
/// so the AI knows which scripts, classes, and methods exist and where.
/// </summary>
public static class ProjectIndexer
{
    private static List<string> _index = new List<string>();

    /// <summary>
    /// Get current index (does NOT auto-refresh).
    /// </summary>
    public static List<string> GetIndex()
    {
        return _index;
    }

    /// <summary>
    /// Force a full rescan of the Scripts folder.
    /// </summary>
    public static void Refresh()
    {
        _index.Clear();
        string scriptsPath = Path.Combine(Application.dataPath, "Scripts");

        if (!Directory.Exists(scriptsPath))
        {
            Debug.LogWarning("[AI Assistant] Scripts folder not found: " + scriptsPath);
            return;
        }

        string[] files = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);

        Regex classRegex = new Regex(@"class\s+(\w+)");
        Regex methodRegex = new Regex(@"(public|private|protected|internal)\s+[\w<>\[\]]+\s+(\w+)\s*\(");
        Regex fieldRegex = new Regex(@"(public|private|protected|internal)\s+[\w<>\[\]]+\s+(\w+)\s*(=|;)");
        // Matches lines like "public int health;" or "private Rigidbody rb = null;"

        foreach (string file in files)
        {
            string relativePath = "Assets" + file.Substring(Application.dataPath.Length).Replace("\\", "/");
            string content = File.ReadAllText(file);

            var classes = classRegex.Matches(content);
            if (classes.Count > 0)
            {
                foreach (Match classMatch in classes)
                {
                    string className = classMatch.Groups[1].Value;
                    _index.Add($"File: {relativePath}");
                    _index.Add($"  Class: {className}");

                    // Track fields
                    foreach (Match fieldMatch in fieldRegex.Matches(content))
                    {
                        if (fieldMatch.Index > classMatch.Index) // Ensure it's inside the class
                        {
                            string fieldName = fieldMatch.Groups[2].Value;
                            _index.Add($"    Field: {fieldName}");
                        }
                    }

                    // Track methods
                    foreach (Match methodMatch in methodRegex.Matches(content))
                    {
                        if (methodMatch.Index > classMatch.Index)
                        {
                            string methodName = methodMatch.Groups[2].Value;
                            _index.Add($"    Method: {methodName}()");
                        }
                    }
                }
            }
            else
            {
                // File with no classes
                _index.Add($"File: {relativePath}");
            }
        }

        Debug.Log($"[AI Assistant] Scripts folder index refreshed. Entries: {_index.Count}");
    }
}
