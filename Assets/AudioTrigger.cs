using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    private AudioSource jumpscareAudio;
    private bool hasPlayed = false;
    private bool playedOnlyOnce = false;
    void Start()
    {
        jumpscareAudio = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            if (hasPlayed && playedOnlyOnce) return;

            if (jumpscareAudio != null && !jumpscareAudio.isPlaying) 
            {
                jumpscareAudio.Play();
                hasPlayed = true;
            }
        }
    }
}
