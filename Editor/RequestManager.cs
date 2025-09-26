using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class ChatMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class Choice
{
    public int index;
    public ChatMessage message;
}

[System.Serializable]
public class ChatResponse
{
    public Choice[] choices;
}

// Helper classes for sending request
[System.Serializable]
public class Message
{
    public string role;
    public string content;
}

[System.Serializable]
public class ChatRequest
{
    public string model;
    public Message[] messages;
}

public static class RequestManager
{
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
    private const string Model = "gpt-4o-mini"; // change if needed

    private static string ApiKey => EditorPrefs.GetString("AI_API_KEY", "YOUR_KEY_HERE");

    public static async Task<string> SendPromptAsync(string prompt, string context)
    {
        string systemInstruction =
@"You are a Unity Editor assistant.
- Always produce Unity editor-safe C# scripts.
- Follow Unity coding conventions.
- Respect Unity lifecycle methods (Awake, Start, Update, OnDestroy, etc).
- Assume the Unity version is 6000.0.58f1 as reference.
- Never produce obsolete API calls or unsafe editor operations.
- Be concise and focused.";

        // Build request object safely
        var chatRequest = new ChatRequest
        {
            model = Model,
            messages = new Message[]
            {
                new Message { role = "system", content = systemInstruction },
                new Message { role = "user", content = $"Context:\n{context}\n\nPrompt:\n{prompt}" }
            }
        };

        string bodyJson = JsonUtility.ToJson(chatRequest);

        using (var request = new UnityWebRequest(ApiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + ApiKey);

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse only the assistant’s text
                var rawJson = request.downloadHandler.text;
                var response = JsonUtility.FromJson<ChatResponse>(rawJson);

                if (response != null && response.choices != null && response.choices.Length > 0)
                {
                    return response.choices[0].message.content.Trim();
                }

                return "[Error] Empty response";
            }
            else
            {
                return "[Error] " + request.error + "\n" + request.downloadHandler.text;
            }
        }
    }
}
