using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Runs a trivia round inside an encounter scene: pulls a generated multiple-choice question,
/// grades the player's pick, tracks the running tally, and reports the outcome back to
/// <see cref="PlayerScript"/> before unloading itself.
/// <para>
/// Questions that parse successfully are cached to disk and reused as offline fallbacks, so the
/// game stays playable without network access or an API key.
/// </para>
/// <para>
/// A round of <see cref="WumpusDuelQuestionCount"/> questions is the Wumpus duel and is narrated
/// with in-character generated dialogue; any other length is an ordinary shop or pit challenge.
/// </para>
/// </summary>
public class TriviaDisplay : MonoBehaviour
{
    /// <summary>Question count that marks a round as the Wumpus duel rather than a shop challenge.</summary>
    private const int WumpusDuelQuestionCount = 5;

    /// <summary>Seconds the correct answer stays on screen before the next question loads.</summary>
    private const float AnswerRevealSeconds = 2.0f;

    /// <summary>Length of the answer label prefix ("A:") stripped before comparing answers.</summary>
    private const int AnswerLabelLength = 2;

    /// <summary>Name of the on-disk cache of previously generated questions.</summary>
    private const string QuestionCacheFileName = "newdata.json";

    /// <summary>Prompt asking the model for a labelled multiple-choice question.</summary>
    private const string QuestionPrompt =
        "Generate a weird, truly unique trivia question with four multiple-choice answers. "
        + "Clearly label four answers and the one correct answer. Use the format: "
        + "Question: <question>\\nA: <answer1>\\nB: <answer2>\\nC: <answer3>\\nD: <answer4>\\nCorrect: <correctans>";

    /// <summary>Regex capturing the question, four answers and the correct label from a response.</summary>
    private const string QuestionPattern =
        "\"content\":\\s*\"Question: (.*?)\\\\nA: (.*?)\\\\nB: (.*?)\\\\nC: (.*?)\\\\nD: (.*?)\\\\nCorrect: (.*?)\"";

    private const string WumpusGreetingPrompt =
        "Imagine you are the wumpus and the player has came to hunt you. What do you say?";

    private const string WumpusTookHitPrompt =
        "Imagine you are the wumpus and the player has landed a hit on you. What do you say?";

    private const string WumpusLandedHitPrompt =
        "Imagine you are the wumpus and you landed a blow on the the pesky hunter trying to kill you. What do you say?";

    private const string WumpusDefeatedPrompt =
        "Imagine you are the wumpus and the player just defeated you, but you can run away. What do you say?";

    private const string WumpusVictoryPrompt =
        "Imagine you are the wumpus and you are about to kill the pesky hunter who tried to kill you. What do you say?";

    /// <summary>Appended to every dialogue prompt so responses do not arrive wrapped in quotes.</summary>
    private const string NoQuotesInstruction = "Don't include any quotes.";

    /// <summary>Name of the scene holding the player, used to route results back to it.</summary>
    private const string MainSceneName = "MainScene";

    /// <summary>Name of the GameObject carrying <see cref="PlayerScript"/> in the main scene.</summary>
    private const string PlayerObjectName = "sprite";

    /// <summary>A single multiple-choice question and its correct answer.</summary>
    [System.Serializable]
    public class QuestionData
    {
        /// <summary>The question text.</summary>
        public string question;

        /// <summary>Text of answer A.</summary>
        public string answerA;

        /// <summary>Text of answer B.</summary>
        public string answerB;

        /// <summary>Text of answer C.</summary>
        public string answerC;

        /// <summary>Text of answer D.</summary>
        public string answerD;

        /// <summary>The correct answer, as returned by the model.</summary>
        public string correct;
    }

    /// <summary>Serialization wrapper for the on-disk question cache.</summary>
    [System.Serializable]
    public class QuestionFile
    {
        /// <summary>Every question cached so far.</summary>
        public List<QuestionData> questions;
    }

    /// <summary>Label showing the current question.</summary>
    public TextMeshProUGUI questionText;

    /// <summary>Label showing the correct answer once the player has picked.</summary>
    public TextMeshProUGUI answerText;

    /// <summary>Label for answer A.</summary>
    [FormerlySerializedAs("t1")]
    public Text answerTextA;

    /// <summary>Label for answer B.</summary>
    [FormerlySerializedAs("t2")]
    public Text answerTextB;

    /// <summary>Label for answer C.</summary>
    [FormerlySerializedAs("t3")]
    public Text answerTextC;

    /// <summary>Label for answer D.</summary>
    [FormerlySerializedAs("t4")]
    public Text answerTextD;

    /// <summary>Readout for the current question number.</summary>
    [FormerlySerializedAs("qnum")]
    public TextMeshProUGUI questionNumberText;

    /// <summary>Readout for the total number of questions in this round.</summary>
    [FormerlySerializedAs("qmax")]
    public TextMeshProUGUI questionTotalText;

    /// <summary>Toggle for answer A.</summary>
    public Toggle toggle1;

    /// <summary>Toggle for answer B.</summary>
    public Toggle toggle2;

    /// <summary>Toggle for answer C.</summary>
    public Toggle toggle3;

    /// <summary>Toggle for answer D.</summary>
    public Toggle toggle4;

    /// <summary>Name of this encounter scene, unloaded once the round resolves.</summary>
    [FormerlySerializedAs("cavename")]
    public string encounterSceneName;

    /// <summary>Label carrying the Wumpus's in-character dialogue during a duel.</summary>
    [FormerlySerializedAs("textComponent")]
    public Text wumpusDialogueText;

    /// <summary>True while the correct answer is being displayed between questions.</summary>
    [FormerlySerializedAs("isTimed")]
    public bool isRevealingAnswer = false;

    /// <summary>Seconds left before the next question loads.</summary>
    [FormerlySerializedAs("timedtime")]
    public float revealCountdown = AnswerRevealSeconds;

    /// <summary>
    /// Questions in this round. Set per-scene in the inspector;
    /// <see cref="WumpusDuelQuestionCount"/> marks the round as the Wumpus duel.
    /// </summary>
    [FormerlySerializedAs("questions")]
    public int totalQuestions;

    /// <summary>Questions asked so far in this round.</summary>
    [FormerlySerializedAs("count")]
    public int questionsAsked = 0;

    /// <summary>Questions answered correctly so far in this round.</summary>
    [FormerlySerializedAs("right")]
    public int correctAnswers = 0;

    /// <summary>
    /// Which challenge this round belongs to: <c>"arrow"</c>, <c>"secret"</c>, <c>"wumpus"</c>
    /// or <c>"pit"</c>. Passed straight back to <see cref="PlayerScript"/>.
    /// </summary>
    [FormerlySerializedAs("usage")]
    public string challengeType;

    /// <summary>The correct answer for the question currently on screen.</summary>
    [FormerlySerializedAs("correctans")]
    public string correctAnswer;

    /// <summary>Random source used when drawing an offline fallback question.</summary>
    public System.Random rnd = new System.Random();

    private List<QuestionData> localQuestions = new List<QuestionData>();
    private string filePath;

    /// <summary>
    /// Loads the cached question bank, opens the Wumpus duel with a taunt if applicable,
    /// wires up the answer toggles, and asks the first question.
    /// </summary>
    private void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, QuestionCacheFileName);
        LoadCachedQuestions();

        if (IsWumpusDuel)
        {
            StartCoroutine(SpeakAsWumpus(WumpusGreetingPrompt));
        }

        toggle1.onValueChanged.AddListener(isOn => OnAnswerToggled(isOn, answerTextA));
        toggle2.onValueChanged.AddListener(isOn => OnAnswerToggled(isOn, answerTextB));
        toggle3.onValueChanged.AddListener(isOn => OnAnswerToggled(isOn, answerTextC));
        toggle4.onValueChanged.AddListener(isOn => OnAnswerToggled(isOn, answerTextD));

        StartAnswer();
    }

    /// <summary>True when this round is the five-question duel with the Wumpus.</summary>
    private bool IsWumpusDuel => totalQuestions == WumpusDuelQuestionCount;

    /// <summary>
    /// Advances the reveal timer and, once it expires, either resolves the round or moves on
    /// to the next question. Also persists the question cache.
    /// </summary>
    private void Update()
    {
        SaveCachedQuestions();

        if (isRevealingAnswer)
        {
            revealCountdown -= Time.deltaTime;
        }

        if (revealCountdown > 0.0f)
        {
            return;
        }

        if (correctAnswers > totalQuestions / 2)
        {
            ResolveRound(true);
        }
        else if (questionsAsked >= totalQuestions)
        {
            ResolveRound(false);
        }
        else
        {
            isRevealingAnswer = false;
            revealCountdown = AnswerRevealSeconds;
            StartAnswer();
        }
    }

    /// <summary>
    /// Clears the toggles, advances the question counter and requests the next question.
    /// </summary>
    public void StartAnswer()
    {
        ClearAnswerSelection();
        questionsAsked++;
        answerText.text = "";
        StartCoroutine(RequestQuestion());
    }

    /// <summary>
    /// Grades the player's pick, shows the correct answer, and charges them a coin for the attempt.
    /// Ignored while a previous answer is still being revealed.
    /// </summary>
    /// <param name="chosenAnswer">Full label of the answer the player selected, e.g. "B: 1854".</param>
    public void RevealAnswer(string chosenAnswer)
    {
        if (isRevealingAnswer)
        {
            return;
        }

        isRevealingAnswer = true;
        answerText.text = correctAnswer;

        if (StripAnswerLabel(correctAnswer) == StripAnswerLabel(chosenAnswer))
        {
            if (IsWumpusDuel)
            {
                StartCoroutine(SpeakAsWumpus(WumpusTookHitPrompt));
            }
            correctAnswers++;
            answerText.color = Color.green;
        }
        else
        {
            if (IsWumpusDuel)
            {
                StartCoroutine(SpeakAsWumpus(WumpusLandedHitPrompt));
            }
            answerText.color = Color.red;
        }

        GameObject playerObject = FindObjectInScene(MainSceneName, PlayerObjectName);
        if (playerObject != null)
        {
            playerObject.GetComponent<PlayerScript>().SendMessage("PayCoin");
        }
    }

    /// <summary>
    /// Extracts the assistant's message from a chat-completion response body.
    /// </summary>
    /// <param name="jsonResponse">Raw JSON body returned by the API.</param>
    /// <returns>The cleaned message text, or <c>null</c> when no content field is present.</returns>
    public static string ExtractMessage(string jsonResponse)
    {
        return OpenAIClient.ExtractMessageContent(jsonResponse);
    }

    /// <summary>
    /// Ends the round: narrates the outcome if this is the Wumpus duel, tells
    /// <see cref="PlayerScript"/> what happened, and unloads this scene.
    /// </summary>
    /// <param name="won">True when the player answered more than half the questions correctly.</param>
    private void ResolveRound(bool won)
    {
        if (IsWumpusDuel)
        {
            StartCoroutine(SpeakFinalLineAndResolve(won ? WumpusDefeatedPrompt : WumpusVictoryPrompt, won));
            return;
        }

        NotifyPlayer(won);
        UnloadEncounterScene();
    }

    /// <summary>
    /// Sends the round result to <see cref="PlayerScript"/> in the main scene.
    /// </summary>
    /// <param name="won">True when the player cleared the round.</param>
    private void NotifyPlayer(bool won)
    {
        GameObject playerObject = FindObjectInScene(MainSceneName, PlayerObjectName);
        if (playerObject != null)
        {
            playerObject.GetComponent<PlayerScript>()
                .SendMessage(won ? "CorrectAnswer" : "WrongAnswer", challengeType);
        }
    }

    /// <summary>Unloads this encounter scene, returning control to the map.</summary>
    private void UnloadEncounterScene()
    {
        Scene scene = SceneManager.GetSceneByName(encounterSceneName);
        if (scene != null)
        {
            SceneManager.UnloadSceneAsync(scene);
        }
    }

    /// <summary>
    /// Shows a generated line of Wumpus dialogue. Leaves the previous line in place when the
    /// API is unavailable.
    /// </summary>
    /// <param name="prompt">Scenario handed to the model.</param>
    /// <returns>A coroutine to be driven with <c>StartCoroutine</c>.</returns>
    private IEnumerator SpeakAsWumpus(string prompt)
    {
        return OpenAIClient.SendChatCompletion(
            prompt + NoQuotesInstruction,
            OpenAIClient.ShortResponseTokens,
            response =>
            {
                string line = OpenAIClient.ExtractMessageContent(response);
                if (!string.IsNullOrEmpty(line))
                {
                    wumpusDialogueText.text = line;
                }
            });
    }

    /// <summary>
    /// Speaks the Wumpus's closing line, then reports the result and unloads the scene.
    /// The result is reported whether or not the dialogue call succeeds.
    /// </summary>
    /// <param name="prompt">Scenario handed to the model.</param>
    /// <param name="won">True when the player cleared the round.</param>
    /// <returns>A coroutine to be driven with <c>StartCoroutine</c>.</returns>
    private IEnumerator SpeakFinalLineAndResolve(string prompt, bool won)
    {
        yield return SpeakAsWumpus(prompt);

        NotifyPlayer(won);
        UnloadEncounterScene();
    }

    /// <summary>
    /// Requests a fresh multiple-choice question and puts it on screen, falling back to the
    /// cached question bank when the call or the parse fails.
    /// </summary>
    /// <returns>A coroutine to be driven with <c>StartCoroutine</c>.</returns>
    private IEnumerator RequestQuestion()
    {
        return OpenAIClient.SendChatCompletion(
            QuestionPrompt,
            OpenAIClient.QuestionResponseTokens,
            DisplayQuestionFromResponse,
            _ => UseFallbackQuestion());
    }

    /// <summary>
    /// Parses a chat-completion response into a question and shows it, or falls back to the
    /// cached bank if any field is missing.
    /// </summary>
    /// <param name="response">Raw JSON body returned by the API.</param>
    private void DisplayQuestionFromResponse(string response)
    {
        QuestionData parsed = ParseQuestion(response);
        if (parsed == null)
        {
            UseFallbackQuestion();
            return;
        }

        localQuestions.Add(parsed);
        ShowQuestion(parsed);
    }

    /// <summary>
    /// Pulls the question, four answers and correct label out of a chat-completion response.
    /// </summary>
    /// <param name="jsonResponse">Raw JSON body returned by the API.</param>
    /// <returns>The parsed question, or <c>null</c> when the response did not match the format.</returns>
    private static QuestionData ParseQuestion(string jsonResponse)
    {
        Match match = Regex.Match(jsonResponse, QuestionPattern);
        if (!match.Success)
        {
            return null;
        }

        QuestionData parsed = new QuestionData
        {
            question = OpenAIClient.CleanText(match.Groups[1].Value),
            answerA = OpenAIClient.CleanText(match.Groups[2].Value),
            answerB = OpenAIClient.CleanText(match.Groups[3].Value),
            answerC = OpenAIClient.CleanText(match.Groups[4].Value),
            answerD = OpenAIClient.CleanText(match.Groups[5].Value),
            correct = OpenAIClient.CleanText(match.Groups[6].Value)
        };

        bool isComplete = !string.IsNullOrEmpty(parsed.question)
            && !string.IsNullOrEmpty(parsed.answerA)
            && !string.IsNullOrEmpty(parsed.answerB)
            && !string.IsNullOrEmpty(parsed.answerC)
            && !string.IsNullOrEmpty(parsed.answerD)
            && !string.IsNullOrEmpty(parsed.correct);

        return isComplete ? parsed : null;
    }

    /// <summary>Puts a question and its four answers on screen.</summary>
    /// <param name="data">The question to display.</param>
    private void ShowQuestion(QuestionData data)
    {
        questionNumberText.text = questionsAsked.ToString();
        questionTotalText.text = totalQuestions.ToString();
        questionText.text = data.question;

        answerTextA.text = "A: " + data.answerA;
        answerTextB.text = "B: " + data.answerB;
        answerTextC.text = "C: " + data.answerC;
        answerTextD.text = "D: " + data.answerD;

        correctAnswer = data.correct;
    }

    /// <summary>
    /// Draws a question from the offline cache, or a hardcoded placeholder when the cache is empty.
    /// </summary>
    private void UseFallbackQuestion()
    {
        ClearAnswerSelection();

        QuestionData fallback = localQuestions != null && localQuestions.Count > 0
            ? localQuestions[rnd.Next(localQuestions.Count)]
            : PlaceholderQuestion();

        ShowQuestion(fallback);
    }

    /// <summary>Last-resort question used when no question has ever been cached.</summary>
    /// <returns>A hardcoded placeholder question.</returns>
    private static QuestionData PlaceholderQuestion()
    {
        return new QuestionData
        {
            question = "skibidi toilet",
            answerA = "rizzler",
            answerB = "ew no",
            answerC = "GET OUT",
            answerD = "D",
            correct = "rizzler"
        };
    }

    /// <summary>Unchecks all four answer toggles.</summary>
    private void ClearAnswerSelection()
    {
        toggle1.isOn = false;
        toggle2.isOn = false;
        toggle3.isOn = false;
        toggle4.isOn = false;
    }

    /// <summary>Grades the matching answer when one of the four toggles is switched on.</summary>
    /// <param name="isOn">Whether the toggle was switched on rather than off.</param>
    /// <param name="answerLabel">Label holding the answer text tied to that toggle.</param>
    private void OnAnswerToggled(bool isOn, Text answerLabel)
    {
        if (isOn)
        {
            RevealAnswer(answerLabel.text);
        }
    }

    /// <summary>Drops the leading "A:" style prefix so two answers can be compared on text alone.</summary>
    /// <param name="answer">A full answer label.</param>
    /// <returns>The answer text without its letter prefix.</returns>
    private static string StripAnswerLabel(string answer)
    {
        return answer.Substring(AnswerLabelLength, answer.Length - AnswerLabelLength);
    }

    /// <summary>Reads the cached question bank from disk, if one has been written.</summary>
    private void LoadCachedQuestions()
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("No questions file found.");
            return;
        }

        string json = File.ReadAllText(filePath);
        QuestionFile data = JsonUtility.FromJson<QuestionFile>(json);
        localQuestions = data.questions ?? new List<QuestionData>();
    }

    /// <summary>Writes the cached question bank back to disk.</summary>
    private void SaveCachedQuestions()
    {
        QuestionFile data = new QuestionFile { questions = localQuestions };
        File.WriteAllText(filePath, JsonUtility.ToJson(data, true));
    }

    /// <summary>
    /// Finds a root GameObject by name inside a loaded scene.
    /// </summary>
    /// <param name="sceneName">Scene to search.</param>
    /// <param name="objectName">Name of the root GameObject to find.</param>
    /// <returns>The GameObject, or <c>null</c> when the scene is not loaded or has no such object.</returns>
    private GameObject FindObjectInScene(string sceneName, string objectName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
        {
            return null;
        }

        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            if (obj.name == objectName)
            {
                return obj;
            }
        }
        return null;
    }
}
