using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Game Configuration")]
    [SerializeField] public int totalFruitScore = 5;
    [SerializeField] private int totalTimeLimit = 60;
    [SerializeField] private int currentFruitScore = 0;
    [SerializeField] private float currentTime = 10;
    [SerializeField] private bool gameActive = true;
    [SerializeField] private bool canCompleteLevelFlag = false;

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




    void Start()
    {
        InitializeGame();
    }

    // Update is called once per frame
    void Update()
    {
        if (gameActive)
        {
            UpdateTimer();
        }
    }

    public void UpdateTimer()
    {
        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            gameActive = false;
        }
    }


    public void InitializeGame()
    {
        currentFruitScore = 0;
        currentTime = totalTimeLimit;
        //Find all the fruit game objects from the scene
        totalFruitScore = GameObject.FindGameObjectsWithTag("Collectible").Length;
    }



    public void AddCollectible()
    {
        currentFruitScore++;

        Debug.Log("Food Collected" + currentFruitScore + "/" + totalFruitScore);
        if (currentFruitScore >= totalFruitScore)
        {
            Debug.Log("You have collected all the food");
        }

    }


}
