using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController instance;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;

        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }


    public void LoadScene(int sceneIndex)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
        UpdateMusicForScene(sceneIndex);
    }

    public void PauseTime()
    {
        Time.timeScale = 0f;

    }

    public void ResumeTime()
    {
        Time.timeScale = 1f;
        // Hide the pause menu UI when resuming

    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        LoadScene(currentIndex);
        UpdateMusicForScene(currentIndex);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int nextcurrentIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextcurrentIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextcurrentIndex);
            UpdateMusicForScene(nextcurrentIndex);
        }
        else
        {
            SceneManager.LoadScene(0);
            UpdateMusicForScene(0);
        }
    }
    public void GoTotitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }


    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void UpdateMusicForScene(int sceneIndex)
    {
        if (AudioManager.Instance == null) return;



        if (sceneIndex > 0)
        {
            AudioManager.Instance.PlayMusic("GamePlay");
        }
        else
        {
            AudioManager.Instance.PlayMusic("MenuMusic");
        }
    }
}