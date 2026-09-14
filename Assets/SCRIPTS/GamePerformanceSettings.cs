using UnityEngine;

/// <summary>
/// Global performance settings for the game.
/// This script does NOT need to be attached to a GameObject.
/// Unity automatically runs it before the first scene loads.
/// </summary>
public static class GamePerformanceSettings
{
    // ==============================
    // FPS LIMITS
    // ==============================

    public const int MobileTargetFps = 60;
    public const int DesktopTargetFps = 60;


    // ==============================
    // AUTOMATIC STARTUP
    // ==============================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnStartup()
    {
        ApplyFrameRate();
        ApplyPlatformQuality();
    }


    // ==============================
    // FRAME RATE
    // ==============================

    public static void ApplyFrameRate()
    {
        // Disable VSync so our FPS limit controls the frame rate.
        QualitySettings.vSyncCount = 0;

        // Set FPS depending on the platform.
        if (IsMobilePlatform())
        {
            Application.targetFrameRate = MobileTargetFps;
        }
        else
        {
            Application.targetFrameRate = DesktopTargetFps;
        }
    }


    // ==============================
    // QUALITY SETTINGS
    // ==============================

    public static void ApplyPlatformQuality()
    {
        // Only change quality automatically on mobile.
        if (!IsMobilePlatform())
            return;

        // Quality Level 0 should be your lowest/mobile quality.
        const int mobileQualityIndex = 0;

        if (QualitySettings.GetQualityLevel() != mobileQualityIndex)
        {
            QualitySettings.SetQualityLevel(
                mobileQualityIndex,
                true
            );
        }
    }


    // ==============================
    // MOBILE DETECTION
    // ==============================

    public static bool IsMobilePlatform()
    {
#if UNITY_ANDROID || UNITY_IOS

        return true;

#else

        return Application.isMobilePlatform;

#endif
    }


    // ==============================
    // REDUCED GRAPHICS CHECK
    // ==============================

    public static bool ShouldUseReducedSceneSettings()
    {
        return IsMobilePlatform()
               || QualitySettings.GetQualityLevel() == 0;
    }
}