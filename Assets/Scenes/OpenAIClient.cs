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
    public const string ChatCompletionsEndpoint = "https://api.openai.com/v1/chat/completions";

    /// <summary>
    /// Model used for every in-game generation. Chosen for cost: these calls are frequent,
    /// short, and not reasoning-heavy, so the cheapest current model is the right fit.
    /// </summary>
    public const string DefaultModel = "gpt-4.1-mini";

    /// <summary>Token budget for short one-line responses such as hints and Wumpus taunts.</summary>
    public const int ShortResponseTokens = 50;

    /// <summary>Token budget for a full multiple-choice trivia question.</summary>
    public const int QuestionResponseTokens = 300;

    /// <summary>
    /// Total tries for a structured request. A model occasionally returns content that parses
    /// as JSON but fails the caller's semantic checks, and one retry clears most of those.
    /// </summary>
    public const int MaxStructuredAttempts = 2;

    private const string ApiKeyResourceName = "openai_api_key";
    private const string ApiKeyEnvironmentVariable = "OPENAI_API_KEY";
    private const string JsonContentType = "application/json";

    /// <summary>
    /// Captures the assistant's message. The body alternative steps over escaped characters so a
    /// quote inside the content (unavoidable once the content is itself JSON) does not end the match.
    /// </summary>
    private const string MessageContentPattern = "\"content\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"";

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
    /// <param name="prompt">User-role prompt. Escaped before it goes into the payload.</param>
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
        return SendRequest(BuildRequestBody(prompt, maxTokens, null, null), onSuccess, onFailure);
    }

    /// <summary>
    /// Asks the model for a response constrained to a JSON schema, and keeps the answer only if
    /// the caller's validator accepts it.
    /// <para>
    /// The schema makes the model's output well-formed; <paramref name="isAcceptable"/> is what
    /// makes it usable, since a schema can guarantee the shape of a payload but not that (say) an
    /// answer index actually points at one of the answers. A rejected response is retried up to
    /// <see cref="MaxStructuredAttempts"/> times before the caller is told to fall back.
    /// </para>
    /// </summary>
    /// <param name="prompt">User-role prompt. Escaped before it goes into the payload.</param>
    /// <param name="maxTokens">Upper bound on the number of tokens in the completion.</param>
    /// <param name="schemaName">Schema name reported to the API; must match <c>^[a-zA-Z0-9_-]+$</c>.</param>
    /// <param name="schemaJson">The JSON Schema body the response must conform to.</param>
    /// <param name="isAcceptable">Semantic check run on the extracted content; a false result triggers a retry.</param>
    /// <param name="onSuccess">Invoked with the assistant's JSON content once it passes the check.</param>
    /// <param name="onFailure">Invoked with a reason when every attempt has been used up.</param>
    /// <returns>A coroutine to be driven with <c>StartCoroutine</c>.</returns>
    public static IEnumerator SendStructuredCompletion(
        string prompt,
        int maxTokens,
        string schemaName,
        string schemaJson,
        Func<string, bool> isAcceptable,
        Action<string> onSuccess,
        Action<string> onFailure = null)
    {
        string requestBody = BuildRequestBody(prompt, maxTokens, schemaName, schemaJson);
        string lastFailure = "no attempt was made";

        for (int attempt = 1; attempt <= MaxStructuredAttempts; attempt++)
        {
            string rawResponse = null;
            string requestError = null;

            yield return SendRequest(
                requestBody,
                response => rawResponse = response,
                error => requestError = error);

            if (requestError != null)
            {
                lastFailure = requestError;
            }
            else
            {
                string content = ExtractMessageContent(rawResponse);
                if (string.IsNullOrEmpty(content))
                {
                    lastFailure = "response contained no message content";
                }
                else if (isAcceptable == null || isAcceptable(content))
                {
                    onSuccess?.Invoke(content);
                    yield break;
                }
                else
                {
                    lastFailure = "response did not pass validation";
                }
            }

            if (attempt < MaxStructuredAttempts)
            {
                Debug.LogWarning($"Structured OpenAI request attempt {attempt} failed ({lastFailure}); retrying.");
            }
        }

        Debug.LogWarning($"Structured OpenAI request gave up after {MaxStructuredAttempts} attempts: {lastFailure}.");
        onFailure?.Invoke(lastFailure);
    }

    /// <summary>
    /// Extracts the assistant's message text from a chat-completion response body.
    /// </summary>
    /// <param name="jsonResponse">Raw JSON body returned by the API.</param>
    /// <returns>The unescaped message text, or <c>null</c> when no content field is present.</returns>
    public static string ExtractMessageContent(string jsonResponse)
    {
        if (string.IsNullOrEmpty(jsonResponse))
        {
            return null;
        }

        Match match = Regex.Match(jsonResponse, MessageContentPattern);
        return match.Success ? UnescapeJsonString(match.Groups[1].Value) : null;
    }

    /// <summary>
    /// Turns the escape sequences of a JSON string literal back into the characters they stand for.
    /// </summary>
    /// <param name="text">The raw contents of a JSON string, without its surrounding quotes.</param>
    /// <returns>The decoded text.</returns>
    public static string UnescapeJsonString(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        StringBuilder builder = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '\\' || i + 1 >= text.Length)
            {
                builder.Append(text[i]);
                continue;
            }

            char escape = text[++i];
            switch (escape)
            {
                case 'n': builder.Append('\n'); break;
                case 't': builder.Append('\t'); break;
                case 'r': builder.Append('\r'); break;
                case 'b': builder.Append('\b'); break;
                case 'f': builder.Append('\f'); break;
                case '"': builder.Append('"'); break;
                case '\\': builder.Append('\\'); break;
                case '/': builder.Append('/'); break;
                case 'u':
                    if (i + 4 < text.Length
                        && int.TryParse(
                            text.Substring(i + 1, 4),
                            System.Globalization.NumberStyles.HexNumber,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out int codePoint))
                    {
                        builder.Append((char)codePoint);
                        i += 4;
                    }
                    else
                    {
                        builder.Append(escape);
                    }
                    break;
                default:
                    builder.Append(escape);
                    break;
            }
        }
        return builder.ToString();
    }

    /// <summary>Escapes a string so it can be embedded in a JSON string literal.</summary>
    /// <param name="text">Arbitrary text.</param>
    /// <returns>The text with quotes, backslashes and control characters escaped.</returns>
    public static string EscapeJsonString(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(text.Length + 16);
        foreach (char character in text)
        {
            switch (character)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                case '\b': builder.Append("\\b"); break;
                case '\f': builder.Append("\\f"); break;
                default:
                    if (character < ' ')
                    {
                        builder.Append("\\u").Append(((int)character).ToString("x4"));
                    }
                    else
                    {
                        builder.Append(character);
                    }
                    break;
            }
        }
        return builder.ToString();
    }

    /// <summary>Performs one HTTP round trip against the chat-completions endpoint.</summary>
    private static IEnumerator SendRequest(string requestBody, Action<string> onSuccess, Action<string> onFailure)
    {
        if (!IsConfigured)
        {
            Debug.LogWarning(
                $"OpenAI API key not configured. Add a '{ApiKeyResourceName}' TextAsset under a " +
                $"Resources folder or set the {ApiKeyEnvironmentVariable} environment variable.");
            onFailure?.Invoke("missing API key");
            yield break;
        }

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
    /// Builds the chat-completions payload. Passing a schema switches the call into structured
    /// output mode, where the model is constrained to emit JSON matching that schema.
    /// </summary>
    private static string BuildRequestBody(string prompt, int maxTokens, string schemaName, string schemaJson)
    {
        StringBuilder body = new StringBuilder();
        body.Append("{\"model\":\"").Append(DefaultModel).Append("\",");
        body.Append("\"messages\":[{\"role\":\"user\",\"content\":\"")
            .Append(EscapeJsonString(prompt))
            .Append("\"}],");
        body.Append("\"max_tokens\":").Append(maxTokens);

        if (!string.IsNullOrEmpty(schemaJson))
        {
            body.Append(",\"response_format\":{\"type\":\"json_schema\",\"json_schema\":{")
                .Append("\"name\":\"").Append(schemaName).Append("\",")
                .Append("\"strict\":true,")
                .Append("\"schema\":").Append(schemaJson)
                .Append("}}");
        }

        body.Append('}');
        return body.ToString();
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
