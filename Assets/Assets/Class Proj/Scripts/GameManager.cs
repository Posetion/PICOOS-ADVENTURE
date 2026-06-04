using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Game Configuration Settings")]
    [SerializeField] private int totalPlayerScore = 10;
    [SerializeField] private int totalTimeLimit = 60;
    [SerializeField] private int currentFruitScore = 0;
    [SerializeField] private float currentTime = 0; // 1. Changed to float internally for smooth tracking
    [SerializeField] private bool gameActive = true;
    [SerializeField] private bool canCompleteLevelFlag = false;
    [SerializeField] private bool timerIsRunning = true;

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

    private void Start()
    {
        // 2. Set the starting time to your time limit when the game begins
        currentTime = totalTimeLimit;
    }

    private void Update()
    {
        if (!timerIsRunning) { return; }

        // 3. Smoothly subtract float time elapsed
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
        }
        else
        {
            currentTime = 0;
            timerIsRunning = false;
            gameActive = false;
            Debug.Log("Time's Up!");
        }
    }

    public int GetFruitScore()
    {
        return currentFruitScore;
    }

    // 4. Returns the float as a clean, rounded whole integer to your Timer script
    public int GetTime()
    {
        return Mathf.CeilToInt(currentTime);
    }

    public void Collect()
    {
        currentFruitScore += 1;
        Debug.Log($"Collected! You have {currentFruitScore} points.");

        if (currentFruitScore >= totalPlayerScore) 
        {
            Win();
        }
    }

    private void Win() 
    {
        Debug.Log("You have won the game!");
    
    }
}
