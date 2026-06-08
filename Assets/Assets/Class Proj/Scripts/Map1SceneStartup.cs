using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Map1(Forest) only. Removes editor-only FS setup root objects that add Awake overhead on play.
/// Self-disables outside Map1(Forest) so it cannot affect other scenes even if mis-wired.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class Map1SceneStartup : MonoBehaviour
{
    static bool sRanForActiveScene;

    void Awake()
    {
        if (!Map1SceneGuard.IsMap1ForestScene(gameObject.scene))
        {
            enabled = false;
            return;
        }

        if (sRanForActiveScene)
        {
            return;
        }

        sRanForActiveScene = true;
        SceneManager.sceneUnloaded += _ => sRanForActiveScene = false;
        RemoveFsSystemRootObjects(gameObject.scene);
    }

    static void RemoveFsSystemRootObjects(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root != null && root.name == "FS System")
            {
                Destroy(root);
            }
        }
    }
}
