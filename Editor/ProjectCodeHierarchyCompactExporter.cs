using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ProjectCodeHierarchyCompactExporter
{
    private const string OutputFileName = "ProjectCodeHierarchy.compact.txt";

    [MenuItem("AI Tools/Save Compact Code Hierarchy")]
    public static void SaveCompactHierarchy()
    {
        ProjectCodeHierarchyIndex.Refresh();
        CodeFolderNode root = ProjectCodeHierarchyIndex.GetIndex();

        if (root == null)
        {
            Debug.LogError("[AI Assistant] Code hierarchy index is null.");
            return;
        }

        StringBuilder sb = new StringBuilder(8192);
        WriteFolder(root, sb);

        string path = Path.Combine(Application.dataPath, OutputFileName);
        File.WriteAllText(path, sb.ToString());

        Debug.Log($"[AI Assistant] Compact hierarchy saved to: {path}");
        AssetDatabase.Refresh();
    }

    private static void WriteFolder(CodeFolderNode folder, StringBuilder sb)
    {
        foreach (var file in folder.Files)
        {
            sb.AppendLine(file.RelativePath);

            // Deduplicate classes by name per file
            HashSet<string> emittedClasses = new HashSet<string>();

            foreach (var cls in file.Classes)
            {
                if (!emittedClasses.Add(cls.Name))
                    continue;

                sb.Append("  ");
                sb.Append(cls.Name);

                if (!string.IsNullOrEmpty(cls.BaseClass) || cls.Interfaces.Count > 0)
                {
                    sb.Append(" : ");
                    if (!string.IsNullOrEmpty(cls.BaseClass))
                        sb.Append(cls.BaseClass);

                    for (int i = 0; i < cls.Interfaces.Count; i++)
                    {
                        sb.Append(i == 0 && string.IsNullOrEmpty(cls.BaseClass) ? "" : " | ");
                        sb.Append(cls.Interfaces[i]);
                    }
                }

                sb.AppendLine();

                // Fields (state)
                foreach (var field in cls.Fields)
                {
                    sb.Append("    ");
                    sb.AppendLine(field);
                }

                // Methods (behavior)
                foreach (var method in cls.MethodSignatures)
                {
                    sb.Append("    ");
                    sb.AppendLine(method);
                }
            }

            sb.AppendLine();
        }

        foreach (var sub in folder.Subfolders)
        {
            WriteFolder(sub, sb);
        }
    }
}