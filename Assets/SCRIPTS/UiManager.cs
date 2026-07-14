using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UiManager : MonoBehaviour
{
    [Header("Game HUD")]
    [SerializeField] private GameObject GameHUD;
    [SerializeField] private TMP_Text foodCounterText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Slider timeCounterSlider;
    [SerializeField] private Slider foodCounterSlider;
    [SerializeField] private Animator returnHomePage;
    [SerializeField]
    private TMP_Text prompyText;

    [Header("Pause Menu")]
    [SerializeField] public GameObject pauseMenu;
    [Header("Game Over Menu")]
    [SerializeField] public GameObject gameOver;

    [Header("Key Popup")]
    [SerializeField] private GameObject keyPopupPanel;
    [SerializeField] private TMP_Text keyPopupText;
    [SerializeField] private string keyCollectedMessage = "Key Collected! Find the chest to get food!";

    [Header("Win Menu")]
    [SerializeField] public GameObject win;
    [SerializeField] public TextMeshProUGUI victoryTimeText;
    [SerializeField] public GameObject[] stars;
    public static UiManager instance;
    [SerializeField] public GameObject player;

    void Awake()
    {
        // Initialize the Singleton instance
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (foodCounterText == null || timerText == null || keyPopupText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (foodCounterText == null && texts[i].name.Contains("Food"))
                    foodCounterText = texts[i];
                else if (timerText == null && texts[i].name.Contains("Timer"))
                    timerText = texts[i];
                else if (keyPopupText == null && texts[i].name.Contains("KeyPopup"))
                    keyPopupText = texts[i];
            }
        }

        if (keyPopupPanel == null)
        {
            Transform panel = transform.Find("KeyPopupPanel");
            if (panel != null)
                keyPopupPanel = panel.gameObject;
        }
    }

    void Start()
    {
        InitializeUI();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (Time.timeScale == 0)
        {
            Debug.Log("Cursor method has entered");
            player.GetComponent<StarterAssetsInputs>().cursorInputForLook = false;
            player.GetComponent<StarterAssetsInputs>().look = new Vector2(0, 0);
        }
        else
        {
            player.GetComponent<StarterAssetsInputs>().cursorInputForLook = true;
        }

    }

    private void InitializeUI()
    {
        if (GameHUD != null)
            GameHUD.SetActive(true);

        HideKeyPopup();

        if (GameManager.instance == null || foodCounterText == null)
            return;

        foodCounterText.text = $"Food: {GameManager.instance.GetCurrentFood()}/{GameManager.instance.GetTotalFood()}";

        foodCounterSlider.maxValue = (float)GameManager.instance?.GetTotalFood();
        timeCounterSlider.maxValue = (float)GameManager.instance?.GetTotalTime();

        //timerText.text = $"Food:{GameManager.instance.GetCUrrentTime()}/{GameManager.instance.GetTotalime()}";
        player = GameObject.FindGameObjectWithTag("Player");


    }
    public void UpdateFoodScoreUI(int currentScore, int totalScore)
    {
        if (foodCounterText == null)
            return;

        foodCounterText.text = $"Food: {currentScore}/{totalScore}";

        foodCounterSlider.value = currentScore;




    }
    public void UpdateTimeUI(float timeRemaining)
    {
        if (timerText == null)
            return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";

        timeCounterSlider.value = timeRemaining;



        if (timeRemaining < 10f)
        {
            timerText.color = Color.red;
        }
        else
        {
            timerText.color = Color.white;
        }
    }

    public void ShowKeyPopup()
    {
        if (keyPopupText != null)
            keyPopupText.text = keyCollectedMessage;

        if (keyPopupPanel != null)
            keyPopupPanel.SetActive(true);
    }

    public void HideKeyPopup()
    {
        if (keyPopupPanel != null)
            keyPopupPanel.SetActive(false);
    }



    public void ShowReturnHomePrompt()
    {
        prompyText.text = "Return back to camp!";
        returnHomePage.SetTrigger("Trigger");
    }

    public void ShowNeedMoreFoodMessage()
    {
        prompyText.text = "need more snack for the road";
        returnHomePage.SetTrigger("Trigger");
    }


    public void PauseButton()
    {
        Time.timeScale = 0f;
        GameHUD.SetActive(false);
        pauseMenu.SetActive(true);
    }

    public void TogglePause()
    {
        bool isPaused = pauseMenu.activeSelf;
        pauseMenu.SetActive(!isPaused);
        GameHUD.SetActive(isPaused);
        Time.timeScale = isPaused ? 1f : 0f;
    }


    public void RestartButton()
    {
        SceneController.instance.RestartLevel();
    }
    public void NextLevelButton()
    {
        SceneController.instance.LoadNextLevel();
    }
    public void HomeButton()
    {
        SceneController.instance.GoTotitle();
    }
    public void ResumeButton()
    {
        // Centralize all resume actions here
        Time.timeScale = 1f;

        if (GameHUD != null)
            GameHUD.SetActive(true);

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        // If your settings panel is a child of the pause menu, it will hide automatically.
        // If it's a separate GameObject, make sure to explicitly deactivate it here as well:
        // settingsPanel.SetActive(false); 
    }

    public void ShowGameOver()
    {
        Time.timeScale = 0f;

        if (GameHUD != null)
            GameHUD.SetActive(false);

        if (gameOver != null)
            gameOver.SetActive(true);
    }

    public void ShowWinPanel(int starsEarned, float currentTime)
    {
        Time.timeScale = 0f;

        if (GameHUD != null)
            GameHUD.SetActive(false);

        if (gameOver != null)
            gameOver.SetActive(false);

        if (win != null)
            win.SetActive(true);



        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        if (victoryTimeText != null)
        {
            victoryTimeText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].SetActive(i < starsEarned);
        }

        Time.timeScale = 0f;



    }


    // Add this method anywhere inside your UiManager class
    public void HideHUDForCutscene()
    {
        if (GameHUD != null)
        {
            GameHUD.SetActive(false);
        }
    }
}