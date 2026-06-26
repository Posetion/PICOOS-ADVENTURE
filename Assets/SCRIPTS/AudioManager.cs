using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Starting Music")]
    [Tooltip("Drag a music clip here to auto-play it when this scene loads")]
    [SerializeField] private AudioClip startMusicClip;

    [Header("Sound Effects")]
    [SerializeField]
    private List<Sound> soundEffects;

    [Header("Music Audios")]
    [SerializeField]
    private List<Sound> musicAudios;

    private float musicVolume = 0.5f;
    private float sfxVolume = 1.0f;
    private bool musicMuted = false;
    private bool sfxMuted = false;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);

        LoadAudioSetting();


    }


    void Start()
    {
        ApplyAudioSetting();

        if (startMusicClip != null)
        {
            musicAudioSource.clip = startMusicClip;
            musicAudioSource.loop = true;
            musicAudioSource.volume = musicMuted ? 0f : musicVolume;
            musicAudioSource.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }


    public void LoadAudioSetting()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.2f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        musicMuted = PlayerPrefs.GetFloat("MusicMuted", 0) == 1;
        sfxMuted = PlayerPrefs.GetFloat("SFXMuted", 0) == 1;
    }

    private void ApplyAudioSetting()
    {
        musicAudioSource.volume = musicMuted ? 0f : musicVolume;
        sfxAudioSource.volume = sfxMuted ? 0f : sfxVolume;

    }

    public void PlaySFX(string sfxName)
    {
        // FIX: Changed from System.Array.Find to list.Find
        Sound sfx = soundEffects.Find(s => s.name == sfxName);

        if (sfx != null && sfx.clip != null)
        {
            // Note: Ensure your Sound class has pitchMin and pitchMax fields!
            float pitch = Random.Range(sfx.pitchMin, sfx.pitchMax);
            sfxAudioSource.pitch = pitch;

            sfxAudioSource.PlayOneShot(sfx.clip, sfx.volume);
        }
        else
        {
            Debug.LogWarning($"SFX '{sfxName}' not found!");
        }
    }



    public void PlayMusic(string musicName)
    {
        Sound musicAudio = musicAudios.Find(s => s.name == musicName);
        if (musicAudio != null && musicAudio.clip != null)
        {
            musicAudioSource.clip = musicAudio.clip;

            musicAudioSource.Play();
        }
        else
        {
            Debug.LogWarning($"Music track '{musicName}' not found!");
        }
    }


    public void SetMusicVolume(float musicVolume)
    {
        musicVolume = Mathf.Clamp01(musicVolume);
        musicAudioSource.volume = musicVolume;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float sfxVolume)
    {
        sfxVolume = Mathf.Clamp01(sfxVolume);
        sfxAudioSource.volume = sfxMuted ? 0f : sfxVolume;
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }



    public void ToggleMusicVolume()
    {
        musicMuted = !musicMuted;
        musicVolume = musicMuted ? 0f : musicVolume;
        PlayerPrefs.SetFloat("MusicMuted", musicVolume);
        PlayerPrefs.Save();

    }
    public void ToggleSfxVolume()
    {
        sfxMuted = !sfxMuted;
        sfxVolume = sfxMuted ? 0f : sfxVolume;
        PlayerPrefs.SetFloat("SFXMuted", sfxVolume);
        PlayerPrefs.Save();

    }

    public void StopMusic()
    {
        musicAudioSource.Stop();
    }

    public void PauseMusic()
    {
        musicAudioSource.Pause();
    }

    public void ResumeMusic()
    {
        musicAudioSource.UnPause();
    }



    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
    public bool IsMusicMuted() => musicMuted;
    public bool IsSFXMuted() => sfxMuted;

    // Used by MusicZone to crossfade between tracks
    private Coroutine activeFadeCoroutine;

    public void PlayMusicWithFade(AudioClip clip, float fadeDuration = 1.5f, float startOffset = 0f)
    {
        if (clip == null) return;

        if (activeFadeCoroutine != null)
            StopCoroutine(activeFadeCoroutine);

        activeFadeCoroutine = StartCoroutine(CrossFadeMusic(clip, fadeDuration, startOffset));
    }

    private IEnumerator CrossFadeMusic(AudioClip nextClip, float fadeDuration, float startOffset)
    {
        float targetVol = musicMuted ? 0f : musicVolume;
        float elapsed = 0f;
        float fromVol = musicAudioSource.volume;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicAudioSource.volume = Mathf.Lerp(fromVol, 0f, elapsed / fadeDuration);
            yield return null;
        }
        musicAudioSource.volume = 0f;

        musicAudioSource.clip = nextClip;
        musicAudioSource.time = Mathf.Clamp(startOffset, 0f, nextClip.length - 0.1f);
        musicAudioSource.Play();

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicAudioSource.volume = Mathf.Lerp(0f, targetVol, elapsed / fadeDuration);
            yield return null;
        }
        musicAudioSource.volume = targetVol;
        activeFadeCoroutine = null;
    }

}
