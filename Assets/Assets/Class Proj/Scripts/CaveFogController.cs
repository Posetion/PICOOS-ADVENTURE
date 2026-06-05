using UnityEngine;
using System.Collections;

public class CaveFogController : MonoBehaviour
{
    [Header("Outside (Forest) Settings")]
    [Range(0f, 0.1f)]
    public float outsideDensity = 0.04f;      // Match your Step 1 density here
    public Color outsideAmbientColor = Color.white;

    [Header("Inside (Cave) Settings")]
    [Range(0f, 0.1f)]
    public float insideDensity = 0f;          // 0 means NO fog inside the cave
    public Color insideAmbientColor = new Color(0.1f, 0.1f, 0.1f); // Dark but not pitch black

    [Header("Transition Settings")]
    public float transitionSpeed = 2.0f;     // Seconds it takes to fade

    private void OnTriggerEnter(Collider other)
    {
        // Only trigger if the object walking through is the player
        if (other.CompareTag("Player"))
        {
            StopAllCoroutines();
            StartCoroutine(TransitionFog(insideDensity, insideAmbientColor));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StopAllCoroutines();
            StartCoroutine(TransitionFog(outsideDensity, outsideAmbientColor));
        }
    }

    IEnumerator TransitionFog(float targetDensity, Color targetAmbient)
    {
        float time = 0;
        float startDensity = RenderSettings.fogDensity;
        Color startAmbient = RenderSettings.ambientLight;

        while (time < transitionSpeed)
        {
            time += Time.deltaTime;
            float progress = time / transitionSpeed;

            // Smoothly shift the global URP fog values
            RenderSettings.fogDensity = Mathf.Lerp(startDensity, targetDensity, progress);
            RenderSettings.ambientLight = Color.Lerp(startAmbient, targetAmbient, progress);
            yield return null;
        }

        // Lock in final target values
        RenderSettings.fogDensity = targetDensity;
        RenderSettings.ambientLight = targetAmbient;
    }
}

