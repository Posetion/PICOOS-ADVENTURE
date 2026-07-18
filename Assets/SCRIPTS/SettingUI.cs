using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle musicMuteToggle;
    [SerializeField] private Toggle sfxMuteToggle;

    [Header("Graphics Settings")]
    [SerializeField] private TMP_Dropdown qualityDropdown;


    private void Start()
    {
        LoadSettings();
        SetupListeners();
    }

    private void LoadSettings()
    {
        // Load audio settings
        if (AudioManager.Instance != null)
        {
            musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
            sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();
            musicMuteToggle.isOn = AudioManager.Instance.IsMusicMuted();
            sfxMuteToggle.isOn = AudioManager.Instance.IsSFXMuted();
        }

        // Load graphics settings
        int savedQuality = PlayerPrefs.GetInt("GraphicsQuality", 2); // Default: Medium
        qualityDropdown.value = savedQuality;
        QualitySettings.SetQualityLevel(savedQuality);
    }

    private void SetupListeners()
    {
        // Audio sliders
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        // Mute toggles
        musicMuteToggle.onValueChanged.AddListener(OnMusicMuteToggled);
        sfxMuteToggle.onValueChanged.AddListener(OnSFXMuteToggled);

        // Quality dropdown
        qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    private void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSfxVolume(value);
        // Play test sound
        AudioManager.Instance?.PlaySFX("UIClick");
    }

    private void OnMusicMuteToggled(bool muted)
    {
        AudioManager.Instance?.ToggleMusicVolume();
    }

    private void OnSFXMuteToggled(bool muted)
    {
        AudioManager.Instance?.ToggleSfxVolume();
    }

    private void OnQualityChanged(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("GraphicsQuality", qualityIndex);
        PlayerPrefs.Save();
    }

    public void OnResetGame()
    {
        // 1. Delete ONLY the level progression data so we don't break audio/graphics settings
        // Adjust the "3" if you have more levels (e.g., if you have 5 levels, use i <= 5)
        for (int i = 1; i <= 3; i++)
        {
            string levelKey = $"Level{i}Complete";
            if (PlayerPrefs.HasKey(levelKey))
            {
                PlayerPrefs.DeleteKey(levelKey);
            }
        }

        // Save changes to disk immediately
        PlayerPrefs.Save();
        Debug.Log("[SettingUI] Level completion data has been safely cleared!");

        // 2. Force the game to reload the Title/Main Menu scene so the level selectors visually update
        if (SceneController.instance != null)
        {
            SceneController.instance.GoTotitle();
        }
        else
        {
            // Fallback just in case SceneController isn't in this specific scene yet
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }


}
