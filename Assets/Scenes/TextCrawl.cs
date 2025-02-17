using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Creates a Star Wars-like scrolling text
// Calls the OpenAI web API to generate a backstory about Hunt the Wumpus
// Makes the text slowly move upwards
public class TextCrawl : MonoBehaviour
{
    public float scrollSpeed = 50000f;
    public Text textComponent;
    public RectTransform textRectTransform;

    // Calls OpenAI to create a backstory using ChatGPT
    private IEnumerator CallOpenAI()
    {
        
        textComponent.text = "Long ago, in a hidden world beneath the earth's surface, there existed an advanced civilization. These people were masters of both technology and magic, living in harmony with the cavernous depths they called home. They created a maze of tunnels and chambers, filled with puzzles and traps to protect their most sacred treasures and secrets. As guardians of their hidden realm, they engineered a creature known as the Wumpus. This beast was not evil but was designed to be the ultimate protector, shrouded in mystery and endowed with powers that could manipulate the very tunnels it roamed. The Wumpus was meant to deter intruders, guarding the civilization's treasures from those unworthy of its secrets. However, over centuries, the civilization vanished, leaving behind only their labyrinth and the Wumpus. The maze, now a stuff of legends on the surface, attracts adventurers and treasure seekers. Brave souls enter the depths, not just in search of treasure, but to uncover the lost stories of the ancient world, all while avoiding the ever-watchful eyes of the Wumpus, which continues to fulfill its duty as guardian of the forgotten civilization.";
        yield return null;
    }


    // Parses response
    private string ExtractGeneratedText(string jsonResponse)
    {
        string pattern = "\"content\": \"(.*?)\"";
        var match = System.Text.RegularExpressions.Regex.Match(jsonResponse, pattern);
        if (match.Success)
        {
            return Regex.Replace(match.Groups[1].Value, @"[\\\/]", "");
        }
        return null;
    }

    void Start()
    {
        textComponent.text = "";
        if (textComponent == null)
        {
            textComponent = GetComponent<Text>();
        }

        if (textRectTransform == null)
        {
            textRectTransform = textComponent.GetComponent<RectTransform>();
        }

        // Set the initial position of the text
        textRectTransform.anchoredPosition = new Vector2(0, -Screen.height);
        StartCoroutine(CallOpenAI());
    }

    void Update()
    {
        if (textComponent.text != "")
        {
            // Move the text upwards
            textRectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);

            // Optionally, reset the position if it moves off the screen (looping effect)
            if (textRectTransform.anchoredPosition.y > 2000)
            {
                SceneManager.LoadScene("MainScene");
            }
        }
        
    }
}
