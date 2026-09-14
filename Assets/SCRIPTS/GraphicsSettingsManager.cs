using UnityEngine;
using TMPro;

public class GraphicsSettingsManager : MonoBehaviour
{
    [Header("Graphics Dropdown")]
    [SerializeField] private TMP_Dropdown graphicsDropdown;

    private const string GraphicsQualityKey = "GraphicsQuality";

    private void Start()
    {
        int savedQuality = PlayerPrefs.GetInt(GraphicsQualityKey, 1);

        savedQuality = Mathf.Clamp(savedQuality, 0, 2);

        ApplyGraphicsQuality(savedQuality);

        if (graphicsDropdown != null)
        {
            graphicsDropdown.value = savedQuality;
            graphicsDropdown.RefreshShownValue();

            graphicsDropdown.onValueChanged.AddListener(ApplyGraphicsQuality);
        }
    }

    public void ApplyGraphicsQuality(int qualityLevel)
    {
        qualityLevel = Mathf.Clamp(qualityLevel, 0, 2);

        // Tell Unity which quality level is active
        QualitySettings.SetQualityLevel(qualityLevel);

        switch (qualityLevel)
        {
            case 0:
                ApplyLowQuality();
                break;

            case 1:
                ApplyNormalQuality();
                break;

            case 2:
                ApplyHighQuality();
                break;
        }

        // Save setting
        PlayerPrefs.SetInt(GraphicsQualityKey, qualityLevel);
        PlayerPrefs.Save();

        Debug.Log(
            "Graphics Quality changed to: " +
            QualitySettings.names[qualityLevel]
        );
    }

    private void ApplyLowQuality()
    {
        Debug.Log("Applying LOW graphics");

        // FPS
        Application.targetFrameRate = 60;

        // Shadows
        QualitySettings.shadowDistance = 25f;
        QualitySettings.shadowResolution = ShadowResolution.Low;

        // Texture quality
        QualitySettings.globalTextureMipmapLimit = 1;

        // Anisotropic filtering
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;

        // LOD
        QualitySettings.lodBias = 0.5f;

        // Soft particles
        QualitySettings.softParticles = false;

        // Realtime reflection probes
        QualitySettings.realtimeReflectionProbes = false;
    }

   private void ApplyNormalQuality()
    {
        Debug.Log("Applying NORMAL graphics");

        // FPS
        Application.targetFrameRate = 60;

        // Shadows
        QualitySettings.shadowDistance = 50f;
        QualitySettings.shadowResolution = ShadowResolution.Medium;

        // Texture quality
        QualitySettings.globalTextureMipmapLimit = 0;

        // Anisotropic filtering (Fixed: changed PerTexture to Enable)
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;

        // LOD
        QualitySettings.lodBias = 1f;

        // Soft particles
        QualitySettings.softParticles = true;

        // Realtime reflection probes
        QualitySettings.realtimeReflectionProbes = true;
    }

    private void ApplyHighQuality()
    {
        Debug.Log("Applying HIGH graphics");

        // FPS
        Application.targetFrameRate = 60;

        // Shadows
        QualitySettings.shadowDistance = 100f;
        QualitySettings.shadowResolution = ShadowResolution.High;

        // Texture quality
        QualitySettings.globalTextureMipmapLimit = 0;

        // Anisotropic filtering
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

        // LOD
        QualitySettings.lodBias = 2f;

        // Soft particles
        QualitySettings.softParticles = true;

        // Realtime reflection probes
        QualitySettings.realtimeReflectionProbes = true;
    }

    private void OnDestroy()
    {
        if (graphicsDropdown != null)
        {
            graphicsDropdown.onValueChanged.RemoveListener(ApplyGraphicsQuality);
        }
    }
}