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
        Time.timeScale = 0f;

        // If a UiManager exists in this scene, trigger its pause setup
        if (UiManager.instance != null)
        {
            if (UiManager.instance.pauseMenu != null)
                UiManager.instance.pauseMenu.SetActive(true);

            // Optional: Hide the HUD when paused if you'd like
            // UiManager.instance.ToggleHUD(false); 
        }
    }

    public void ResumeTime()
    {
        Time.timeScale = 1f;

        // If a UiManager exists in this scene, close the pause menu
        if (UiManager.instance != null)
        {
            if (UiManager.instance.pauseMenu != null)
                UiManager.instance.pauseMenu.SetActive(false);

            // Optional: Show the HUD back
            // UiManager.instance.ToggleHUD(true);
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
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"[SceneController] Next Level button clicked. Loading scene {nextIndex}.");
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.Log("[SceneController] No more levels found. Returning to Scene 0.");
            SceneManager.LoadScene(0);
        }
    }
    public void GoTotitle()
    {
        Time.timeScale = 1f;
        Debug.Log("[SceneController] GoToTitle clicked. Loading scene 0.");
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

}