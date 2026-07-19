using UnityEngine;

public class ApplyShadowSettings : MonoBehaviour
{
    private void Start()
    {
        // Load the saved setting
        bool shadowsEnabled = PlayerPrefs.GetInt("Shadows", 1) == 1;

        Debug.Log("PlayerPref Shadows = " + shadowsEnabled);

        // Find every Light in the scene
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        foreach (Light light in lights)
        {
            // Only affect Directional Lights
            if (light.type == LightType.Directional)
            {
                light.shadows = shadowsEnabled
                    ? LightShadows.Soft
                    : LightShadows.None;

                Debug.Log("Applied to: " + light.name + " | Shadows: " + light.shadows);
            }
        }
    }
}