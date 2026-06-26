using UnityEngine;
using UnityEngine.Events;

public class TimePowerUp : MonoBehaviour
{
    private bool collected = false; // Prevent double collection

    [Header("Powerup Settings")]
    [Tooltip("How much time in seconds this power-up rewards the player.")]
    public float timeToAdd = 5.0f;

    [Header("Visual Feedback")]
    public GameObject[] powerupModels;
    public ParticleSystem collectedVFX;

    public UnityEvent OnCollectedEvent;

    private void Start()
    {
        // Make sure the collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Randomly pick a model if variants are supplied
        if (powerupModels != null && powerupModels.Length > 0)
        {
            int index = Random.Range(0, powerupModels.Length);

            foreach (GameObject model in powerupModels)
            {
                if (model != null) model.SetActive(false);
            }

            if (powerupModels[index] != null)
            {
                powerupModels[index].SetActive(true);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only the player can collect, and only once
        if (other.gameObject.CompareTag("Player") && !collected)
        {
            Collect();
        }
    }

    private void Collect()
    {
        collected = true;

        // Audio Handling
        if (AudioManager.Instance == null)
        {
            Debug.LogError("AudioManager Instance is NULL!");
        }
        else
        {
            AudioManager.Instance.PlaySFX("Collect"); // Or replace with a unique SFX like "TimeGain"
        }

        // Add the time to GameManager
        if (GameManager.instance != null)
        {
            GameManager.instance.AddTime(timeToAdd);
        }
        else
        {
            Debug.LogError("GameManager instance is NULL!");
        }

        // Spawn VFX
        if (collectedVFX != null)
        {
            Instantiate(collectedVFX, transform.position, Quaternion.identity);
        }

        OnCollectedEvent?.Invoke();

        // Clean up object
        Destroy(gameObject);
    }
}