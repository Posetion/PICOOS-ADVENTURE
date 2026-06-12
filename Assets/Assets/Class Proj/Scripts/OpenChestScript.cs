using UnityEngine;

public class ChestInteract : MonoBehaviour
{
    [SerializeField] private ChestController chest;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            chest.OpenChest();
        }
    }
}