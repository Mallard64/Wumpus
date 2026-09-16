using NUnit.Framework;

/// <summary>
/// Covers response parsing. The previous content regex stopped at the first quote it saw, which
/// was fine for a plain sentence but truncates a structured-output response, whose content is
/// itself JSON and therefore full of escaped quotes.
/// </summary>
public class OpenAIClientTests
{
    [Test]
    public void ExtractMessageContent_ReadsAPlainSentence()
    {
        string response = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"Bats are mammals.\"}}]}";

        Assert.AreEqual("Bats are mammals.", OpenAIClient.ExtractMessageContent(response));
    }

    [Test]
    public void ExtractMessageContent_ReadsStructuredJsonContentWhole()
    {
        // The content field holds an entire escaped JSON document; none of it may be lost.
        string response =
            "{\"choices\":[{\"message\":{\"content\":\""
            + "{\\\"question\\\":\\\"Which bird cannot fly?\\\",\\\"answers\\\":[\\\"Ostrich\\\",\\\"Robin\\\",\\\"Eagle\\\",\\\"Swift\\\"],\\\"correctIndex\\\":0}"
            + "\"}}]}";

        string content = OpenAIClient.ExtractMessageContent(response);

        Assert.AreEqual(
            "{\"question\":\"Which bird cannot fly?\",\"answers\":[\"Ostrich\",\"Robin\",\"Eagle\",\"Swift\"],\"correctIndex\":0}",
            content);

        // And it must survive all the way into a usable question.
        TriviaDisplay.QuestionData parsed = TriviaDisplay.ParseQuestion(content);
        Assert.IsNotNull(parsed, "Structured content did not survive extraction.");
        Assert.AreEqual("Which bird cannot fly?", parsed.question);
    }

    [Test]
    public void ExtractMessageContent_KeepsTextAfterAnEscapedQuote()
    {
        string response = "{\"choices\":[{\"message\":{\"content\":\"She said \\\"hello\\\" and left.\"}}]}";

        Assert.AreEqual("She said \"hello\" and left.", OpenAIClient.ExtractMessageContent(response));
    }

    [Test]
    public void ExtractMessageContent_ReturnsNullWhenThereIsNoContent()
    {
        Assert.IsNull(OpenAIClient.ExtractMessageContent(null));
        Assert.IsNull(OpenAIClient.ExtractMessageContent(""));
        Assert.IsNull(OpenAIClient.ExtractMessageContent("{\"error\":{\"message\":\"nope\"}}"));
    }

    [Test]
    public void UnescapeJsonString_DecodesTheStandardEscapes()
    {
        Assert.AreEqual("line\nbreak", OpenAIClient.UnescapeJsonString("line\\nbreak"));
        Assert.AreEqual("tab\there", OpenAIClient.UnescapeJsonString("tab\\there"));
        Assert.AreEqual("a\\b", OpenAIClient.UnescapeJsonString("a\\\\b"));
        Assert.AreEqual("a/b", OpenAIClient.UnescapeJsonString("a\\/b"));
        Assert.AreEqual("quote\"mark", OpenAIClient.UnescapeJsonString("quote\\\"mark"));
    }

    [Test]
    public void UnescapeJsonString_DecodesUnicodeEscapes()
    {
        Assert.AreEqual("café", OpenAIClient.UnescapeJsonString("caf\\u00e9"));
    }

    [Test]
    public void EscapeJsonString_RoundTripsThroughUnescape()
    {
        string[] samples =
        {
            "plain text",
            "quotes \" inside",
            "back\\slash",
            "new\nline\twith\ttabs",
            "{\"nested\":\"json\"}",
            "café naïve"
        };

        foreach (string sample in samples)
        {
            string round = OpenAIClient.UnescapeJsonString(OpenAIClient.EscapeJsonString(sample));
            Assert.AreEqual(sample, round, $"Round trip changed: {sample}");
        }
    }

    [Test]
    public void EscapeJsonString_EscapesTheCharactersThatWouldBreakThePayload()
    {
        Assert.AreEqual("say \\\"hi\\\"", OpenAIClient.EscapeJsonString("say \"hi\""));
        Assert.AreEqual("a\\\\b", OpenAIClient.EscapeJsonString("a\\b"));
        Assert.AreEqual("one\\ntwo", OpenAIClient.EscapeJsonString("one\ntwo"));
    }

    [Test]
    public void DefaultModel_IsNotTheRetiredGpt35Turbo()
    {
        Assert.AreNotEqual("gpt-3.5-turbo", OpenAIClient.DefaultModel);
        Assert.IsNotEmpty(OpenAIClient.DefaultModel);
    }
}
