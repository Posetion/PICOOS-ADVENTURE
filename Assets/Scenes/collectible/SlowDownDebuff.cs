using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using System.Threading.Tasks; // Required for async Task delays
using StarterAssets;           // Required to reference your player script

public class SlowDownDebuff : MonoBehaviour
{
    private bool collected = false; // Prevent double collection

    public GameManager GameManager;
    [Header("Visual Feedback")]
    public float destroyDelay = 0.5f;
    public GameObject[] collectibleModels;
    public ParticleSystem collectedVFX;

    [Header("Debuff Settings")]
    [Tooltip("0.5f means 50% normal speed")]
    public float speedMultiplier = 0.5f;
    [Tooltip("Duration in seconds before player regains normal speed")]
    public float slowDuration = 15f;

    public UnityEvent OnCollectedEvent;

    private void Start()
    {
        // Make sure the collider is a trigger so the wolf passes through it
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        int index = Random.Range(0, collectibleModels.Length);

        foreach (GameObject model in collectibleModels)
        {
            model.SetActive(false);
        }

        if (collectibleModels.Length > 0)
        {
            collectibleModels[index].gameObject.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only the player can collect, and only once
        if (other.gameObject.CompareTag("Player") && !collected)
        {
            ThirdPersonController playerScript = other.gameObject.GetComponent<ThirdPersonController>();
            Collect(playerScript);
        }
    }

    private void Collect(ThirdPersonController player)
    {
        collected = true;

        if (GameManager.instance != null)
        {
            GameManager.instance.AddCollectible();
        }

        if (collectedVFX != null)
        {
            Instantiate(collectedVFX, transform.position, Quaternion.identity);
        }

        OnCollectedEvent.Invoke();

        // Fire off the background timer task if the player script exists
        if (player != null)
        {
            // We do NOT use 'await' here because we want this method to finish instantly
            _ = StartSlowTimerAsync(player, speedMultiplier, slowDuration);
        }

        // This object is now instantly removed from the scene Hierarchy!
        Destroy(gameObject);
    }

    // An async method runs on the background player loop and survives object destruction
    private static async Task StartSlowTimerAsync(ThirdPersonController player, float multiplier, float duration)
    {
        // 1. Cache the player's original speeds before making modifications
        float originalMoveSpeed = player.MoveSpeed;
        float originalSprintSpeed = player.SprintSpeed;

        // 2. Slow down the player
        player.MoveSpeed *= multiplier;
        player.SprintSpeed *= multiplier;

        // 3. Wait for the designated duration. 
        // Task.Delay works in milliseconds, so we multiply our seconds by 1000.
        await Task.Delay((int)(duration * 1000));

        // 4. Safely restore the original speeds if the player hasn't been destroyed/left the match
        if (player != null)
        {
            player.MoveSpeed = originalMoveSpeed;
            player.SprintSpeed = originalSprintSpeed;
        }
    }
}