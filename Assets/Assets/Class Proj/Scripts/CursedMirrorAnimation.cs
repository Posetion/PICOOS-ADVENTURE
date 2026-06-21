using UnityEngine;

/// <summary>
/// Makes a mirror float and sway forever with a per-instance random phase
/// so multiple mirrors do not animate in sync.
/// </summary>
[DisallowMultipleComponent]
public class CursedMirrorAnimation : MonoBehaviour
{
    [Header("Floating")]
    [SerializeField] private float floatHeight = 0.5f;
    [SerializeField] private float floatSpeed = 1.5f;

    [Header("Magical Sway")]
    [SerializeField] private float swayAngle = 8f;
    [SerializeField] private float swaySpeedMultiplier = 1.2f;

    private Vector3 startPosition;
    private Vector3 startEulerAngles;
    private float phaseOffset;

    private void Start()
    {
        // Capture the mirror's original pose so animation is always relative.
        startPosition = transform.position;
        startEulerAngles = transform.eulerAngles;

        // Different start phase per mirror to avoid synchronized movement.
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float t = Time.time * floatSpeed + phaseOffset;

        // Float only on the Y axis.
        Vector3 position = startPosition;
        position.y = startPosition.y + Mathf.Sin(t) * floatHeight;
        transform.position = position;

        // Add a gentle Z-axis sway for a cursed/magical feel.
        float sway = Mathf.Sin(t * swaySpeedMultiplier) * swayAngle;
        transform.rotation = Quaternion.Euler(
            startEulerAngles.x,
            startEulerAngles.y,
            startEulerAngles.z + sway
        );
    }
}
