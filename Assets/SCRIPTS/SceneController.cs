using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneController : MonoBehaviour
{
    public static SceneController instance;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }



    private IEnumerator WaitAndPlayMusic(int sceneIndex)
    {
        yield return new WaitForEndOfFrame();

        if (AudioManager.Instance != null)
        {
            if (sceneIndex == 0)
            {
                /*Debug.Log("[SceneController] Target is Scene 0. Requesting 'MenuMusic'.");
                AudioManager.Instance.PlayMusic("MenuMusic");*/
            }
            else
            {
                // Dynamically look for "Level1", "Level2", etc.
                string trackName = "Level" + sceneIndex;
                Debug.Log($"[SceneController] Target is Scene {sceneIndex}. Requesting '{trackName}'.");
                AudioManager.Instance.PlayMusic(trackName);
            }
        }
        else
        {
            Debug.LogError("[SceneController] ERROR: AudioManager.Instance is NULL!");
        }
    }


    public void LoadScene(int sceneIndex)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }

    public void PauseTime()
    {
        if (UiManager.instance != null)
        {
            UiManager.instance.PauseButton();
        }
        else
        {
            Time.timeScale = 0f;
        }
    }

    public void ResumeTime()
    {
        if (UiManager.instance != null)
        {
            UiManager.instance.ResumeButton();
        }
        else
        {
            Time.timeScale = 1f;
        }
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
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        // If we are currently on Map 3, do NOT load index 4 (Tutorial). Go to Main Menu instead.
        if (currentIndex == 3)
        {
            Debug.Log("[SceneController] Map 3 completed. Returning to Main Menu instead of Tutorial.");
            SceneManager.LoadScene(0);
            return;
        }

        // Standard next level progression (excluding index 4)
        if (nextIndex < SceneManager.sceneCountInBuildSettings && nextIndex != 4)
        {
            Debug.Log($"[SceneController] Next Level button clicked. Loading scene {nextIndex}.");
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.Log("[SceneController] No more main levels found. Returning to Scene 0.");
            SceneManager.LoadScene(0);
        }
    }
    public void GoTotitle()
    {
        Time.timeScale = 1f;
        Debug.Log("[SceneController] GoToTitle clicked. Loading scene 0.");
        SceneManager.LoadScene(0);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }


    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}