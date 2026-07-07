using UnityEngine;

public class GameQuit : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void QuitGame()
    {
        // If we are running in a standalone build of the game
#if UNITY_STANDALONE
        // Quit the application
        Application.Quit();
#endif
        // If we are running in the editor
#if UNITY_EDITOR
        // Stop playing the scene
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
