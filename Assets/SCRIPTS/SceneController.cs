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
    /*
        private void UpdateMusicForScene(int sceneIndex)
        {
            // This is no longer needed if you use the coroutine, but keeping it clean just in case:
            if (AudioManager.Instance == null) return;

            if (sceneIndex == 0)
            {
                AudioManager.Instance.PlayMusic("MenuMusic");
            }
            else
            {
                AudioManager.Instance.PlayMusic("GamePlay");
            }
        }
        */
}