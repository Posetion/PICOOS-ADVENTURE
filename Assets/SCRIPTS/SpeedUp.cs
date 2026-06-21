using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using System.Threading.Tasks; // Required for background async delays
using StarterAssets;           // Required to reference your player script

public class CollectibleEffect : MonoBehaviour
{
    // Define the different types of mechanics this item can use
    public enum EffectType { SpeedUp, SlowDown, Freeze }

    private bool collected = false; // Prevent double collection

    [Header("Collectible Setup")]
    public EffectType itemEffect = EffectType.SpeedUp;
    public GameManager GameManager;

    [Header("Visual Feedback")]
    public float destroyDelay = 0.5f;
    public GameObject[] collectibleModels;
    public ParticleSystem collectedVFX;

    [Header("Effect Modifiers")]
    [Tooltip("Multiplier for SpeedUp/SlowDown (e.g., 1.75f for speed, 0.5f for slow). Ignored if Freeze is selected.")]
    public float speedMultiplier = 1.75f;
    [Tooltip("Duration in seconds before the player returns to normal")]
    public float effectDuration = 15f;

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


        // FIXED: Changed 'collectedX' to 'collectedVFX' to prevent errors
        if (collectedVFX != null)
        {
            Instantiate(collectedVFX, transform.position, Quaternion.identity);
        }

        OnCollectedEvent.Invoke();

        // Spin up the background timer based on the chosen inspector settings
        if (player != null)
        {
            _ = StartEffectTimerAsync(player, itemEffect, speedMultiplier, effectDuration);
        }

        // This object is now instantly removed from the scene Hierarchy!
        Destroy(gameObject);
    }

    // Static async functions bypass the life cycle of the destroyed GameObject
    private static async Task StartEffectTimerAsync(ThirdPersonController player, EffectType effect, float multiplier, float duration)
    {
        // 1. Cache the player's original settings
        float originalMoveSpeed = player.MoveSpeed;
        float originalSprintSpeed = player.SprintSpeed;

        Animator playerAnimator = player.GetComponent<Animator>();
        float originalAnimSpeed = playerAnimator != null ? playerAnimator.speed : 1f;

        // 2. Apply the chosen effect type
        switch (effect)
        {
            case EffectType.SpeedUp:
            case EffectType.SlowDown:
                player.MoveSpeed *= multiplier;
                player.SprintSpeed *= multiplier;
                break;

            case EffectType.Freeze:
                player.MoveSpeed = 0f;
                player.SprintSpeed = 0f;
                if (playerAnimator != null) playerAnimator.speed = 0f; // Freeze animation frame
                break;
        }

        // 3. Wait out the duration window (seconds * 1000 = milliseconds)
        await Task.Delay((int)(duration * 1000));

        // 4. Safely restore all original parameters if the player still exists in the scene
        if (player != null)
        {
            player.MoveSpeed = originalMoveSpeed;
            player.SprintSpeed = originalSprintSpeed;

            if (playerAnimator != null)
            {
                playerAnimator.speed = originalAnimSpeed;
            }
        }
    }
}