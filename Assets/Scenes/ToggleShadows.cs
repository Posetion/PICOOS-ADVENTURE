using UnityEngine;
using UnityEngine.UI;

public class ShadowToggle : MonoBehaviour
{
    [SerializeField] private Toggle shadowToggle;
    [SerializeField] private Light directionalLight;

    private void Start()
    {
        // Load saved setting
        bool shadowsEnabled = PlayerPrefs.GetInt("Shadows", 1) == 1;

        shadowToggle.isOn = shadowsEnabled;
        ToggleShadows(shadowsEnabled);

        shadowToggle.onValueChanged.AddListener(ToggleShadows);
    }

    public void ToggleShadows(bool enabled)
    {
        PlayerPrefs.SetInt("Shadows", enabled ? 1 : 0);
        PlayerPrefs.Save();

        directionalLight.shadows = enabled
            ? LightShadows.Soft
            : LightShadows.None;
    }
}