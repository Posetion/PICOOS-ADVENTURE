using UnityEngine;

public class SpotlightFollowMouse : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rotationSpeed = 10f;

    private Camera mainCamera;

    void Start()
    {
        // Cache the main camera for better performance
        mainCamera = Camera.main;
    }

    void Update()
    {
        RotateLightToMouse();
    }

    void RotateLightToMouse()
    {
        // Convert screen mouse position into a 3D Ray
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Cast the ray to find where it intersects with the environment
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            // Determine the direction from the spotlight to the hit point
            Vector3 targetDirection = hit.point - transform.position;

            // Calculate the rotation needed to look in that direction
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            // Smoothly rotate the spotlight towards the target position
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
