using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Wires the "play again" button on the win and lose screens back to the main scene.
/// </summary>
public class LoadMainScene : MonoBehaviour
{
    /// <summary>Scene loaded when the button is pressed.</summary>
    private const string MainSceneName = "MainScene";

    /// <summary>The replay button. Assign this in the inspector.</summary>
    public Button yourButton;

    /// <summary>Subscribes to the replay button, if one has been assigned.</summary>
    private void Start()
    {
        if (yourButton != null)
        {
            yourButton.onClick.AddListener(OnButtonPress);
        }
    }

    /// <summary>Restarts the game by loading the main scene.</summary>
    private void OnButtonPress()
    {
        SceneManager.LoadScene(MainSceneName);
    }
}
