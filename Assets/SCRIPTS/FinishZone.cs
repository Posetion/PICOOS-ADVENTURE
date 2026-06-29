using UnityEngine;

public class FinishZone : MonoBehaviour
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
            Debug.Log("Player reached Finish Zone!");
            GameManager.instance?.TriggerCookingCutScene();
            UiManager.instance.ShowWinPanel();
        }
    }
}