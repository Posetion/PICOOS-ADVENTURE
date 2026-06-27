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


    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
        //UpdateMusicForScene(sceneName);
    }

    public void PauseTime()
    {
        Time.timeScale = 0f;
    }

    public void ResumeTime()
    {
        Time.timeScale = 1f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentIndex);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int nextcurrentIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextcurrentIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextcurrentIndex);
        }
        else
        {
            SceneManager.LoadScene(0);
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
        if (AudioManager.Instance == null)
        {
            return;
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
}