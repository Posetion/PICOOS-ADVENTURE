#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only safety net: warns if Map1-only components are present outside Map1(Forest).
/// </summary>
[InitializeOnLoad]
static class Map1OnlyComponentValidator
{
    static Map1OnlyComponentValidator()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        ValidateScene(scene);
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
        {
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            ValidateScene(SceneManager.GetSceneAt(i));
        }
    }

    static void ValidateScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || Map1SceneGuard.IsMap1ForestScene(scene))
        {
            return;
        }

        foreach (Map1PicoSwimDriver driver in Object.FindObjectsByType<Map1PicoSwimDriver>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (driver.gameObject.scene != scene)
            {
                continue;
            }

            Debug.LogWarning(
                $"Map1-only component Map1PicoSwimDriver found in '{scene.path}'. "
                + "Remove it from this scene so Map1 swim logic cannot affect other maps.",
                driver);
        }

        foreach (Map1SceneStartup startup in Object.FindObjectsByType<Map1SceneStartup>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (startup.gameObject.scene != scene)
            {
                continue;
            }

            Debug.LogWarning(
                $"Map1-only component Map1SceneStartup found in '{scene.path}'. "
                + "Remove it from this scene so Map1 startup cleanup cannot affect other maps.",
                startup);
        }
    }
}
#endif
