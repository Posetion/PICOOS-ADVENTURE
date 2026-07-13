using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialButtonController : MonoBehaviour
{
    [Header("Scene Index Configuration")]
    [SerializeField] private int tutorialMapIndex = 4;

    /// <summary>
    /// Attach this method to your Main Menu button's OnClick event.
    /// </summary>
    public void ClickToStartTutorial()
    {
        Time.timeScale = 1f;
        Debug.Log($"[TutorialButton] Loading Tutorial Scene (Index {tutorialMapIndex}).");
        SceneManager.LoadScene(tutorialMapIndex);
    }
}