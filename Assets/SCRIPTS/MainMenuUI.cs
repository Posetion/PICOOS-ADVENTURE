using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class MainMenuUI : MonoBehaviour
{
    public Button playButton;
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Level Buttons")]
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private GameObject[] lockIcons;

    [Header("Level Selection Navigation")]
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private Button playSelectedLevelButton;
    [SerializeField] private GameObject[] levelHighlights;
    [SerializeField] private TextMeshProUGUI selectedLevelText;

    private const int TOTAL_LEVELS = 3;
    private int currentSelectedLevelIndex = 0;



    void Start()
    {

        ShowMainPanel();
        CheckLevelUnlocks();
        UpdateLevelSelectionDisplay();

        if (playButton != null && SceneController.instance != null)
        {
            // This ensures it always points to the active, surviving SceneController instance
            playButton.onClick.AddListener(() => SceneController.instance.LoadScene(1));
        }
    }


    private void CheckLevelUnlocks()
    {
        for (int i = 0; i < TOTAL_LEVELS; i++)
        {
            int levelNumber = i + 1;
            if (levelNumber == 1)
            {
                levelButtons[i].interactable = true;
                lockIcons[i].SetActive(false);
            }
            else
            {
                int previousLevel = levelNumber - 1;
                bool previousComplete = PlayerPrefs.GetInt($"Level{previousLevel}Complete", 0) == 1;
                levelButtons[i].interactable = previousComplete;
                lockIcons[i].SetActive(!previousComplete);
            }
        }
    }


    public void LoadLevel(int levelNumber)
    {
        SceneController.instance?.LoadScene(levelNumber);
    }

    public void OnLeftArrow()
    {
        currentSelectedLevelIndex--;
        if (currentSelectedLevelIndex < 0)
        {
            currentSelectedLevelIndex = TOTAL_LEVELS - 1;
        }
        UpdateLevelSelectionDisplay();
        AudioManager.Instance?.PlaySFX("UIClick");
    }

    public void OnRightArrow()
    {
        currentSelectedLevelIndex++;
        if (currentSelectedLevelIndex >= TOTAL_LEVELS)
        {
            currentSelectedLevelIndex = 0;
        }
        UpdateLevelSelectionDisplay();
        AudioManager.Instance?.PlaySFX("UIClick");
    }

    private void UpdateLevelSelectionDisplay()
    {
        for (int i = 0; i < TOTAL_LEVELS; i++)
        {
            if (levelHighlights != null && i < levelHighlights.Length && levelHighlights[i] != null)
            {
                levelHighlights[i].SetActive(i == currentSelectedLevelIndex);
            }
        }

        if (selectedLevelText != null)
        {
            int levelNumber = currentSelectedLevelIndex + 1;
            selectedLevelText.text = $"{levelNumber}/3";
        }

        if (playSelectedLevelButton != null)
        {
            playSelectedLevelButton.interactable = levelButtons[currentSelectedLevelIndex].interactable;
        }
    }

    public void OnPlayButton()
    {
        int levelToLoad = 1;
        for (int i = 1; i <= TOTAL_LEVELS; i++)
        {
            bool levelComplete = PlayerPrefs.GetInt($"Level{i}Complete", 0) == 1;
            if (!levelComplete)
            {
                levelToLoad = i;
                break;
            }
        }

        if (levelToLoad == 1 && PlayerPrefs.GetInt("Level3Complete", 0) == 1)
        {
            levelToLoad = TOTAL_LEVELS;
        }

        LoadLevel(levelToLoad);
    }


    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void ShowLevelSelect()
    {
        mainPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
        currentSelectedLevelIndex = 0;
        UpdateLevelSelectionDisplay();
    }

    public void ShowSettings()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void BackToMain()
    {
        ShowMainPanel();
    }

    //#endregion


    public void OnQuitButton()
    {
        SceneController.instance?.QuitGame();
    }


}