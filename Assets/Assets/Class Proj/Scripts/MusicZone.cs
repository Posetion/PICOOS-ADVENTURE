using UnityEngine;

public class MusicZone : MonoBehaviour
{
    
    public AudioClip mainMusic;

    
    public AudioClip scaryMusic;

    [Header("Fade Settings")]
    public float fadeDuration = 1.5f;

    
    public float scaryMusicStartOffset = 2f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        AudioManager.Instance.PlayMusicWithFade(scaryMusic, fadeDuration, scaryMusicStartOffset);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        AudioManager.Instance.PlayMusicWithFade(mainMusic, fadeDuration, 0f);
    }
}
