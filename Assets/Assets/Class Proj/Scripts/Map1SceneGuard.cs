using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared guard for Map1(Forest)-only runtime behaviour.
/// </summary>
public static class Map1SceneGuard
{
    public const string SceneName = "Map1(Forest)";
    public const string SceneAssetPath = "Assets/Scenes/Map1(Forest).unity";
    public const string Map2SceneName = "Map2 (Cave)";
    public const string Map2SceneAssetPath = "Assets/Scenes/Map2 (Cave).unity";
    public const string Map3SceneName = "Map3(Desert)";
    public const string Map3SceneAssetPath = "Assets/Scenes/Map3(Desert).unity";

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

    public static bool IsMap2CaveScene(Scene scene)
    {
        if (!scene.IsValid())
        {
            return false;
        }

        if (scene.name == Map2SceneName)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(scene.path))
        {
            return scene.path.Replace('\\', '/')
                .EndsWith(Map2SceneAssetPath, System.StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public static bool IsMap1OrMap2Scene(Scene scene)
    {
        return IsMap1ForestScene(scene) || IsMap2CaveScene(scene);
    }

    public static bool IsMap3DesertScene(Scene scene)
    {
        if (!scene.IsValid())
        {
            return false;
        }

        if (scene.name == Map3SceneName)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(scene.path))
        {
            return scene.path.Replace('\\', '/')
                .EndsWith(Map3SceneAssetPath, System.StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public static bool IsMap1OrMap2OrMap3Scene(Scene scene)
    {
        return IsMap1ForestScene(scene) || IsMap2CaveScene(scene) || IsMap3DesertScene(scene);
    }
}
