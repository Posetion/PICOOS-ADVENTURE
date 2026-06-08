using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared guard for Map1(Forest)-only runtime behaviour.
/// </summary>
static class Map1SceneGuard
{
    public const string SceneAssetPath = "Assets/Scenes/Map1(Forest).unity";

    public static bool IsActiveMap1Forest()
    {
        Scene scene = SceneManager.GetActiveScene();
        return IsMap1ForestScene(scene);
    }

    public static bool IsMap1ForestScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(scene.path))
        {
            return scene.path.Replace('\\', '/')
                .EndsWith(SceneAssetPath, System.StringComparison.OrdinalIgnoreCase);
        }

        return scene.name == "Map1(Forest)";
    }
}
