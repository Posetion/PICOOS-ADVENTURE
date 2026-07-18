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
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Force cursor settings immediately on startup
            EnableCursor();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Automatically ensures the cursor stays on whenever any map or scene finishes loading
        EnableCursor();
        StartCoroutine(ForceCursorDelayed());
    }
    private IEnumerator ForceCursorDelayed()
    {
        // Wait until the very end of the frame so other scripts finish their Awake/Start calls
        yield return new WaitForEndOfFrame();
        EnableCursor();
        Debug.Log("[SceneController] Cursor force-enabled at the end of the frame.");
    }
    /// <summary>
    /// Centralized function to keep the cursor active and unlocked
    /// </summary>
    private void EnableCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
        EnableCursor(); // Ensures cursor remains visible if pausing triggers changes
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
        EnableCursor();
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

        if (currentIndex >= 1)
        {
            PlayerPrefs.SetInt($"Level{currentIndex}Complete", 1);
            PlayerPrefs.Save();
            Debug.Log($"[SceneController] Saved completion data: Level{currentIndex}Complete = 1");
        }

        int nextIndex = currentIndex + 1;

        if (currentIndex == 3)
        {
            Debug.Log("[SceneController] Map 3 completed. Returning to Main Menu instead of Tutorial.");
            SceneManager.LoadScene(0);
            return;
        }

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