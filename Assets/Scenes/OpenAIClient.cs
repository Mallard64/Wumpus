using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Single entry point for every OpenAI chat-completion call the game makes.
/// <para>
/// The API key is never stored in source. It is resolved at runtime, in order, from:
/// <list type="number">
///   <item><description>A <c>TextAsset</c> named <c>openai_api_key</c> under any <c>Resources</c> folder (git-ignored).</description></item>
///   <item><description>The <c>OPENAI_API_KEY</c> environment variable (useful in the Unity Editor).</description></item>
/// </list>
/// When no key is present the client fails fast through <c>onFailure</c> so callers can
/// degrade gracefully instead of blocking on a doomed web request.
/// </para>
/// </summary>
public static class OpenAIClient
{
    /// <summary>OpenAI chat-completions REST endpoint.</summary>
    public const string ChatCompletionsEndpoint = "https://api.openai.com/v1/chat/completions";

    /// <summary>Model used for every in-game generation (trivia, taunts, hints).</summary>
    public const string DefaultModel = "gpt-3.5-turbo";

    /// <summary>Token budget for short one-line responses such as hints and Wumpus taunts.</summary>
    public const int ShortResponseTokens = 50;

    /// <summary>Token budget for a full multiple-choice trivia question.</summary>
    public const int QuestionResponseTokens = 100;

    private const string ApiKeyResourceName = "openai_api_key";
    private const string ApiKeyEnvironmentVariable = "OPENAI_API_KEY";
    private const string JsonContentType = "application/json";

    /// <summary>Matches the assistant text inside a chat-completion response body.</summary>
    private const string MessageContentPattern = "\"content\":\\s*\"(.*?)\"";

    /// <summary>Strips stray escape slashes left behind by the lightweight regex parsing.</summary>
    private const string EscapeSlashPattern = @"[\\\/]";

    private static string cachedApiKey;
    private static bool apiKeyResolved;

    /// <summary>
    /// The resolved OpenAI API key, or <c>null</c> when none is configured.
    /// Resolution happens once and is cached for the lifetime of the domain.
    /// </summary>
    public static string ApiKey
    {
        get
        {
            if (!apiKeyResolved)
            {
                cachedApiKey = ResolveApiKey();
                apiKeyResolved = true;
            }
            return cachedApiKey;
        }
    }

    /// <summary>True when an API key is available and live generation can be attempted.</summary>
    public static bool IsConfigured => !string.IsNullOrEmpty(ApiKey);

    /// <summary>
    /// Sends a single-turn chat completion request and reports the raw response body.
    /// </summary>
    /// <param name="prompt">
    /// User-role prompt. Inserted into the JSON payload verbatim, so it must not contain
    /// unescaped double quotes; escape sequences such as <c>\n</c> are passed through to the model.
    /// </param>
    /// <param name="maxTokens">Upper bound on the number of tokens in the completion.</param>
    /// <param name="onSuccess">Invoked with the raw JSON response body on a 2xx result.</param>
    /// <param name="onFailure">Invoked with a human-readable reason when the call cannot complete.</param>
    /// <returns>A coroutine to be driven with <c>StartCoroutine</c>.</returns>
    public static IEnumerator SendChatCompletion(
        string prompt,
        int maxTokens,
        Action<string> onSuccess,
        Action<string> onFailure = null)
    {
        if (!IsConfigured)
        {
            Debug.LogWarning(
                $"OpenAI API key not configured. Add a '{ApiKeyResourceName}' TextAsset under a " +
                $"Resources folder or set the {ApiKeyEnvironmentVariable} environment variable.");
            onFailure?.Invoke("missing API key");
            yield break;
        }

        string requestBody = BuildRequestBody(prompt, maxTokens);

        using (UnityWebRequest request = new UnityWebRequest(ChatCompletionsEndpoint, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", JsonContentType);
            request.SetRequestHeader("Authorization", $"Bearer {ApiKey}");

            yield return request.SendWebRequest();

            bool failed = request.result == UnityWebRequest.Result.ConnectionError
                       || request.result == UnityWebRequest.Result.ProtocolError;

            if (failed)
            {
                Debug.LogError($"OpenAI request failed ({request.responseCode}): {request.error}");
                onFailure?.Invoke(request.error);
            }
            else
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Extracts the assistant's message text from a chat-completion response body.
    /// </summary>
    /// <param name="jsonResponse">Raw JSON body returned by the API.</param>
    /// <returns>The cleaned message text, or <c>null</c> when no content field is present.</returns>
    public static string ExtractMessageContent(string jsonResponse)
    {
        if (string.IsNullOrEmpty(jsonResponse))
        {
            return null;
        }

        Match match = Regex.Match(jsonResponse, MessageContentPattern);
        return match.Success ? CleanText(match.Groups[1].Value) : null;
    }

    /// <summary>
    /// Removes escape slashes that survive the regex-based response parsing.
    /// </summary>
    /// <param name="text">Text captured from a JSON response.</param>
    /// <returns>The text with backslashes and forward slashes removed.</returns>
    public static string CleanText(string text)
    {
        return text == null ? null : Regex.Replace(text, EscapeSlashPattern, string.Empty);
    }

    /// <summary>Builds the chat-completions JSON payload for a single user message.</summary>
    private static string BuildRequestBody(string prompt, int maxTokens)
    {
        return "{\"model\": \"" + DefaultModel + "\", \"messages\": [{\"role\": \"user\", \"content\": \""
             + prompt + "\"}], \"max_tokens\": " + maxTokens + "}";
    }

    /// <summary>Looks for a key in Resources first, then in the process environment.</summary>
    private static string ResolveApiKey()
    {
        TextAsset keyAsset = Resources.Load<TextAsset>(ApiKeyResourceName);
        if (keyAsset != null && !string.IsNullOrWhiteSpace(keyAsset.text))
        {
            return keyAsset.text.Trim();
        }

        string environmentKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
        return string.IsNullOrWhiteSpace(environmentKey) ? null : environmentKey.Trim();
    }
}
