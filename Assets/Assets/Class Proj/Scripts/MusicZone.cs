using UnityEngine;

public class MusicZone : MonoBehaviour
{
    [Tooltip("Drag the kawaii background music clip here")]
    public AudioClip mainMusic;

    [Tooltip("Drag the halloween mountain scary music clip here")]
    public AudioClip scaryMusic;

    [Header("Fade Settings")]
    public float fadeDuration = 1.5f;

    [Tooltip("Skip the first N seconds of the scary music clip")]
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
