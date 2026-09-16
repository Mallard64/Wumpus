using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Wires the "play again" button on the win and lose screens back to the main scene.
/// </summary>
public class LoadMainScene : MonoBehaviour
{
    private const string MainSceneName = "MainScene";

    public Button yourButton;

    private void Start()
    {
        if (yourButton != null)
        {
            yourButton.onClick.AddListener(OnButtonPress);
        }
    }

    private void OnButtonPress()
    {
        SceneManager.LoadScene(MainSceneName);
    }
}
