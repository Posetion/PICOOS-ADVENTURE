using UnityEngine;

/// <summary>
/// Caps frame rate and picks the Mobile quality tier on phones/tablets.
/// PC/Editor is capped at 100 FPS so gameplay pacing stays consistent while testing.
/// </summary>
public static class GamePerformanceSettings
{
    public const int MobileTargetFps = 60;
    public const int DesktopTargetFps = 100;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyOnStartup()
    {
        ApplyFrameRate();
        ApplyPlatformQuality();
    }

    public static void ApplyFrameRate()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = IsMobilePlatform() ? MobileTargetFps : DesktopTargetFps;
    }

    public static void ApplyPlatformQuality()
    {
        if (!IsMobilePlatform())
            return;

        const int mobileQualityIndex = 0;
        if (QualitySettings.GetQualityLevel() != mobileQualityIndex)
            QualitySettings.SetQualityLevel(mobileQualityIndex, applyExpensiveChanges: true);
    }

    public static bool IsMobilePlatform()
    {
#if UNITY_ANDROID || UNITY_IOS
        return true;
#else
        return Application.isMobilePlatform;
#endif
    }

    public static bool ShouldUseReducedSceneSettings()
    {
        return IsMobilePlatform() || QualitySettings.GetQualityLevel() == 0;
    }
}
