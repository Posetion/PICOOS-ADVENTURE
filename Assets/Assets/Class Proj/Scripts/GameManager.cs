using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;

    [Header("Game Configuration Settings")]
    [SerializeField] private int totalPlayerScore = 10;
    [SerializeField] private int totalTimeLimit = 60;
    [SerializeField] private int currentFruitScore = 0;
    [SerializeField] private int currentTime = 0;
    [SerializeField] private bool gameActive = false;
    [SerializeField] private bool canCompleteLevelFlag = false;

    public int GetFruitScore()
    {
        return currentFruitScore;
    }



    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

    }

    



    public void Collect()
    {
        // 1. Spawn the VFX if one is assigned
        
        currentFruitScore += 1;

        // 2. Add your game logic here (e.g., updating the inventory or score)
        Debug.Log($"Collected! You have {currentFruitScore} points.");

        // TODO: ScoreManager.Instance.AddScore(scoreValue);

        // 3. Destroy the collectible object
       
    }
}
