/*using TMPro;
using UnityEngine;

public class Score : MonoBehaviour
{
    // Changed from TextMeshPro to TextMeshProUGUI for UI Canvas compatibility
    private TextMeshProUGUI tmp;

    public GameManager gameManager;

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        // Added a null check to prevent errors if GameManager is missing
        if (gameManager != null)
        {
            tmp.text = $"Items Collected {gameManager.GetFruitScore()}/10";
        }

        if (gameManager.GetFruitScore() >= gameManager.GetFruitScore()) 
        {
            tmp.color = Color.green; // Change text color to green when the player has collected enough items

        }
    }
}
*/