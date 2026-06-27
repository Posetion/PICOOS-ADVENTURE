using TMPro;
using UnityEngine;


public class UiManager : MonoBehaviour
{
    [Header("Game HUD")]
    [SerializeField] private GameObject GameHUD;
    [SerializeField] private TMP_Text foodCounterText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Animator returnHomePage;






    void Awake()
    {
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





}
