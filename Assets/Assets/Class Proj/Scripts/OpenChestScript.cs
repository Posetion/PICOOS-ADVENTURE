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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (uiManager != null)
            uiManager.HideKeyPopup();

        chest?.OpenChest();
    }
}