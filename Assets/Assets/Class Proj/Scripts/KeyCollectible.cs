using UnityEngine;

public class KeyCollectible : MonoBehaviour
{
    [Header("Optional — auto-finds UiManager if empty")]
    [SerializeField] private UiManager uiManager;

    [SerializeField] private GameObject collectVFX;

    [Header("Spin Animation")]
    [SerializeField] private float rotationSpeed = 90f; // degrees per second
    [SerializeField] private float tiltAngle = 30f;     // forward tilt, collectible style

    private void Awake()
    {
        if (uiManager == null)
            uiManager = FindAnyObjectByType<UiManager>();

        // Tilt the key forward once; the spin below keeps this tilt.
        transform.rotation = Quaternion.Euler(tiltAngle, transform.eulerAngles.y, 0f);
    }

    private void Update()
    {
        // Spin around the world Y axis so the tilt stays fixed while it rotates.
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ChestController.hasKey = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("keyCollected");
            }

            if (uiManager != null)
                uiManager.ShowKeyPopup();

            if (collectVFX != null)
            {
                Instantiate(collectVFX, transform.position, transform.rotation);
            }

            Debug.Log("Key Collected!");

            Destroy(gameObject);
        }
    }
}