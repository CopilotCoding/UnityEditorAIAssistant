using System;
using System.IO;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UnityAIBuilder
{
    // --- Entry point ---
    public static void BuildFromJSON(string json, string mode)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        if (string.IsNullOrWhiteSpace(json))
        {
            LogWithTimestamp(timestamp, "Received empty JSON. Aborting generation.", true);
            return;
        }

        JObject root;
        try
        {
            root = JObject.Parse(json);
        }
        catch (Exception ex)
        {
            LogWithTimestamp(timestamp, $"Failed to parse JSON: {ex.Message}\nJSON: {json}", true);
            return;
        }

        LogWithTimestamp(timestamp, $"Starting {mode} generation.");

        try
        {
            switch (mode)
            {
                case "Asset":
                    BuildPrefab(root);
                    break;
                case "Scene":
                    BuildScene(root);
                    break;
                case "FullGame":
                    BuildFullGame(root);
                    break;
                default:
                    LogWithTimestamp(timestamp, $"Unknown generation mode: {mode}", false);
                    break;
            }
        }
        catch (Exception ex)
        {
            LogWithTimestamp(timestamp, $"Unexpected exception during {mode} generation: {ex.Message}", true);
        }
    }

    // --- Prefab generation ---
    private static void BuildPrefab(JObject assetJson)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        if (assetJson == null)
        {
            LogWithTimestamp(timestamp, "Prefab JSON is null.", true);
            return;
        }

        string name = assetJson["name"]?.ToString() ?? "UnnamedPrefab";
        LogWithTimestamp(timestamp, $"Generating Prefab: {name}");

        GameObject go = new GameObject(name);

        var components = assetJson["components"] as JArray;
        if (components == null)
        {
            LogWithTimestamp(timestamp, $"No components found for prefab '{name}'.", false);
        }
        else
        {
            foreach (var comp in components)
            {
                try
                {
                    string type = comp["type"]?.ToString();
                    if (string.IsNullOrEmpty(type))
                    {
                        LogWithTimestamp(timestamp, $"Skipping component with missing 'type': {comp}", false);
                        continue;
                    }

                    if (type == "MeshRenderer")
                    {
                        go.AddComponent<MeshRenderer>();
                        LogWithTimestamp(timestamp, $"Added MeshRenderer to '{name}'.");
                    }
                    else
                    {
                        var scriptType = GetTypeByName(type);
                        if (scriptType != null)
                        {
                            go.AddComponent(scriptType);
                            LogWithTimestamp(timestamp, $"Added script component '{type}' to '{name}'.");
                        }
                        else
                        {
                            LogWithTimestamp(timestamp, $"Script type '{type}' not found. Skipping.", false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWithTimestamp(timestamp, $"Exception adding component: {ex.Message}", true);
                }
            }
        }

        try
        {
            string path = $"Assets/Generated/Prefabs/{name}.prefab";
            Directory.CreateDirectory("Assets/Generated/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
            LogWithTimestamp(timestamp, $"Prefab saved: {path}");
        }
        catch (Exception ex)
        {
            LogWithTimestamp(timestamp, $"Failed to save prefab: {ex.Message}", true);
        }
    }

    // --- Scene generation ---
    private static void BuildScene(JObject sceneJson)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        if (sceneJson == null)
        {
            LogWithTimestamp(timestamp, "Scene JSON is null.", true);
            return;
        }

        string sceneName = sceneJson["name"]?.ToString() ?? "UnnamedScene";
        string path = $"Assets/Generated/Scenes/{sceneName}.unity";
        Directory.CreateDirectory("Assets/Generated/Scenes");

        LogWithTimestamp(timestamp, $"Generating Scene: {sceneName}");

        Scene scene;
        try
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }
        catch (Exception ex)
        {
            LogWithTimestamp(timestamp, $"Failed to create new scene: {ex.Message}", true);
            return;
        }

        var objects = sceneJson["objects"] as JArray;
        if (objects == null)
        {
            LogWithTimestamp(timestamp, $"Scene '{sceneName}' has no objects defined.", false);
        }
        else
        {
            foreach (var obj in objects)
            {
                try
                {
                    string type = obj["type"]?.ToString();
                    if (string.IsNullOrEmpty(type))
                    {
                        LogWithTimestamp(timestamp, $"Skipping object with missing 'type': {obj}", false);
                        continue;
                    }

                    if (type == "Prefab")
                    {
                        string prefabRef = obj["ref"]?.ToString();
                        if (string.IsNullOrEmpty(prefabRef))
                        {
                            LogWithTimestamp(timestamp, $"Prefab object missing 'ref': {obj}", false);
                            continue;
                        }

                        string prefabPath = $"Assets/Generated/Prefabs/{prefabRef}.prefab";
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (prefab == null)
                        {
                            LogWithTimestamp(timestamp, $"Prefab not found at path: {prefabPath}", false);
                            continue;
                        }

                        Vector3 pos = ParseVector3Safe(obj["position"]);
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        instance.transform.position = pos;

                        LogWithTimestamp(timestamp, $"Instantiated prefab '{prefabRef}' at {pos}");
                    }
                    else
                    {
                        GameObject go = new GameObject(obj["name"]?.ToString() ?? "UnnamedObject");
                        Vector3 pos = ParseVector3Safe(obj["position"]);
                        go.transform.position = pos;
                        LogWithTimestamp(timestamp, $"Created empty GameObject '{go.name}' at {pos}");
                    }
                }
                catch (Exception ex)
                {
                    LogWithTimestamp(timestamp, $"Exception creating object: {ex.Message}", true);
                }
            }
        }

        try
        {
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            LogWithTimestamp(timestamp, $"Scene saved: {path}");
        }
        catch (Exception ex)
        {
            LogWithTimestamp(timestamp, $"Failed to save scene: {ex.Message}", true);
        }
    }

    // --- Full game generation ---
    private static void BuildFullGame(JObject gameJson)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        if (gameJson == null)
        {
            LogWithTimestamp(timestamp, "FullGame JSON is null.", true);
            return;
        }

        var scenes = gameJson["scenes"] as JArray;
        if (scenes == null)
        {
            LogWithTimestamp(timestamp, "No scenes found in FullGame JSON.", false);
            return;
        }

        foreach (var sceneJson in scenes)
        {
            BuildScene(sceneJson as JObject);
        }

        LogWithTimestamp(timestamp, "Full game generation complete!");
    }

    // --- Helpers ---
    private static Vector3 ParseVector3Safe(JToken token)
    {
        if (token == null || token.Type != JTokenType.Array || token.Count() < 3)
        {
            Debug.LogWarning($"[UnityAIBuilder] Invalid or missing position vector: {token}");
            return Vector3.zero;
        }

        try
        {
            float x = token[0]?.ToObject<float>() ?? 0f;
            float y = token[1]?.ToObject<float>() ?? 0f;
            float z = token[2]?.ToObject<float>() ?? 0f;
            return new Vector3(x, y, z);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UnityAIBuilder] Exception parsing position vector: {ex.Message}");
            return Vector3.zero;
        }
    }

    private static System.Type GetTypeByName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;

        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(typeName);
                if (type != null) return type;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UnityAIBuilder] Exception searching for type '{typeName}': {ex.Message}");
        }

        return null;
    }

    private static void LogWithTimestamp(string timestamp, string message, bool isError = false)
    {
        string logMessage = $"[{timestamp}] [UnityAIBuilder] {message}";
        if (isError)
            Debug.LogError(logMessage);
        else
            Debug.Log(logMessage);
    }
}
