using TMPro;
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
            tmp.text = $"Score: {gameManager.GetFruitScore()}";
        }
    }
}
