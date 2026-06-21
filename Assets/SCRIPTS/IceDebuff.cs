using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using System.Threading.Tasks; // Required for background async delays
using StarterAssets;           // Required to look up your player script

public class IceDebuff : MonoBehaviour
{
    private bool collected = false; // Prevent double collection

    public GameManager GameManager;
    [Header("Visual Feedback")]
    public float destroyDelay = 0.5f;
    public GameObject[] collectibleModels;
    public ParticleSystem collectedVFX;

    [Header("Freeze Settings")]
    [Tooltip("Duration in seconds before player can move or animate again")]
    public float freezeDuration = 3f; // 3 seconds is usually sweet for a total freeze

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
        // Only the player can trigger, and only once
        if (other.gameObject.CompareTag("Player") && !collected)
        {
            ThirdPersonController playerScript = other.gameObject.GetComponent<ThirdPersonController>();
            Collect(playerScript);
        }
    }

    private void Collect(ThirdPersonController player)
    {
        collected = true;



        if (collectedVFX != null)
        {
            Instantiate(collectedVFX, transform.position, Quaternion.identity);
        }

        OnCollectedEvent.Invoke();

        // Spin up the background freeze timer if the player script exists
        if (player != null)
        {
            _ = StartFreezeTimerAsync(player, freezeDuration);
        }

        // Destroys the collectible instantly on collection!
        Destroy(gameObject);
    }

    // Static async functions bypass the life cycle of the destroyed GameObject
    private static async Task StartFreezeTimerAsync(ThirdPersonController player, float duration)
    {
        // 1. Cache the player's original speeds before zeroing them out
        float originalMoveSpeed = player.MoveSpeed;
        float originalSprintSpeed = player.SprintSpeed;

        // 2. Completely halt movement attributes
        player.MoveSpeed = 0f;
        player.SprintSpeed = 0f;

        // 3. Freeze the player's Animator component if they have one
        Animator playerAnimator = player.GetComponent<Animator>();
        if (playerAnimator != null)
        {
            // Setting speed to 0 pauses the current running animation frame entirely
            playerAnimator.speed = 0f;
        }

        // 4. Wait out the freeze window (multiplied by 1000 for milliseconds)
        await Task.Delay((int)(duration * 1000));

        // 5. Restore original configurations safely if the player asset is still valid
        if (player != null)
        {
            player.MoveSpeed = originalMoveSpeed;
            player.SprintSpeed = originalSprintSpeed;

            // Re-enable the animation system playback rate back to normal speed
            if (playerAnimator != null)
            {
                playerAnimator.speed = 1f;
            }
        }
    }
}