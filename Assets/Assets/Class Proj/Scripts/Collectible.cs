using Unity.VisualScripting;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    [SerializeField] private GameObject collectVFX;
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object colliding with the collectible is the Player
        if (other.gameObject.CompareTag("Player"))
        {
            gameManager.Collect();
            Destroy(gameObject);

            if (collectVFX != null)
            {
                // Spawns the VFX at the collectible's current position and rotation
                Instantiate(collectVFX, transform.position, transform.rotation);
            }
        }
    }

}