using UnityEngine;

/// <summary>
/// Loops the board's world Y position between minY and maxY forever.
/// </summary>
[DisallowMultipleComponent]
public class FlyingBoardAnimation : MonoBehaviour
{
    [SerializeField] private float minY = 4f;
    [SerializeField] private float maxY = 8f;
    [SerializeField] private float moveSpeed = 0.5f;

    private float startX;
    private float startZ;

    private void Start()
    {
        Vector3 pos = transform.position;
        startX = pos.x;
        startZ = pos.z;
    }

    private void Update()
    {
        // 0 at minY, 1 at maxY, then back to 0 (smooth loop)
        float t = (Mathf.Sin(Time.time * moveSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float y = Mathf.Lerp(minY, maxY, t);
        transform.position = new Vector3(startX, y, startZ);
    }
}
