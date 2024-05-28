using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro; // Namespace for TextMeshPro elements

public class TriviaDisplay : MonoBehaviour
{
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI answerText;
    public TriviaGenerator triviaGenerator;
    public TriviaQuestion question;
    public Button button1;
    
    public string cavename;

    public Button button2;
    
    public Button button3;
    
    public Button button4;

    public TextMeshProUGUI t1;
    
    public TextMeshProUGUI t2;
    
    public TextMeshProUGUI t3;
    
    public TextMeshProUGUI t4;

    public bool isTimed = false;

    public float timedtime = 2.0f;

    public static TriviaDisplay instance = null;

    void Start()
    {
        Button btn1 = button1.GetComponent<Button>();
	    btn1.onClick.AddListener(TaskOnClick);
        Button btn2 = button2.GetComponent<Button>();
	    btn2.onClick.AddListener(TaskOnClick);
        Button btn3 = button3.GetComponent<Button>();
	    btn3.onClick.AddListener(TaskOnClick);
        Button btn4 = button4.GetComponent<Button>();
	    btn4.onClick.AddListener(TaskOnClick);
        StartAnswer();
    }

    void Update() {
        if (isTimed) {
            timedtime -= Time.deltaTime;
        }
        if (timedtime <= 0.0f) {
            
            SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(cavename));
        }
        
    }

    public void StartAnswer()
    {
        // Optionally hide the answer text initially
        answerText.text = "";

        // Get a random question and display it
        question = triviaGenerator.GetRandomQuestion();
        questionText.text = question.Question;
        t1.text = question.Answer1;
        t2.text = question.Answer2;
        t3.text = question.Answer3;
        t4.text = question.Answer4;
        Debug.Log("Skibidi toilet");
    }

    // Call this method when the button is clicked to reveal the answer
    public void RevealAnswer()
    {
        isTimed = true;
        answerText.text = question.Answer;
    }

    void TaskOnClick() {
		Debug.Log (t1.text);
        RevealAnswer();
	}

    void TaskOnClick2()
    {
        Debug.Log(t2.text);
        RevealAnswer();
    }

    void TaskOnClick3()
    {
        Debug.Log(t3.text);
        RevealAnswer();
    }

    void TaskOnClick4()
    {
        Debug.Log(t4.text);
        RevealAnswer();
    }
}

