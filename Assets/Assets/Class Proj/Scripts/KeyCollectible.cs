using UnityEngine;

public class KeyCollectible : MonoBehaviour
{
    [Header("Optional — auto-finds UiManager if empty")]
    [SerializeField] private UiManager uiManager;

    [SerializeField] private GameObject collectVFX;

    private void Awake()
    {
        if (uiManager == null)
            uiManager = FindAnyObjectByType<UiManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        ChestController.hasKey = true;

        if (uiManager != null)
            uiManager.ShowKeyPopup();

        if (collectVFX != null)
            Instantiate(collectVFX, transform.position, transform.rotation);

        Debug.Log("Key Collected!");

        Destroy(gameObject);
    }
}
