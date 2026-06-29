using UnityEngine;

public class LevelMusic : MonoBehaviour
{
    [SerializeField] private string musicName;

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(musicName);
        }
    }
}