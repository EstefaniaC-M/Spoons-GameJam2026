using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverr : MonoBehaviour
{
    public GameObject panel;

    void Start()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Show()
    {
        panel.SetActive(true);
        Time.timeScale = 0f;   // freezes the game behind the screen
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // temporary test: press G to show the screen
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) Show();
    }
}