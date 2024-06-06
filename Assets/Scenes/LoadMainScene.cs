using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadMainScene : MonoBehaviour
{
    public Button yourButton; // Drag your button here in the Inspector

    void Start()
    {
        if (yourButton != null)
        {
            yourButton.onClick.AddListener(OnButtonPress);
        }
    }

    void OnButtonPress()
    {
        SceneManager.LoadScene("MainScene");
    }
}
