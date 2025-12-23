using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class CodeFolderNode
{
    public string Name;
    public List<CodeFolderNode> Subfolders = new();
    public List<CodeFileNode> Files = new();
}

[Serializable]
public class CodeFileNode
{
    public string RelativePath;
    public List<CodeClassNode> Classes = new();
}

[Serializable]
public class CodeClassNode
{
    public string Name;
    public string BaseClass;
    public List<string> Interfaces = new();
    public List<string> Fields = new();
    public List<string> MethodSignatures = new();
}

public static class ProjectCodeHierarchyIndex
{
    private static CodeFolderNode _root;

    public static CodeFolderNode GetIndex() => _root;

    private static int FindMatchingBrace(string text, int startIndex)
    {
        int depth = 0;
        for (int i = startIndex; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') depth--;
            if (depth == 0) return i;
        }
        return -1;
    }

    public static void Refresh()
    {
        string scriptsPath = Path.Combine(Application.dataPath, "Scripts");
        if (!Directory.Exists(scriptsPath))
        {
            Debug.LogWarning("[AI Assistant] Scripts folder not found: " + scriptsPath);
            return;
        }

        _root = new CodeFolderNode { Name = "Scripts" };
        string[] files = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);

        // Match class name up to opening brace
        Regex classRegex = new(@"class\s+(\w+)(?:\s*:\s*([\w\s,<>]+))?\s*\{", RegexOptions.Singleline);
        Regex fieldRegex = new(@"(public|private|protected|internal)\s+[\w<>\[\]]+\s+(\w+)\s*(=|;)");
        Regex methodRegex = new(@"(public|private|protected|internal)\s+([\w<>\[\]]+)\s+(\w+)\s*\(([^)]*)\)");

        foreach (string file in files)
        {
            string relativePath = file.Substring(scriptsPath.Length + 1).Replace("\\", "/");
            string[] pathParts = relativePath.Split('/');

            CodeFolderNode currentFolder = _root;
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                string folderName = pathParts[i];
                CodeFolderNode nextFolder = currentFolder.Subfolders.Find(f => f.Name == folderName);
                if (nextFolder == null)
                {
                    nextFolder = new CodeFolderNode { Name = folderName };
                    currentFolder.Subfolders.Add(nextFolder);
                }
                currentFolder = nextFolder;
            }

            string content = File.ReadAllText(file);
            CodeFileNode fileNode = new() { RelativePath = "Assets/Scripts/" + relativePath };

            Dictionary<string, CodeClassNode> classMap = new();

            foreach (Match classMatch in classRegex.Matches(content))
            {
                string className = classMatch.Groups[1].Value;
                if (classMap.ContainsKey(className)) continue;

                CodeClassNode classNode = new() { Name = className };

                if (classMatch.Groups[2].Success)
                {
                    string[] parents = classMatch.Groups[2].Value.Split(',');
                    classNode.BaseClass = parents[0].Trim();
                    for (int i = 1; i < parents.Length; i++)
                        classNode.Interfaces.Add(parents[i].Trim());
                }

                int bodyStart = classMatch.Index + classMatch.Value.LastIndexOf('{');
                int bodyEnd = FindMatchingBrace(content, bodyStart);
                if (bodyEnd > bodyStart)
                {
                    string classBody = content.Substring(bodyStart + 1, bodyEnd - bodyStart - 1);

                    // Deduplicate fields/methods
                    HashSet<string> addedFields = new();
                    foreach (Match fieldMatch in fieldRegex.Matches(classBody))
                        addedFields.Add(fieldMatch.Groups[2].Value);
                    classNode.Fields.AddRange(addedFields);

                    HashSet<string> addedMethods = new();
                    foreach (Match methodMatch in methodRegex.Matches(classBody))
                    {
                        string signature = $"{methodMatch.Groups[2].Value} {methodMatch.Groups[3].Value}({methodMatch.Groups[4].Value.Trim()})";
                        addedMethods.Add(signature);
                    }
                    classNode.MethodSignatures.AddRange(addedMethods);
                }

                classMap[className] = classNode;
                fileNode.Classes.Add(classNode);
            }

            currentFolder.Files.Add(fileNode);
        }

        Debug.Log("[AI Assistant] Hierarchical code index refreshed.");
    }
}