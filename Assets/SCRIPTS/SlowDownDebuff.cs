using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using System.Threading.Tasks; // Required for async Task delays
using StarterAssets;           // Required to reference your player script

public class SlowDownDebuff : MonoBehaviour
{
    private bool collected = false; // Prevent double collection

    [Header("Visual Feedback")]
    public float destroyDelay = 0.5f;
    public GameObject[] collectibleModels;
    public ParticleSystem collectedVFX;

    public ParticleSystem collectedVFX2;

    [Header("Debuff Settings")]
    [Tooltip("0.5f means 50% normal speed")]
    public float speedMultiplier = 0.5f;
    [Tooltip("Duration in seconds before player regains normal speed")]
    public float slowDuration = 15f;
    [Tooltip("Amount of time (in seconds) to deduct from the match clock")]
    public float timePenalty = 10f;

    public UnityEvent OnCollectedEvent;

    private void Start()
    {
        // Make sure the collider is a trigger so the player passes through it
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
            Map1PicoSwimDriver swimScript = other.gameObject.GetComponent<Map1PicoSwimDriver>();
            Collect(playerScript, swimScript);
        }
    }

    private void Collect(ThirdPersonController player, Map1PicoSwimDriver swimDriver)
    {
        collected = true;

        if (collectedVFX != null)
        {
            Instantiate(collectedVFX, transform.position, Quaternion.identity);
        }

        if (collectedVFX2 != null)
        {
            Instantiate(collectedVFX2, transform.position, Quaternion.identity);
        }

        OnCollectedEvent.Invoke();

        // 1. --- DEDUCT TIME FROM THE GAME CLOCK ---
        if (GameManager.instance != null)
        {
            GameManager.instance.AddTime(-timePenalty);
        }
        else
        {
            Debug.LogWarning("Time penalty could not be applied because GameManager.instance is null!");
        }

        // 2. --- START SLOWDOWN TIMER FOR LAND & WATER SPEEDS ---
        if (player != null)
        {
            // We do NOT use 'await' here because we want this method to finish instantly
            _ = StartSlowTimerAsync(player, swimDriver, speedMultiplier, slowDuration);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("explosion");
        }

        // This object is now instantly removed from the scene Hierarchy!
        Destroy(gameObject);
    }

    // An async method runs on the background player loop and survives object destruction
    private static async Task StartSlowTimerAsync(ThirdPersonController player, Map1PicoSwimDriver swimDriver, float multiplier, float duration)
    {
        // --- A. CACHE ORIGINAL LAND & SWIM SPEEDS ---
        float originalMoveSpeed = player.MoveSpeed;
        float originalSprintSpeed = player.SprintSpeed;

        float originalNormalSwim = 0f;
        float originalFastSwim = 0f;
        float originalUnderwaterSwim = 0f;
        bool hasSwimDriver = swimDriver != null;

        if (hasSwimDriver)
        {
            originalNormalSwim = swimDriver.normalSpeed;
            originalFastSwim = swimDriver.fastSwimSpeed;
            originalUnderwaterSwim = swimDriver.underWaterSpeed;
        }

        // --- B. APPLY SPEED REDUCTIONS (DEBUFF) ---
        player.MoveSpeed *= multiplier;
        player.SprintSpeed *= multiplier;

        if (hasSwimDriver)
        {
            swimDriver.normalSpeed *= multiplier;
            swimDriver.fastSwimSpeed *= multiplier;
            swimDriver.underWaterSpeed *= multiplier;
        }

        // --- C. WAIT FOR DEBUFF DURATION ---
        await Task.Delay((int)(duration * 1000));

        // --- D. SAFELY RESTORE ORIGINAL LAND & SWIM SPEEDS ---
        if (player != null)
        {
            player.MoveSpeed = originalMoveSpeed;
            player.SprintSpeed = originalSprintSpeed;
        }

        if (swimDriver != null)
        {
            swimDriver.normalSpeed = originalNormalSwim;
            swimDriver.fastSwimSpeed = originalFastSwim;
            swimDriver.underWaterSpeed = originalUnderwaterSwim;
        }
    }
}