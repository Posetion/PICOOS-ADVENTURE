using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared Map1(Forest) terrain alignment helpers used at runtime and in the editor.
/// TerrainCollider ignores parent scale; the Terrain renderer does not.
/// Terrain must live at the scene root with unit scale so visuals match physics.
/// </summary>
public static class Map1ForestTerrainUtility
{
    public static readonly Vector3 Map1TerrainWorldPosition = new(-50.02005f, 0f, -50.02005f);

    public static void EnsureTerrainPhysicsAligned(Scene scene)
    {
        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < terrains.Length; i++)
        {
            AlignTerrainTransform(terrains[i], scene);
        }
    }

    static void AlignTerrainTransform(Terrain terrain, Scene scene)
    {
        if (terrain == null || terrain.gameObject.scene != scene || terrain.terrainData == null)
        {
            return;
        }

        Transform terrainTransform = terrain.transform;
        Vector3 worldPosition = terrainTransform.position;
        Quaternion worldRotation = terrainTransform.rotation;

        if (terrainTransform.parent != null || !HasUnitLossyScale(terrainTransform))
        {
            terrainTransform.SetParent(null, true);
            terrainTransform.localScale = Vector3.one;
            terrainTransform.SetPositionAndRotation(worldPosition, worldRotation);
        }

        TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
        if (terrainCollider != null)
        {
            terrainCollider.terrainData = terrain.terrainData;
            terrainCollider.enabled = true;
        }
    }

    public static bool TryGetTerrainSurfaceY(Vector3 worldPosition, Scene scene, out float surfaceY)
    {
        surfaceY = float.MinValue;
        bool found = false;

        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.gameObject.scene != scene || terrain.terrainData == null)
            {
                continue;
            }

            if (!HasUnitLossyScale(terrain.transform))
            {
                continue;
            }

            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            if (worldPosition.x < terrainPosition.x - 0.25f
                || worldPosition.x > terrainPosition.x + terrainSize.x + 0.25f
                || worldPosition.z < terrainPosition.z - 0.25f
                || worldPosition.z > terrainPosition.z + terrainSize.z + 0.25f)
            {
                continue;
            }

            float sample = terrain.SampleHeight(worldPosition) + terrainPosition.y;
            if (!found || sample > surfaceY)
            {
                surfaceY = sample;
                found = true;
            }
        }

        return found;
    }

    public static void PlacePlayerOnTerrain(GameObject player, Scene scene)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
        {
            return;
        }

        Vector3 position = player.transform.position;
        if (!TryGetTerrainSurfaceY(position, scene, out float surfaceY))
        {
            return;
        }

        float capsuleBottomOffset = GetCapsuleBottomOffset(player.transform, controller);
        position.y = surfaceY - capsuleBottomOffset + controller.skinWidth;

        controller.enabled = false;
        player.transform.position = position;
        Physics.SyncTransforms();
        controller.enabled = true;
    }

    public static float GetCapsuleBottomOffset(Transform transform, CharacterController controller)
    {
        return (controller.center.y - controller.height * 0.5f) * transform.lossyScale.y;
    }

    static bool HasUnitLossyScale(Transform transform)
    {
        Vector3 lossyScale = transform.lossyScale;
        return Mathf.Approximately(lossyScale.x, 1f)
            && Mathf.Approximately(lossyScale.y, 1f)
            && Mathf.Approximately(lossyScale.z, 1f);
    }
}
