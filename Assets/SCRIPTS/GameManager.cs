using UnityEngine;
using System.Collections;

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
    [SerializeField] private UiManager uiManager;
    [SerializeField] private FinishZone finishZone;


    [Header("Cooking Cutscene")]
    [SerializeField] private GameObject cookingCutsceneTrigger;
    [SerializeField] private GameObject cookingCutscene;
    [Tooltip("How many seconds to wait before ending the cutscene and showing the win screen")]
    [SerializeField] private float cutsceneDuration = 5.0f; // <--- NEW VARIABLE HERE!



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
            UpdateUI();
        }
    }

    public void UpdateTimer()
    {
        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            gameActive = false;

            UpdateUI(); // Show 00:00 before stopping

            if (uiManager == null)
                uiManager = FindAnyObjectByType<UiManager>();

            uiManager?.ShowGameOver();
        }
    }

    public void InitializeGame()
    {
        currentFruitScore = 0;
        currentTime = totalTimeLimit;

        // Include disabled chest spawn collectibles so the total matches the level.
        Collectible[] allCollectibles = FindObjectsByType<Collectible>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        totalFruitScore = 0;
        for (int i = 0; i < allCollectibles.Length; i++)
        {
            if (allCollectibles[i].gameObject.scene == gameObject.scene)
                totalFruitScore++;
        }

        uiManager = FindAnyObjectByType<UiManager>();

        if (uiManager == null)
        {
            Debug.Log("CAnt fond UIManager,The UI will break");
        }

        UpdateUI();
    }

    public void AddCollectible()
    {
        currentFruitScore++;

        Debug.Log("Food Collected " + currentFruitScore + "/" + totalFruitScore);
        if (currentFruitScore >= totalFruitScore)
        {
            Debug.Log("You have collected all the food");
            finishZone?.ActivateFinishZone();

            uiManager?.ShowReturnHomeBar();
        }

        if (uiManager == null)
            uiManager = FindAnyObjectByType<UiManager>();

        UpdateUI();
    }

    public void UpdateUI()
    {
        uiManager?.UpdateFoodScoreUI(currentFruitScore, totalFruitScore);
        uiManager?.UpdateTimeUI(currentTime);
    }

    public void TriggerCookingCutScene()
    {
        StartCoroutine(PlayCutsceneThenWin());
    }

    private IEnumerator PlayCutsceneThenWin()
    {
        // 1. Pause gameplay updates
        gameActive = false;

        // 2. Hide the Game HUD immediately so the screen is clean
        if (uiManager == null) uiManager = FindAnyObjectByType<UiManager>();
        uiManager?.HideHUDForCutscene();

        // 3. Turn on your cutscene GameObject
        if (cookingCutscene != null)
        {
            cookingCutscene.SetActive(true);
        }

        Debug.Log("Cutscene Started, HUD Hidden...");

        // 4. Wait for the custom duration set in the Inspector
        yield return new WaitForSeconds(cutsceneDuration); // <--- UPDATED HERE!

        Debug.Log("Cutscene Finished. Showing Win Panel.");

        // 5. Finally, reveal the win screen
        uiManager?.ShowWinPanel();
    }

    public bool isGameActive() => gameActive;
    public int GetCurrentFood() => currentFruitScore;
    public int GetTotalFood() => totalFruitScore;
    public float GetCurrentTime() => currentTime;
    public float GetTotalTime() => totalTimeLimit;

    public void AddTime(float amount)
    {
        if (!gameActive) return;

        currentTime += amount;

        // Optional: Clamp time so it doesn't exceed the level's total limit
        if (currentTime > totalTimeLimit)
        {
            currentTime = totalTimeLimit;
        }

        Debug.Log("Time added! Current time: " + currentTime);
        UpdateUI();
    }
}