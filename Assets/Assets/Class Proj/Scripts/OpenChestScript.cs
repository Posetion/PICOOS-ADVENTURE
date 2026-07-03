using UnityEngine;

public class ChestInteract : MonoBehaviour
{
    [SerializeField] private ChestController chest;

    [Header("Optional — auto-finds UiManager if empty")]
    [SerializeField] private UiManager uiManager;

    private void Awake()
    {
        if (uiManager == null)
            uiManager = FindAnyObjectByType<UiManager>();

        if (!CompareTag("Chest"))
            Debug.LogWarning($"{name} should use the Chest tag for the key popup system.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (uiManager != null)
            uiManager.HideKeyPopup();

        OnChestReached();

        if (chest != null)
            chest.OpenChest();
    }

    public void OnChestReached()
    {
        Debug.Log("Player reached the chest!");
    }
}
