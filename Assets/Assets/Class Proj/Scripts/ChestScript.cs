using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestController : MonoBehaviour
{
    public static bool hasKey = false;

    [SerializeField] private Animator animator;
    private bool opened = false;

    [Header("Drag your 3 Spawn Collectible objects here (auto-found if empty)")]
    [SerializeField] private GameObject[] collectibles;

    [Header("Jump from chest to placed position")]
    [SerializeField] private float arcHeight = 2.5f;
    [SerializeField] private float flightDuration = 0.8f;
    [SerializeField] private float staggerDelay = 0.12f;

    private struct SpawnData
    {
        public GameObject obj;
        public Vector3 position;
        public Quaternion rotation;
    }

    private SpawnData[] spawnData;

    private void Awake()
    {
        hasKey = false;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (collectibles == null || collectibles.Length == 0)
            collectibles = FindSpawnCollectibles();

        CacheSpawnPositions();
    }

    public void OpenChest()
    {
        if (opened) return;

        if (!hasKey)
        {
            Debug.Log("You need the key!");
            return;
        }

        opened = true;
        animator.SetTrigger("Open");
        StartCoroutine(LaunchCollectiblesToPlacedPositions());

        Debug.Log("Chest Opened!");
    }

    private GameObject[] FindSpawnCollectibles()
    {
        var found = new List<GameObject>();
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.scene != gameObject.scene) continue;
            if (!obj.name.StartsWith("Spawn Collectible")) continue;
            found.Add(obj);
        }

        found.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return found.ToArray();
    }

    private void CacheSpawnPositions()
    {
        if (collectibles == null || collectibles.Length == 0)
        {
            spawnData = System.Array.Empty<SpawnData>();
            return;
        }

        spawnData = new SpawnData[collectibles.Length];
        for (int i = 0; i < collectibles.Length; i++)
        {
            GameObject obj = collectibles[i];
            spawnData[i] = new SpawnData
            {
                obj = obj,
                position = obj.transform.position,
                rotation = obj.transform.rotation
            };
        }
    }

    private IEnumerator LaunchCollectiblesToPlacedPositions()
    {
        yield return null;

        if (spawnData == null || spawnData.Length == 0)
        {
            Debug.LogWarning("ChestController: No spawn collectibles assigned!");
            yield break;
        }

        Vector3 chestPopPos = transform.position + transform.up * 1.5f;

        for (int i = 0; i < spawnData.Length; i++)
        {
            SpawnData data = spawnData[i];
            if (data.obj == null) continue;

            Collider pickupCol = data.obj.GetComponent<Collider>();
            if (pickupCol != null)
                pickupCol.enabled = false;

            Rigidbody rb = data.obj.GetComponent<Rigidbody>();
            if (rb != null)
                Destroy(rb);

            // Small spread at chest so they don't stack on launch
            float angle = (360f / spawnData.Length) * i;
            Vector3 launchOffset = Quaternion.AngleAxis(angle, transform.up) * transform.forward * 0.2f;
            Vector3 startPos = chestPopPos + launchOffset;

            data.obj.transform.SetPositionAndRotation(startPos, data.rotation);
            data.obj.SetActive(true);

            StartCoroutine(FlyToPosition(data.obj, startPos, data.position, data.rotation, pickupCol));

            if (staggerDelay > 0f)
                yield return new WaitForSeconds(staggerDelay);
        }
    }

    private IEnumerator FlyToPosition(GameObject obj, Vector3 start, Vector3 end, Quaternion endRotation, Collider pickupCol)
    {
        float elapsed = 0f;

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);

            // Horizontal move + parabolic arc upward
            Vector3 flat = Vector3.Lerp(start, end, t);
            float height = arcHeight * 4f * t * (1f - t);
            obj.transform.position = flat + Vector3.up * height;

            yield return null;
        }

        obj.transform.SetPositionAndRotation(end, endRotation);

        if (pickupCol != null)
            pickupCol.enabled = true;
    }
}
