using TMPro;
using UnityEngine;


public class UiManager : MonoBehaviour
{
    [Header("Game HUD")]
    [SerializeField] private GameObject GameHUD;
    [SerializeField] private TMP_Text foodCounterText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Animator returnHomePage;






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
        GameHUD.SetActive(true);
        foodCounterText.text = $"Food:{GameManager.instance.GetCurrentFood()}/{GameManager.instance.GetTotalFood()}";




        //timerText.text = $"Food:{GameManager.instance.GetCUrrentTime()}/{GameManager.instance.GetTotalime()}";



    }
    public void UpdateFoodScoreUI(int currentScore, int totalScore)
    {
        foodCounterText.text = $"Food: {currentScore}/{totalScore}";
    }
    public void UpdateTimeUI(float timeRemaining)
    {
        int minutes = Mathf.FloorToInt(GameManager.instance.GetCurrentTime() / 60f);
        int seconds = Mathf.FloorToInt(GameManager.instance.GetCurrentTime() % 60f);
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
