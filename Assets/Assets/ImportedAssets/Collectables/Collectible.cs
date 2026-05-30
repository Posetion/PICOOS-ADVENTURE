using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private int scoreValue = 1;
    
    [Header("Effects")]
    [SerializeField] private GameObject collectVFX;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object colliding with the collectible is the Player
        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    private void Collect()
    {
        // 1. Spawn the VFX if one is assigned
        if (collectVFX != null)
        {
            // Spawns the VFX at the collectible's current position and rotation
            Instantiate(collectVFX, transform.position, transform.rotation);
        }

        // 2. Add your game logic here (e.g., updating the inventory or score)
        Debug.Log($"Collected! +{scoreValue} points.");
        
        // TODO: ScoreManager.Instance.AddScore(scoreValue);

        // 3. Destroy the collectible object
        Destroy(gameObject);
    }
}