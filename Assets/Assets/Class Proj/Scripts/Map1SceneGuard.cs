using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared guard for Map1(Forest)-only runtime behaviour.
/// </summary>
public static class Map1SceneGuard
{
    public const string SceneName = "Map1(Forest)";
    public const string SceneAssetPath = "Assets/Scenes/Map1(Forest).unity";

    public static bool IsActiveMap1Forest()
    {
        return IsMap1ForestScene(SceneManager.GetActiveScene());
    }

    public static bool IsMap1ForestScene(Scene scene)
    {
        if (!scene.IsValid())
        {
            return false;
        }

        if (scene.name == SceneName)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(scene.path))
        {
            return scene.path.Replace('\\', '/')
                .EndsWith(SceneAssetPath, System.StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
