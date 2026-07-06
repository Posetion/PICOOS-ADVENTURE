using UnityEngine;

public class KeyCollectible : MonoBehaviour
{
    [Header("Optional — auto-finds UiManager if empty")]
    [SerializeField] private UiManager uiManager;

    [SerializeField] private GameObject collectVFX;

    [Header("Spin Animation")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float tiltAngle = 30f;

    private void Awake()
    {
        if (uiManager == null)
            uiManager = FindAnyObjectByType<UiManager>();

        transform.rotation = Quaternion.Euler(tiltAngle, transform.eulerAngles.y, 0f);
    }

    private void Update()
    {
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        ChestController.hasKey = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("keyCollected");

        if (uiManager != null)
            uiManager.ShowKeyPopup();

        if (collectVFX != null)
            Instantiate(collectVFX, transform.position, transform.rotation);

        Debug.Log("Key Collected!");

        Destroy(gameObject);
    }
}
