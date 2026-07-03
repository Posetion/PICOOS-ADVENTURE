using TMPro;
using UnityEngine;


public class UiManager : MonoBehaviour
{
    [Header("Game HUD")]
    [SerializeField] private GameObject GameHUD;
    [SerializeField] private TMP_Text foodCounterText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Animator returnHomePage;

<<<<<<< Updated upstream
    [Header("Pause Menu")]
    [SerializeField] public GameObject pauseMenu;
    [Header("Game Over Menu")]
    [SerializeField] public GameObject gameOver;
=======
    [Header("Key Popup")]
    [SerializeField] private GameObject keyPopupPanel;
    [SerializeField] private TMP_Text keyPopupText;
    [SerializeField] private string keyCollectedMessage = "Key Collected! Find the chest to get food!";

>>>>>>> Stashed changes

    [Header("Win Menu")]
    [SerializeField] public GameObject win;
    public static UiManager instance;


    void Awake()
    {
<<<<<<< Updated upstream

        // 2. Initialize the Singleton instance
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (foodCounterText == null || timerText == null)
=======
        if (foodCounterText == null || timerText == null || keyPopupText == null)
>>>>>>> Stashed changes
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

    }

    private void InitializeUI()
    {
        if (GameHUD != null)
            GameHUD.SetActive(true);

        HideKeyPopup();

        if (GameManager.instance == null || foodCounterText == null)
            return;

        foodCounterText.text = $"Food: {GameManager.instance.GetCurrentFood()}/{GameManager.instance.GetTotalFood()}";




        //timerText.text = $"Food:{GameManager.instance.GetCUrrentTime()}/{GameManager.instance.GetTotalime()}";



    }
    public void UpdateFoodScoreUI(int currentScore, int totalScore)
    {
        if (foodCounterText == null)
            return;

        foodCounterText.text = $"Food: {currentScore}/{totalScore}";
    }
    public void UpdateTimeUI(float timeRemaining)
    {
        if (timerText == null)
            return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";

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

    public void ShowReturnHomeBar()
    {
        if (returnHomePage != null)
        {
            // Option A: If you are using a Trigger parameter named "SlideIn"
            returnHomePage.SetTrigger("SlideIn");

            // Option B: If you prefer a Boolean parameter instead, uncomment below:
            // returnHomePage.SetBool("IsOpen", true);
        }
        else
        {
            Debug.LogWarning("ReturnHomePage Animator reference is missing in UIManager!");
        }
    }

    public void PauseButton()
    {
        Time.timeScale = 0f;
        GameHUD.SetActive(false);
        pauseMenu.SetActive(true);
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
        Time.timeScale = 1f;
        GameHUD.SetActive(true);
        pauseMenu.SetActive(false);
    }

    public void ShowGameOver()
    {
        Time.timeScale = 0f;

        if (GameHUD != null)
            GameHUD.SetActive(false);

        if (gameOver != null)
            gameOver.SetActive(true);
    }

    public void ShowWinPanel()
    {
        Time.timeScale = 0f;

        if (GameHUD != null)
            GameHUD.SetActive(false);

        if (gameOver != null)
            gameOver.SetActive(false);

        if (win != null)
            win.SetActive(true);
    }
}
