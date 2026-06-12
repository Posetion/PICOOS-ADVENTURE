#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps Map1(Forest) terrain collision aligned with its visuals before play and on demand.
/// </summary>
[InitializeOnLoad]
static class Map1ForestTerrainRepair
{
    const string MenuPath = "Tools/Map1/Fix Terrain And Player Spawn";

    static Map1ForestTerrainRepair()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem(MenuPath)]
    static void RepairActiveSceneFromMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!Map1SceneGuard.IsMap1ForestScene(scene))
        {
            EditorUtility.DisplayDialog(
                "Map1 Terrain Repair",
                "Open Map1(Forest) before running this fix.",
                "OK");
            return;
        }

        RepairScene(scene, markDirty: true);
        EditorUtility.DisplayDialog(
            "Map1 Terrain Repair",
            "Terrain was moved to the scene root with unit scale, and Pico was placed on the ground.",
            "OK");
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!Map1SceneGuard.IsMap1ForestScene(scene))
        {
            return;
        }

        RepairScene(scene, markDirty: true);
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!Map1SceneGuard.IsMap1ForestScene(scene))
        {
            return;
        }

        RepairScene(scene, markDirty: true);
    }

    static void RepairScene(Scene scene, bool markDirty)
    {
        Transform ground = FindGroundTransform(scene);
        Terrain terrain = FindMap1Terrain(scene);
        if (terrain == null)
        {
            Debug.LogWarning("Map1 terrain repair: Terrain object not found.");
            return;
        }

        Transform terrainTransform = terrain.transform;

        if (ground != null)
        {
            ground.localScale = Vector3.one;
        }

        terrainTransform.SetParent(null, true);
        terrainTransform.localScale = Vector3.one;
        terrainTransform.position = Map1ForestTerrainUtility.Map1TerrainWorldPosition;
        terrainTransform.rotation = Quaternion.identity;

        TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
        if (terrainCollider != null)
        {
            terrainCollider.terrainData = terrain.terrainData;
            terrainCollider.enabled = true;
        }

        GameObject player = FindMap1Player(scene);
        if (player != null)
        {
            Map1ForestTerrainUtility.PlacePlayerOnTerrain(player, scene);
        }

        if (markDirty)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    static Transform FindGroundTransform(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform ground = FindChildByName(roots[i].transform, "Ground");
            if (ground != null)
            {
                return ground;
            }
        }

        return null;
    }

    static Terrain FindMap1Terrain(Scene scene)
    {
        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain != null && terrain.gameObject.scene == scene && terrain.gameObject.name == "Terrain")
            {
                return terrain;
            }
        }

        return terrains.Length > 0 ? terrains[0] : null;
    }

    static GameObject FindMap1Player(Scene scene)
    {
        Map1PicoSwimDriver driver = Object.FindFirstObjectByType<Map1PicoSwimDriver>(FindObjectsInactive.Include);
        if (driver != null && driver.gameObject.scene == scene)
        {
            return driver.gameObject;
        }

        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        return tagged != null && tagged.scene == scene ? tagged : null;
    }

    static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform match = FindChildByName(parent.GetChild(i), childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
#endif
