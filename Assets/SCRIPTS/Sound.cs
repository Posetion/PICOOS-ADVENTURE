using UnityEngine;

[System.Serializable]
public class Sound //making my own class to store the sound data, this will be used in the AudioManager to create a list of sounds
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.8f, 1.2f)] public float pitchMin = 0.9f;
    [Range(0.8f, 1.2f)] public float pitchMax = 1.1f;


}
