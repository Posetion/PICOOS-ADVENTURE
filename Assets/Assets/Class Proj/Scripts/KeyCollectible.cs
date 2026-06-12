using UnityEngine;

public class KeyCollectible : MonoBehaviour
{
    [SerializeField] private GameObject collectVFX;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ChestController.hasKey = true;

            if (collectVFX != null)
            {
                Instantiate(collectVFX, transform.position, transform.rotation);
            }

            Debug.Log("Key Collected!");

            Destroy(gameObject);
        }
    }
}