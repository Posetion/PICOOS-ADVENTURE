using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightONOFF : MonoBehaviour
{
    [SerializeField] Light flashlight;

    void Start()
    {
        flashlight = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        // Triggers exactly once per tap, matching how games toggle items
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }

}
