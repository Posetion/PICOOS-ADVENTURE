using UnityEngine;

public class SpotlightFollowMouse : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool useLayerMaskOnly = false;

    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 15f;

    private Camera mainCamera;

    void Start()
    {
        // Cache the main camera for performance
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("SpotlightFollowMouse: No Main Camera found in the scene!", this);
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
            // Creates an invisible mathematical plane at Y = 0 facing straight up
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                targetPoint = ray.GetPoint(enter);
                hasTarget = true;
            }
        }

        // If we found a valid point, rotate towards it
        if (hasTarget)
        {
            Vector3 targetDirection = targetPoint - transform.position;

            // Optional: If you don't want the light to awkwardly tilt up/down too much, 
            // you can uncomment the line below to lock the rotation to a flat plane:
            // targetDirection.y = 0; 

            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

                // Smoothly interpolate rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
}