using UnityEngine;

public class TutomapLeave : MonoBehaviour
{
    private bool activated = false;

    private void Start()
    {
        // Hide the finish zone until all collectibles are collected
        gameObject.SetActive(false);
    }

    // Called by the GameManager
    public void ActivateFinishZone()
    {
        activated = true;
        gameObject.SetActive(true);

        Debug.Log("Finish Zone Activated!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!activated)
            return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player reached Finish Zone! Sending to main menu...");



            // Direct call to your SceneController instance to load Scene 0 (Main Menu)
            if (SceneController.instance != null)
            {
                SceneController.instance.GoTotitle();
            }
            else
            {
                Debug.LogError("SceneController instance not found in the scene!");
            }
        }
    }
}