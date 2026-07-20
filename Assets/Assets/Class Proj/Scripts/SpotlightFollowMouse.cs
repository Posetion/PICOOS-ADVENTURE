using UnityEngine;

public class SpotlightFollowMouse : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The transform representing the direction the player is facing.")]
    [SerializeField] private Transform playerTransform;

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool useLayerMaskOnly = false;

    [Header("Movement & Constraints")]
    [SerializeField] private float rotationSpeed = 15f;
    [Tooltip("The maximum angle (in degrees) the light can deviate from the player's forward direction.")]
    [Range(0f, 180f)][SerializeField] private float maxFollowAngle = 45f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("SpotlightFollowMouse: No Main Camera found in the scene!", this);
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("SpotlightFollowMouse: Player Transform is missing! Deadzone logic will use this object's parent instead.", this);
            // Fallback to parent if no player is assigned
            playerTransform = transform.parent != null ? transform.parent : transform;
        }
    }

    void Update()
    {
        if (mainCamera == null) return;
        RotateLightToMouse();
    }

    void RotateLightToMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint = Vector3.zero;
        bool hasTarget = false;

        // Method 1: Try to hit the actual environment/ground colliders
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            targetPoint = hit.point;
            hasTarget = true;
        }
        // Method 2: Fallback to a flat invisible plane if we miss the ground layer
        else if (!useLayerMaskOnly)
        {
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                targetPoint = ray.GetPoint(enter);
                hasTarget = true;
            }
        }

        if (hasTarget)
        {
            // 1. Calculate the raw desired direction towards the mouse point
            Vector3 targetDirection = targetPoint - transform.position;

            if (targetDirection != Vector3.zero)
            {
                // 2. Get the reference forward vector from your player
                Vector3 playerForward = playerTransform.forward;

                // 3. Calculate the angle between player forward and the mouse direction
                float angle = Vector3.Angle(playerForward, targetDirection);

                // 4. If the mouse is outside the cone deadzone, clamp the direction to the boundary
                if (angle > maxFollowAngle)
                {
                    // Find the axis to rotate around to bring it back into the cone
                    Vector3 cross = Vector3.Cross(playerForward, targetDirection).normalized;

                    // Rotate the player's forward vector by the max allowed angle towards the target direction
                    targetDirection = Quaternion.AngleAxis(maxFollowAngle, cross) * playerForward;
                }

                // 5. Apply the smooth rotation
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    // Optional: Visualizes the cone deadzone in the Unity Editor scene view
    private void OnDrawGizmosSelected()
    {
        Transform refTransform = playerTransform != null ? playerTransform : transform;
        Gizmos.color = Color.yellow;
        Vector3 forward = refTransform.forward;

        // Draw left and right boundaries of the cone
        Vector3 leftBoundary = Quaternion.AngleAxis(-maxFollowAngle, refTransform.up) * forward;
        Vector3 rightBoundary = Quaternion.AngleAxis(maxFollowAngle, refTransform.up) * forward;

        Gizmos.DrawRay(transform.position, leftBoundary * 5f);
        Gizmos.DrawRay(transform.position, rightBoundary * 5f);
    }
}
