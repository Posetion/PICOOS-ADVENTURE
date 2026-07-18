using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightONOFF : MonoBehaviour
{
    [SerializeField] private Light flashlight;

    void Start()
    {
        // If you didn't manually drag a light into the inspector slot, find it dynamically
        if (flashlight == null)
        {
            flashlight = GetComponentInChildren<Light>();
        }

        // Safety check to prevent errors if a light is entirely missing
        if (flashlight == null)
        {
            Debug.LogError($"[FlashlightONOFF] No Light component found on {gameObject.name} or its children!", this);
        }
    }

    void Update()
    {
        if (flashlight == null) return; // Prevent crashes

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }
}