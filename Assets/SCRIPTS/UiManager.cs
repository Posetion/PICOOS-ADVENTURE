using TMPro;
using UnityEngine;


public class UiManager : MonoBehaviour
{
    [Header("Game HUD")]
    [SerializeField] private GameObject GameHUD;
    [SerializeField] private TMP_Text foodCounterText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Animator returnHomePage;

    [Header("Pause Menu")]
    [SerializeField] public GameObject pauseMenu;

    public static UiManager instance;


    void Awake()
    {

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
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (foodCounterText == null && texts[i].name.Contains("Food"))
                    foodCounterText = texts[i];
                else if (timerText == null && texts[i].name.Contains("Timer"))
                    timerText = texts[i];
            }
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

}
