using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Represents a single chat message with a role (system, user, assistant) and content.
/// </summary>
[System.Serializable]
public class ChatMessage
{
    /// <summary>The role of the message (system, user, assistant).</summary>
    public string role;

    /// <summary>The text content of the message.</summary>
    public string content;
}

/// <summary>
/// Represents a single choice returned by the AI in a chat completion response.
/// </summary>
[System.Serializable]
public class Choice
{
    /// <summary>Index of this choice in the response array.</summary>
    public int index;

    /// <summary>The chat message returned by the AI.</summary>
    public ChatMessage message;
}

/// <summary>
/// Represents the overall chat completion response from OpenAI.
/// Contains an array of choices.
/// </summary>
[System.Serializable]
public class ChatResponse
{
    /// <summary>Array of AI-generated choices.</summary>
    public Choice[] choices;
}

/// <summary>
/// Helper class representing a message in the request sent to OpenAI.
/// </summary>
[System.Serializable]
public class Message
{
    /// <summary>Role of the message (system, user).</summary>
    public string role;

    /// <summary>Text content of the message.</summary>
    public string content;
}

/// <summary>
/// Represents the request payload for the OpenAI Chat API.
/// </summary>
[System.Serializable]
public class ChatRequest
{
    /// <summary>The model to use for the request (e.g., gpt-4o-mini).</summary>
    public string model;

    /// <summary>Array of messages forming the conversation context.</summary>
    public Message[] messages;
}

/// <summary>
/// Handles sending prompts to the OpenAI Chat API and retrieving responses.
/// Designed for Unity Editor integration.
/// </summary>
public static class RequestManager
{
    // OpenAI chat completions endpoint
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

    // Default model to use; can be changed if needed
    private const string Model = "gpt-4o-mini";

    /// <summary>
    /// Retrieves the stored API key from EditorPrefs.
    /// Defaults to "YOUR_KEY_HERE" if not set.
    /// </summary>
    private static string ApiKey => EditorPrefs.GetString("AI_API_KEY", "YOUR_KEY_HERE");

    /// <summary>
    /// Sends a user prompt along with the current project context to the AI and returns the response.
    /// </summary>
    /// <param name="prompt">The user-provided prompt to send to the AI.</param>
    /// <param name="context">The current project context to provide AI awareness (e.g., classes and methods).</param>
    /// <returns>The AI-generated response as a string, or an error message starting with "[Error]" if failed.</returns>
    public static async Task<string> SendPromptAsync(string prompt, string context)
    {
        // System instruction guiding AI to behave as a Unity-safe assistant
        string systemInstruction =
@"You are a Unity Editor assistant.
- Always produce Unity editor-safe C# scripts.
- Follow Unity coding conventions.
- Respect Unity lifecycle methods (Awake, Start, Update, OnDestroy, etc).
- Assume the Unity version is 6000.0.58f1 as reference.
- Never produce obsolete API calls or unsafe editor operations.
- Be concise and focused.";

        // Build request object
        var chatRequest = new ChatRequest
        {
            model = Model,
            messages = new Message[]
            {
                new Message { role = "system", content = systemInstruction },
                new Message { role = "user", content = $"Context:\n{context}\n\nPrompt:\n{prompt}" }
            }
        };

        // Serialize request to JSON
        string bodyJson = JsonUtility.ToJson(chatRequest);

        using (var request = new UnityWebRequest(ApiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + ApiKey);

            // Send the request asynchronously
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            // Handle response
            if (request.result == UnityWebRequest.Result.Success)
            {
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
                // Return error with details from UnityWebRequest
                return "[Error] " + request.error + "\n" + request.downloadHandler.text;
            }
        }
    }
}
