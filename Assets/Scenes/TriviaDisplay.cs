using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    /// <summary>Every question is multiple choice with exactly this many options.</summary>
    public const int AnswerCount = 4;

    private const string QuestionCacheFileName = "newdata.json";

    private const string QuestionPrompt =
        "Generate a weird, truly unique trivia question with exactly four multiple-choice answers. "
        + "The four answers must all be different from each other, and correctIndex must be the "
        + "zero-based position of the correct one.";

    /// <summary>Schema name sent to the API; must match <c>^[a-zA-Z0-9_-]+$</c>.</summary>
    private const string QuestionSchemaName = "trivia_question";

    /// <summary>
    /// Shape the model must return. Field names line up with <see cref="QuestionData"/> so the
    /// response deserializes straight into it, and <c>additionalProperties:false</c> plus the full
    /// <c>required</c> list are what OpenAI's strict mode demands.
    /// </summary>
    private const string QuestionSchema =
        "{\"type\":\"object\",\"properties\":{"
        + "\"question\":{\"type\":\"string\"},"
        + "\"answers\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}},"
        + "\"correctIndex\":{\"type\":\"integer\"}},"
        + "\"required\":[\"question\",\"answers\",\"correctIndex\"],"
        + "\"additionalProperties\":false}";

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

    private const string MainSceneName = "MainScene";

    /// <summary>Name of the GameObject carrying <see cref="PlayerScript"/> in the main scene.</summary>
    private const string PlayerObjectName = "sprite";

    /// <summary>
    /// A single multiple-choice question. Field names double as the API's JSON schema, so
    /// renaming one means editing <see cref="QuestionSchema"/> to match.
    /// </summary>
    [System.Serializable]
    public class QuestionData
    {
        public string question;
        public string[] answers;

        /// <summary>Zero-based position of the correct entry in <see cref="answers"/>.</summary>
        public int correctIndex;
    }

    /// <summary>Serialization wrapper for the on-disk question cache.</summary>
    [System.Serializable]
    public class QuestionFile
    {
        public List<QuestionData> questions;
    }

    public TextMeshProUGUI questionText;

    /// <summary>Label showing the correct answer once the player has picked.</summary>
    public TextMeshProUGUI answerText;

    [FormerlySerializedAs("t1")]
    public Text answerTextA;

    [FormerlySerializedAs("t2")]
    public Text answerTextB;

    [FormerlySerializedAs("t3")]
    public Text answerTextC;

    [FormerlySerializedAs("t4")]
    public Text answerTextD;

    [FormerlySerializedAs("qnum")]
    public TextMeshProUGUI questionNumberText;

    [FormerlySerializedAs("qmax")]
    public TextMeshProUGUI questionTotalText;

    public Toggle toggle1;
    public Toggle toggle2;
    public Toggle toggle3;
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

    [FormerlySerializedAs("count")]
    public int questionsAsked = 0;

    [FormerlySerializedAs("right")]
    public int correctAnswers = 0;

    /// <summary>
    /// Which challenge this round belongs to: <c>"arrow"</c>, <c>"secret"</c>, <c>"wumpus"</c>
    /// or <c>"pit"</c>. Passed straight back to <see cref="PlayerScript"/>.
    /// </summary>
    [FormerlySerializedAs("usage")]
    public string challengeType;

    /// <summary>Text of the correct answer for the question currently on screen.</summary>
    [FormerlySerializedAs("correctans")]
    public string correctAnswer;

    /// <summary>Random source for drawing fallback questions and shuffling answer order.</summary>
    public System.Random rnd = new System.Random();

    private List<QuestionData> localQuestions = new List<QuestionData>();
    private string filePath;

    /// <summary>Which on-screen slot holds the correct answer, after shuffling.</summary>
    private int correctSlot;

    /// <summary>Guards against the round resolving more than once while the scene unloads.</summary>
    private bool hasResolved;

    private void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, QuestionCacheFileName);
        LoadCachedQuestions();

        if (IsWumpusDuel)
        {
            StartCoroutine(SpeakAsWumpus(WumpusGreetingPrompt));
        }

        toggle1.onValueChanged.AddListener(isOn => OnAnswerToggled(isOn, 0));
        toggle2.onValueChanged.AddListener(isOn => OnAnswerToggled(isOn, 1));
        toggle3.onValueChanged.AddListener(isOn => OnAnswerToggled(isOn, 2));
        toggle4.onValueChanged.AddListener(isOn => OnAnswerToggled(isOn, 3));

        StartAnswer();
    }

    /// <summary>True when this round is the five-question duel with the Wumpus.</summary>
    private bool IsWumpusDuel => totalQuestions == WumpusDuelQuestionCount;

    /// <summary>
    /// Advances the reveal timer and, once it expires, either resolves the round or moves on
    /// to the next question.
    /// </summary>
    private void Update()
    {
        if (hasResolved)
        {
            return;
        }

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
    /// <param name="chosenSlot">Zero-based on-screen slot the player selected.</param>
    public void RevealAnswer(int chosenSlot)
    {
        if (isRevealingAnswer)
        {
            return;
        }

        isRevealingAnswer = true;
        answerText.text = correctAnswer;

        if (chosenSlot == correctSlot)
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
    /// Ends the round: narrates the outcome if this is the Wumpus duel, tells
    /// <see cref="PlayerScript"/> what happened, and unloads this scene.
    /// </summary>
    /// <param name="won">True when the player answered more than half the questions correctly.</param>
    private void ResolveRound(bool won)
    {
        // Unloading a scene takes at least a frame, so without this the round would report its
        // result once per frame until the scene actually went away, paying the reward each time.
        hasResolved = true;

        if (IsWumpusDuel)
        {
            StartCoroutine(SpeakFinalLineAndResolve(won ? WumpusDefeatedPrompt : WumpusVictoryPrompt, won));
            return;
        }

        NotifyPlayer(won);
        UnloadEncounterScene();
    }

    private void NotifyPlayer(bool won)
    {
        GameObject playerObject = FindObjectInScene(MainSceneName, PlayerObjectName);
        if (playerObject != null)
        {
            playerObject.GetComponent<PlayerScript>()
                .SendMessage(won ? "CorrectAnswer" : "WrongAnswer", challengeType);
        }
    }

    private void UnloadEncounterScene()
    {
        Scene scene = SceneManager.GetSceneByName(encounterSceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(scene);
        }
    }

    /// <summary>
    /// Shows a generated line of Wumpus dialogue. Leaves the previous line in place when the
    /// API is unavailable.
    /// </summary>
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
    private IEnumerator SpeakFinalLineAndResolve(string prompt, bool won)
    {
        yield return SpeakAsWumpus(prompt);

        NotifyPlayer(won);
        UnloadEncounterScene();
    }

    /// <summary>
    /// Requests a fresh multiple-choice question as schema-constrained JSON, falling back to the
    /// cached question bank when every attempt fails.
    /// </summary>
    private IEnumerator RequestQuestion()
    {
        return OpenAIClient.SendStructuredCompletion(
            QuestionPrompt,
            OpenAIClient.QuestionResponseTokens,
            QuestionSchemaName,
            QuestionSchema,
            content => ParseQuestion(content) != null,
            DisplayQuestionFromContent,
            _ => UseFallbackQuestion());
    }

    /// <summary>Shows a validated question and adds it to the offline bank.</summary>
    /// <param name="content">The assistant's JSON content, already checked by the validator.</param>
    private void DisplayQuestionFromContent(string content)
    {
        QuestionData parsed = ParseQuestion(content);
        if (parsed == null)
        {
            UseFallbackQuestion();
            return;
        }

        localQuestions.Add(parsed);
        SaveCachedQuestions();
        ShowQuestion(parsed);
    }

    /// <summary>
    /// Deserializes a question and checks it is actually usable. The schema guarantees the fields
    /// exist, but not that there are four distinct answers or that the index points at one of them,
    /// so those are checked here and a failure sends the caller back for another attempt.
    /// </summary>
    /// <param name="content">The assistant's JSON content.</param>
    /// <returns>The parsed question, or <c>null</c> when it cannot be trusted.</returns>
    public static QuestionData ParseQuestion(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        QuestionData parsed;
        try
        {
            parsed = JsonUtility.FromJson<QuestionData>(content);
        }
        catch (System.Exception)
        {
            return null;
        }

        if (parsed == null || string.IsNullOrWhiteSpace(parsed.question))
        {
            return null;
        }

        if (parsed.answers == null || parsed.answers.Length != AnswerCount)
        {
            return null;
        }

        if (parsed.correctIndex < 0 || parsed.correctIndex >= AnswerCount)
        {
            return null;
        }

        for (int i = 0; i < parsed.answers.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(parsed.answers[i]))
            {
                return null;
            }

            for (int j = i + 1; j < parsed.answers.Length; j++)
            {
                if (string.Equals(parsed.answers[i], parsed.answers[j], System.StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
            }
        }

        return parsed;
    }

    /// <summary>
    /// Puts a question on screen with its answers in a random order, so the correct one is not
    /// always in the position the model happened to put it in.
    /// </summary>
    private void ShowQuestion(QuestionData data)
    {
        questionNumberText.text = questionsAsked.ToString();
        questionTotalText.text = totalQuestions.ToString();
        questionText.text = data.question;

        int[] slots = ShuffledSlots();
        Text[] labels = { answerTextA, answerTextB, answerTextC, answerTextD };
        string[] prefixes = { "A: ", "B: ", "C: ", "D: " };

        for (int slot = 0; slot < AnswerCount; slot++)
        {
            int sourceIndex = slots[slot];
            labels[slot].text = prefixes[slot] + data.answers[sourceIndex];

            if (sourceIndex == data.correctIndex)
            {
                correctSlot = slot;
                correctAnswer = prefixes[slot] + data.answers[sourceIndex];
            }
        }
    }

    /// <summary>Fisher-Yates permutation of the four answer positions.</summary>
    private int[] ShuffledSlots()
    {
        int[] slots = new int[AnswerCount];
        for (int i = 0; i < AnswerCount; i++)
        {
            slots[i] = i;
        }

        for (int i = AnswerCount - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (slots[i], slots[j]) = (slots[j], slots[i]);
        }

        return slots;
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
    private static QuestionData PlaceholderQuestion()
    {
        return new QuestionData
        {
            question = "skibidi toilet",
            answers = new[] { "rizzler", "ew no", "GET OUT", "D" },
            correctIndex = 0
        };
    }

    private void ClearAnswerSelection()
    {
        toggle1.isOn = false;
        toggle2.isOn = false;
        toggle3.isOn = false;
        toggle4.isOn = false;
    }

    /// <summary>Grades the matching slot when one of the four toggles is switched on.</summary>
    private void OnAnswerToggled(bool isOn, int slot)
    {
        if (isOn)
        {
            RevealAnswer(slot);
        }
    }

    /// <summary>
    /// Reads the cached question bank from disk, dropping any entry that no longer passes
    /// validation so a stale or hand-edited cache cannot put a broken question on screen.
    /// </summary>
    private void LoadCachedQuestions()
    {
        localQuestions = new List<QuestionData>();

        if (!File.Exists(filePath))
        {
            return;
        }

        QuestionFile data;
        try
        {
            data = JsonUtility.FromJson<QuestionFile>(File.ReadAllText(filePath));
        }
        catch (System.Exception error)
        {
            Debug.LogWarning($"Could not read the question cache: {error.Message}");
            return;
        }

        if (data?.questions == null)
        {
            return;
        }

        foreach (QuestionData question in data.questions)
        {
            if (IsUsable(question))
            {
                localQuestions.Add(question);
            }
        }
    }

    /// <summary>The same checks <see cref="ParseQuestion"/> applies, for an already-deserialized question.</summary>
    private static bool IsUsable(QuestionData question)
    {
        return question != null
            && !string.IsNullOrWhiteSpace(question.question)
            && question.answers != null
            && question.answers.Length == AnswerCount
            && question.correctIndex >= 0
            && question.correctIndex < AnswerCount;
    }

    /// <summary>Writes the question bank to disk. Called when the bank actually changes.</summary>
    private void SaveCachedQuestions()
    {
        QuestionFile data = new QuestionFile { questions = localQuestions };
        File.WriteAllText(filePath, JsonUtility.ToJson(data, true));
    }

    /// <summary>
    /// Finds a root GameObject by name inside a loaded scene.
    /// </summary>
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
