using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public static class OpenAIClient
{
    private static string GetApiKey()
    {
        try
        {
            string key = EditorPrefs.GetString("AI_API_KEY", "");
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[OpenAIClient] OpenAI API key is not set. Go to Preferences > AI Assistant to set it.");
            }
            return key ?? "";
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OpenAIClient] Exception getting API key: {ex.Message}");
            return "";
        }
    }

    /// <summary>
    /// Sends a prompt to OpenAI and ensures the response is valid Unity JSON.
    /// </summary>
    public static async Task<string> GetAIJSONFromPrompt(string prompt, string parameters, string mode)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        try
        {
            prompt = prompt ?? "";
            parameters = parameters ?? "{}";
            mode = mode ?? "Asset";

            LogWithTimestamp(timestamp, $"Preparing OpenAI request for mode: {mode}\nPrompt: {prompt}\nParameters: {parameters}");

            string apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                LogWithTimestamp(timestamp, "API key missing. Aborting request.");
                return "{}"; // Fail gracefully
            }

            // Wrap the user prompt with strict JSON instructions
            string systemMessage =
                $"You are a Unity AI assistant. Output ONLY valid JSON for {mode} generation. " +
                "Do NOT include explanations, markdown, or text. " +
                "The JSON must include 'name', 'objects', and 'components' as needed for Unity prefabs/scenes.";

            string userMessage = $"Prompt: {prompt}\nParameters: {parameters}\nMode: {mode}\nReturn valid Unity JSON only.";

            JObject requestJson = new JObject
            {
                ["model"] = "gpt-4",
                ["messages"] = new JArray(
                    new JObject { ["role"] = "system", ["content"] = systemMessage },
                    new JObject { ["role"] = "user", ["content"] = userMessage }
                ),
                ["temperature"] = 0.7,
                ["max_tokens"] = 4000
            };

            string url = "https://api.openai.com/v1/chat/completions";
            string jsonString = requestJson.ToString();

            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            LogWithTimestamp(timestamp, "Sending request to OpenAI...");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                LogWithTimestamp(timestamp, $"OpenAI request failed: {request.error}");
                return "{}";
            }

            string responseText = request.downloadHandler?.text ?? "";
            LogWithTimestamp(timestamp, $"OpenAI raw response: {responseText}");

            if (string.IsNullOrWhiteSpace(responseText))
            {
                LogWithTimestamp(timestamp, "OpenAI returned empty response.");
                return "{}";
            }

            try
            {
                JObject responseJson = JObject.Parse(responseText);
                string aiContent = responseJson["choices"]?[0]?["message"]?["content"]?.ToString();

                if (string.IsNullOrWhiteSpace(aiContent))
                {
                    LogWithTimestamp(timestamp, "OpenAI returned empty content field.");
                    return "{}";
                }

                // Validate JSON structure
                JObject.Parse(aiContent); // Will throw if invalid
                LogWithTimestamp(timestamp, "AI JSON parsed successfully.");
                return aiContent;
            }
            catch (Exception ex)
            {
                LogWithTimestamp(timestamp, $"Failed to parse AI content as JSON: {ex.Message}");
                return "{}";
            }
        }
        catch (Exception ex)
        {
            LogWithTimestamp(timestamp, $"Unexpected exception in GetAIJSONFromPrompt: {ex.Message}");
            return "{}";
        }
    }

    private static void LogWithTimestamp(string timestamp, string message)
    {
        string logMessage = $"[{timestamp}] [OpenAIClient] {message}";
        Debug.Log(logMessage);
    }
}
