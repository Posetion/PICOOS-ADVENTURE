using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Map1(Forest) only. Aligns terrain physics and places Pico on the ground once at startup.
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

        Scene scene = gameObject.scene;
        Map1ForestTerrainUtility.EnsureTerrainPhysicsAligned(scene);
        RemoveFsSystemRootObjects(scene);
        Physics.SyncTransforms();
        PlaceAllPlayers(scene);
    }

    static void PlaceAllPlayers(Scene scene)
    {
        Map1PicoSwimDriver[] drivers = Object.FindObjectsByType<Map1PicoSwimDriver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        for (int i = 0; i < drivers.Length; i++)
        {
            Map1PicoSwimDriver driver = drivers[i];
            if (driver != null && driver.gameObject.scene == scene)
            {
                Map1ForestTerrainUtility.PlacePlayerOnTerrain(driver.gameObject, scene);
            }
        }

        GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < taggedPlayers.Length; i++)
        {
            GameObject player = taggedPlayers[i];
            if (player != null && player.scene == scene)
            {
                Map1ForestTerrainUtility.PlacePlayerOnTerrain(player, scene);
            }
        }
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
