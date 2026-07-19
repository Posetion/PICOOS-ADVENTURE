using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text fpsText;

    private float deltaTime;
    private void Start()
    {
        fpsText = GetComponent<TMP_Text>();
        fpsText.text = "FPS: Calculating...";
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        float fps = 1f / deltaTime;

        fpsText.text = Mathf.RoundToInt(fps) + " FPS";

        if (fps >= 60)
            fpsText.color = Color.green;
        else if (fps >= 30)
            fpsText.color = Color.yellow;
        else
            fpsText.color = Color.red;
    }
}
