using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro; // Namespace for TextMeshPro elements
using UnityEngine.Networking;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


// Displays an AI-generated multiple-choice trivia question and four possible answers
// Validates the answer
// Keeps a tally of total correct answers
// Notifies the player of the overall result
// Generates lines for the wumpus based on the result
public class TriviaDisplay : MonoBehaviour
{
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI answerText;
    public ToggleGroup choice;
    public Text t1;
    public Text t2;
    public Text t3;
    public Text t4;
    public Toggle toggle1;
    public Toggle toggle2;
    public Toggle toggle3;
    public Toggle toggle4;
    public string cavename;

    public Text textComponent;

    public bool isTimed = false;

    public float timedtime = 2.0f;

    public static TriviaDisplay instance = null;

    public int questions;
    private string questionString;

    public int count = 0;

    public int right = 0;

    public string usage;

    public string correctans;

    public System.Random rnd = new System.Random();

    private static readonly HttpClient httpClient = new HttpClient();

    // Calls OpenAI based on a given prompt
    private IEnumerator CallOpenAI_WumpusChat(string prompt)
    {
        string apiKey = "REDACTED-OPENAI-KEY"; // Replace with your OpenAI API key
        string url = "https://api.openai.com/v1/chat/completions";

        // Create the JSON request payload
        string jsonContent = "{\"model\": \"gpt-3.5-turbo\", \"messages\": [{\"role\": \"user\", \"content\": \"" + prompt + "Don't include any quotes." + "\"}], \"max_tokens\": 50}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonContent);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            Debug.Log("Sending request to OpenAI API...");
            Debug.Log("Request URL: " + url);
            Debug.Log("Request JSON: " + jsonContent);

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError($"Error: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
            else
            {
                string responseContent = request.downloadHandler.text;
                Debug.Log("Received response from OpenAI API");
                Debug.Log("Response: " + responseContent);

                // Parse the JSON response manually
                string generatedText = ExtractMessage(responseContent);
                if (!string.IsNullOrEmpty(generatedText))
                {
                    Debug.Log("Response text: " + generatedText);
                    textComponent.text = generatedText;
                }
                else
                {
                    Debug.LogWarning("No text found in the response.");
                }
            }
        }
    }

    // Calls OpenAI, notifies the player of the result, and ends the trivia minigame
    private IEnumerator CallOpenAI_WumpusChatFinal(string prompt, bool isWin)
    {
        string apiKey = "REDACTED-OPENAI-KEY"; // Replace with your OpenAI API key
        string url = "https://api.openai.com/v1/chat/completions";

        // Create the JSON request payload
        string jsonContent = "{\"model\": \"gpt-3.5-turbo\", \"messages\": [{\"role\": \"user\", \"content\": \"" + prompt + "Don't include any quotes." + "\"}], \"max_tokens\": 50}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonContent);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            Debug.Log("Sending request to OpenAI API...");
            Debug.Log("Request URL: " + url);
            Debug.Log("Request JSON: " + jsonContent);

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError($"Error: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
            else
            {
                string responseContent = request.downloadHandler.text;
                Debug.Log("Received response from OpenAI API");
                Debug.Log("Response: " + responseContent);

                // Parse the JSON response manually
                string generatedText = ExtractMessage(responseContent);
                if (!string.IsNullOrEmpty(generatedText))
                {
                    Debug.Log("Response text: " + generatedText);
                    textComponent.text = generatedText;
                }
                else
                {
                    Debug.LogWarning("No text found in the response.");
                }
            }
        }
        var component = FindObjectInScene("MainScene", "sprite");
        if (isWin)
        {
            component.GetComponent<PlayerScript>().SendMessage("CorrectAnswer", usage);
        }
        else
        {
            component.GetComponent<PlayerScript>().SendMessage("WrongAnswer", usage);
        }
        var scene = SceneManager.GetSceneByName(cavename);
        if (scene != null)
        {
            SceneManager.UnloadSceneAsync(scene);
        }
    }

    // Generates a question using OpenAI
    private IEnumerator CallOpenAI_Question()
    {
        Dictionary<int, Text> p = new Dictionary<int, Text>();
        p.Add(0, t1);
        p.Add(1, t2);
        p.Add(2, t3);
        p.Add(3, t4);

        string apiKey = "REDACTED-OPENAI-KEY"; // Replace with your OpenAI API key
        string url = "https://api.openai.com/v1/chat/completions";

        // Create the JSON request payload
        string jsonContent = "{\"model\": \"gpt-3.5-turbo\", \"messages\": [{\"role\": \"user\", \"content\": \"Generate a trivia question with four multiple-choice answers. Clearly label four answers and the one correct answer. Use the format: Question: <question>\\nA: <answer1>\\nB: <answer2>\\nC: <answer3>\\nD: <answer4>\\nCorrect: <correctans>\"}], \"max_tokens\": 100}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonContent);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            Debug.Log("Sending request to OpenAI API...");
            Debug.Log("Request URL: " + url);
            Debug.Log("Request JSON: " + jsonContent);

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError($"Error: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
            else
            {
                string responseContent = request.downloadHandler.text;
                Debug.Log("Received response from OpenAI API");
                Debug.Log("Response: " + responseContent);

                // Parse the JSON response manually
                string question, answerA, answerB, answerC, answerD;
                ParseQuestionAndAnswers(responseContent, out question, out answerA, out answerB, out answerC, out answerD, out correctans);

                if (!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(answerA) && !string.IsNullOrEmpty(answerB) && !string.IsNullOrEmpty(answerC) && !string.IsNullOrEmpty(answerD) && !string.IsNullOrEmpty(correctans))
                {
                    Debug.Log("Parsed Question: " + question);
                    Debug.Log("A: " + answerA);
                    Debug.Log("B: " + answerB);
                    Debug.Log("C: " + answerC);
                    Debug.Log("D: " + answerD);
                    Debug.Log("Correct Answer: " + correctans);

                    SetQuestionText(question);
                    p[0].text = "A: " + answerA;
                    p[1].text = "B: " + answerB;
                    p[2].text = "C: " + answerC;
                    p[3].text = "D: " + answerD;

                    List<int> keys = new List<int>(p.Keys);
                    for (int i = 0; i < keys.Count; i++)
                    {
                        int j = Random.Range(0, keys.Count);
                        int temp = keys[i];
                        keys[i] = keys[j];
                        keys[j] = temp;
                    }
                }
                else
                {
                    Debug.LogWarning("Failed to parse the response correctly.");
                }
            }
        }
    }

    private void ParseQuestionAndAnswers(string jsonResponse, out string question, out string answerA, out string answerB, out string answerC, out string answerD, out string correctAnswer)
    {
        // Initialize outputs
        question = answerA = answerB = answerC = answerD = correctAnswer = null;

        // Extract content using a regular expression
        string pattern = "\"content\":\\s*\"Question: (.*?)\\\\nA: (.*?)\\\\nB: (.*?)\\\\nC: (.*?)\\\\nD: (.*?)\\\\nCorrect: (.*?)\"";
        var match = Regex.Match(jsonResponse, pattern);
        if (match.Success)
        {
            question = match.Groups[1].Value;
            answerA = match.Groups[2].Value;
            answerB = match.Groups[3].Value;
            answerC = match.Groups[4].Value;
            answerD = match.Groups[5].Value;
            correctAnswer = match.Groups[6].Value;
        }
    }

    private void SetQuestionText(string qt)
    {
        questionText.text = "Question #" + count + " of " + questions + ":\n" + qt;
        Debug.Log("Setting question text: " + qt);
    }

    private int DetermineCorrectAnswerIndex(string answerA, string answerB, string answerC, string answerD)
    {
        // Simple logic to determine the correct answer index
        if (answerA.Contains("(correct)")) return 0;
        if (answerB.Contains("(correct)")) return 1;
        if (answerC.Contains("(correct)")) return 2;
        if (answerD.Contains("(correct)")) return 3;
        return -1; // No correct answer found
    }


    private string ExtractGeneratedText(string jsonResponse)
    {
        string pattern = "\"content\": \"(.*?)\"";
        var match = System.Text.RegularExpressions.Regex.Match(jsonResponse, pattern);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return null;
    }

    public static string ExtractMessage(string jsonResponse)
    {
        string pattern = "\"content\": \"(.*?)\"";
        var match = System.Text.RegularExpressions.Regex.Match(jsonResponse, pattern);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return null;
    }

    private GameObject FindObjectInScene(string sceneName, string objectName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.isLoaded)
        {
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                if (obj.name == objectName)
                {
                    return obj;
                }
            }
        }
        return null;
    }

    void Start()
    {
        //check if wumpus room
        if (questions == 5)
        {
            StartCoroutine(CallOpenAI_WumpusChat("Imagine you are the wumpus and the player has came to hunt you. What do you say?"));
        }
        toggle1.onValueChanged.AddListener(TaskOnClick1);
        toggle2.onValueChanged.AddListener(TaskOnClick2);
        toggle3.onValueChanged.AddListener(TaskOnClick3);
        toggle4.onValueChanged.AddListener(TaskOnClick4);

        StartAnswer();
    }

    void Update() {
        if (isTimed) {
            timedtime -= Time.deltaTime;
        }
        if (timedtime <= 0.0f) {
            if (right > questions / 2)
            {

                if (questions == 5)
                {
                    StartCoroutine(CallOpenAI_WumpusChatFinal("Imagine you are the wumpus and the player just defeated you, but you can run away. What do you say?", true));
                }
                else
                {
                    var component = FindObjectInScene("MainScene", "sprite");
                    if (component != null)
                    {
                        component.GetComponent<PlayerScript>().SendMessage("CorrectAnswer", usage);
                    }
                    var scene = SceneManager.GetSceneByName(cavename);
                    if (scene != null)
                    {
                        SceneManager.UnloadSceneAsync(scene);
                    }
                }
            }
            else if (count >= questions)
            {
                if (questions == 5)
                {
                    StartCoroutine(CallOpenAI_WumpusChatFinal("Imagine you are the wumpus and you are about to kill the pesky hunter who tried to kill you. What do you say?", false));
                }
                else
                {
                    var component = FindObjectInScene("MainScene", "sprite");
                    if (component != null)
                    {
                        component.GetComponent<PlayerScript>().SendMessage("WrongAnswer", usage);
                    }
                    var scene = SceneManager.GetSceneByName(cavename);
                    if (scene != null)
                    {
                        SceneManager.UnloadSceneAsync(scene);
                    }
                }
            }
            else
            {
                isTimed = false;
                timedtime = 2.0f;
                StartAnswer();
            }

        }
        
    }

    public void StartAnswer()
    {
        toggle1.isOn = false;
        toggle2.isOn = false;
        toggle3.isOn = false;
        toggle4.isOn = false;
        count++;
        // Optionally hide the answer text initially
        answerText.text = "";
        StartCoroutine(CallOpenAI_Question());

    }

    // Call this method when the button is clicked to reveal the answer
    public void RevealAnswer(string ans)
    {
        if (!isTimed)
        {
            isTimed = true;
            answerText.text = correctans;
            if (correctans.Substring(2,correctans.Length-2) == ans.Substring(2,ans.Length-2))
            {
                if (questions == 5)
                {
                    StartCoroutine(CallOpenAI_WumpusChat("Imagine you are the wumpus and the player has landed a hit on you. What do you say?"));
                }
                
                right++;
                answerText.color = Color.green;
            }
            else
            {
                if (questions == 5)
                {
                    StartCoroutine(CallOpenAI_WumpusChat("Imagine you are the wumpus and you landed a blow on the the pesky hunter trying to kill you. What do you say?"));
                }
                answerText.color = Color.red;
            }
            var component = FindObjectInScene("MainScene", "sprite");
            if (component)
            {
                component.GetComponent<PlayerScript>().SendMessage("payCoin");
            }

        }
    }

    // Validates the answers when user chooses an option
    void TaskOnClick1(bool isOn) {
        if (isOn)
        {
            Debug.Log(t1.text);
            RevealAnswer(t1.text);
        }
		
	}

    void TaskOnClick2(bool isOn)
    {
        if (isOn)
        {
            Debug.Log(t2.text);
            RevealAnswer(t2.text);
        }
        
    }

    void TaskOnClick3(bool isOn)
    {
        if (isOn)
        {
            Debug.Log(t3.text);
            RevealAnswer(t3.text);
        }
        
    }

    void TaskOnClick4(bool isOn)
    {
        if (isOn)
        {
            Debug.Log(t4.text);
            RevealAnswer(t4.text);
        }
        
    }
}

