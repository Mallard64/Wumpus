using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Star Wars style opening crawl. Scrolls the game's backstory up the screen and hands off to the
/// main scene once the text has cleared the top.
/// </summary>
public class TextCrawl : MonoBehaviour
{
    /// <summary>Anchored Y position at which the crawl is considered finished.</summary>
    private const float CrawlEndY = 2000f;

    private const string MainSceneName = "MainScene";

    private const string BackstoryText =
        "Long ago, in a hidden world beneath the earth's surface, there existed an advanced civilization. "
        + "These people were masters of both technology and magic, living in harmony with the cavernous depths "
        + "they called home. They created a maze of tunnels and chambers, filled with puzzles and traps to protect "
        + "their most sacred treasures and secrets. As guardians of their hidden realm, they engineered a creature "
        + "known as the Wumpus. This beast was not evil but was designed to be the ultimate protector, shrouded in "
        + "mystery and endowed with powers that could manipulate the very tunnels it roamed. The Wumpus was meant to "
        + "deter intruders, guarding the civilization's treasures from those unworthy of its secrets. However, over "
        + "centuries, the civilization vanished, leaving behind only their labyrinth and the Wumpus. The maze, now a "
        + "stuff of legends on the surface, attracts adventurers and treasure seekers. Brave souls enter the depths, "
        + "not just in search of treasure, but to uncover the lost stories of the ancient world, all while avoiding "
        + "the ever-watchful eyes of the Wumpus, which continues to fulfill its duty as guardian of the forgotten "
        + "civilization.";

    /// <summary>Scroll speed in anchored units per second.</summary>
    public float scrollSpeed = 50000f;

    public Text textComponent;

    /// <summary>Transform moved upwards each frame to produce the scroll.</summary>
    public RectTransform textRectTransform;

    private void Start()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<Text>();
        }

        if (textRectTransform == null)
        {
            textRectTransform = textComponent.GetComponent<RectTransform>();
        }

        textComponent.text = "";

        textRectTransform.anchoredPosition = new Vector2(0, -Screen.height);
        StartCoroutine(ShowBackstory());
    }

    private void Update()
    {
        if (textComponent.text == "")
        {
            return;
        }

        textRectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);

        if (textRectTransform.anchoredPosition.y > CrawlEndY)
        {
            SceneManager.LoadScene(MainSceneName);
        }
    }

    /// <summary>
    /// Puts the backstory on screen. Kept as a coroutine so it can be swapped back to a live
    /// generated intro without changing the call site.
    /// </summary>
    private IEnumerator ShowBackstory()
    {
        textComponent.text = BackstoryText;
        yield return null;
    }
}
