using NUnit.Framework;

/// <summary>
/// Covers the validation layer behind structured trivia output. A JSON schema guarantees the
/// response has the right fields and types, but not that the content is playable, so these are
/// the checks that decide whether a generated question is used or retried.
/// </summary>
public class TriviaQuestionTests
{
    private const string ValidQuestion =
        "{\"question\":\"Which bird cannot fly?\",\"answers\":[\"Ostrich\",\"Robin\",\"Eagle\",\"Swift\"],\"correctIndex\":0}";

    [Test]
    public void ParseQuestion_AcceptsAWellFormedQuestion()
    {
        TriviaDisplay.QuestionData parsed = TriviaDisplay.ParseQuestion(ValidQuestion);

        Assert.IsNotNull(parsed);
        Assert.AreEqual("Which bird cannot fly?", parsed.question);
        Assert.AreEqual(TriviaDisplay.AnswerCount, parsed.answers.Length);
        Assert.AreEqual("Ostrich", parsed.answers[0]);
        Assert.AreEqual(0, parsed.correctIndex);
    }

    [Test]
    public void ParseQuestion_AcceptsACorrectIndexAtTheEnd()
    {
        TriviaDisplay.QuestionData parsed = TriviaDisplay.ParseQuestion(
            "{\"question\":\"Pick D\",\"answers\":[\"a\",\"b\",\"c\",\"d\"],\"correctIndex\":3}");

        Assert.IsNotNull(parsed);
        Assert.AreEqual(3, parsed.correctIndex);
    }

    [Test]
    public void ParseQuestion_RejectsTheWrongNumberOfAnswers()
    {
        Assert.IsNull(TriviaDisplay.ParseQuestion(
            "{\"question\":\"Too few\",\"answers\":[\"a\",\"b\",\"c\"],\"correctIndex\":0}"));

        Assert.IsNull(TriviaDisplay.ParseQuestion(
            "{\"question\":\"Too many\",\"answers\":[\"a\",\"b\",\"c\",\"d\",\"e\"],\"correctIndex\":0}"));

        Assert.IsNull(TriviaDisplay.ParseQuestion(
            "{\"question\":\"None\",\"answers\":[],\"correctIndex\":0}"));
    }

    [Test]
    public void ParseQuestion_RejectsACorrectIndexThatPointsNowhere()
    {
        Assert.IsNull(TriviaDisplay.ParseQuestion(
            "{\"question\":\"Negative\",\"answers\":[\"a\",\"b\",\"c\",\"d\"],\"correctIndex\":-1}"));

        Assert.IsNull(TriviaDisplay.ParseQuestion(
            "{\"question\":\"Past the end\",\"answers\":[\"a\",\"b\",\"c\",\"d\"],\"correctIndex\":4}"));
    }

    [Test]
    public void ParseQuestion_RejectsDuplicateAnswers()
    {
        // Two identical options make the question unanswerable once they are shuffled.
        Assert.IsNull(TriviaDisplay.ParseQuestion(
            "{\"question\":\"Dupes\",\"answers\":[\"Paris\",\"Rome\",\"Paris\",\"Oslo\"],\"correctIndex\":0}"));
    }

    [Test]
    public void ParseQuestion_RejectsDuplicateAnswersDifferingOnlyByCase()
    {
        Assert.IsNull(TriviaDisplay.ParseQuestion(
            "{\"question\":\"Dupes\",\"answers\":[\"Paris\",\"Rome\",\"PARIS\",\"Oslo\"],\"correctIndex\":1}"));
    }

    [Test]
    public void ParseQuestion_RejectsBlankFields()
    {
        Assert.IsNull(TriviaDisplay.ParseQuestion(
            "{\"question\":\"\",\"answers\":[\"a\",\"b\",\"c\",\"d\"],\"correctIndex\":0}"));

        Assert.IsNull(TriviaDisplay.ParseQuestion(
            "{\"question\":\"   \",\"answers\":[\"a\",\"b\",\"c\",\"d\"],\"correctIndex\":0}"));

        Assert.IsNull(TriviaDisplay.ParseQuestion(
            "{\"question\":\"Blank option\",\"answers\":[\"a\",\"\",\"c\",\"d\"],\"correctIndex\":0}"));
    }

    [Test]
    public void ParseQuestion_RejectsMissingFields()
    {
        Assert.IsNull(TriviaDisplay.ParseQuestion("{\"question\":\"No answers\"}"));
        Assert.IsNull(TriviaDisplay.ParseQuestion("{\"answers\":[\"a\",\"b\",\"c\",\"d\"],\"correctIndex\":0}"));
    }

    [Test]
    public void ParseQuestion_RejectsJunk()
    {
        Assert.IsNull(TriviaDisplay.ParseQuestion(null));
        Assert.IsNull(TriviaDisplay.ParseQuestion(""));
        Assert.IsNull(TriviaDisplay.ParseQuestion("   "));
        Assert.IsNull(TriviaDisplay.ParseQuestion("not json at all"));
        Assert.IsNull(TriviaDisplay.ParseQuestion("{\"question\":"));
    }
}
