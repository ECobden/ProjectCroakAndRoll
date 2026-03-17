using UnityEngine;

public class UI_MenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject collectionsPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private UI_MenuButtonController continueButton;
    [SerializeField] private UI_MenuButtonController newGameButton;
    [SerializeField] private UI_MenuButtonController collectionsButton;
    [SerializeField] private UI_MenuButtonController optionsButton;
    [SerializeField] private UI_MenuButtonController exitButton;

    [Header("Main Menu Intro")]
    [SerializeField] private float introInitialDelay = 0.1f;
    [SerializeField] private float introButtonStagger = 0.12f;

    [Header("Gameplay")]
    [SerializeField] private DB_GameManager gameManager;

    [Header("Main Menu Dice")]
    [SerializeField] private UI_MainMenuDice mainMenuDice;

    [Header("Debug\n" + 
        "F1 - Show main menu + replay intro\n" +
        "F2 - Show options panel\n" +
        "F3 - Hide all menu panels")]
    [SerializeField] private bool enableDebugKeys = true;

    private UI_MenuButtonController[] mainMenuButtons;

    private void Update()
    {
        if (!enableDebugKeys) return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("[MenuManager] DEBUG: Showing main menu + replaying intro");
            DebugShowMainMenu();
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("[MenuManager] DEBUG: Showing options panel");
            ShowOptions();
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("[MenuManager] DEBUG: Hiding all panels");
            DebugHideAll();
        }
    }
    
    private void Start()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<DB_GameManager>();

        mainMenuButtons = CreateMainMenuButtonsArray();

        InitializeMenuButtons();

        // Initialize menu state
        ShowMainMenu();
        PlayMainMenuIntroAnimation();
    }
    
    #region Main Menu Buttons
    
    /// <summary>
    /// Starts a new game and closes the menu
    /// </summary>
    public void OnNewGameButtonClicked()
    {
        Debug.Log("[MenuManager] New Game button clicked - initializing gameplay...");

        if (gameManager != null)
        {
            gameManager.InitializeGame();
        }
        else
        {
            Debug.LogError("[MenuManager] No DB_GameManager found! Cannot start game.");
        }

        OnPlayButtonClicked();
    }
    
    /// <summary>
    /// Starts the game by closing the menu
    /// </summary>
    public void OnPlayButtonClicked()
    {
        Debug.Log("Closing menu and starting gameplay...");

        HideMainMenuDice();
        SetPanelState(mainMenuPanel, false);
        SetPanelState(optionsPanel, false);
        SetPanelState(collectionsPanel, false);
    }
    
    /// <summary>
    /// Opens the options menu
    /// </summary>
    public void OnOptionsButtonClicked()
    {
        Debug.Log("Opening options menu...");
        ShowOptions();
    }

    /// <summary>
    /// Opens the collections menu
    /// </summary>
    public void OnCollectionsButtonClicked()
    {
        Debug.Log("Opening collections menu...");
        ShowCollections();
    }
    
    /// <summary>
    /// Exits the application
    /// </summary>
    public void OnExitButtonClicked()
    {
        Debug.Log("Exiting game...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    #endregion
    
    #region Options Menu
    
    /// <summary>
    /// Returns to the main menu from options
    /// </summary>
    public void OnBackButtonClicked()
    {
        Debug.Log("Returning to main menu...");
        ShowMainMenu();
    }
    
    #endregion
    
    #region Helper Methods

    [ContextMenu("Debug: Show Main Menu")]
    public void DebugShowMainMenu()
    {
        HideMainMenuButtons();

        ShowMainMenu();
        PlayMainMenuIntroAnimation();
    }

    [ContextMenu("Debug: Hide All")]
    public void DebugHideAll()
    {
        HideMainMenuButtons();
        SetPanelState(mainMenuPanel, false);
        SetPanelState(optionsPanel, false);
        SetPanelState(collectionsPanel, false);
    }

    private void ShowMainMenu()
    {
        SetupMainMenuDice();
        SetPanelState(mainMenuPanel, true);
        SetPanelState(optionsPanel, false);
        SetPanelState(collectionsPanel, false);
    }
    
    private void ShowOptions()
    {
        HideMainMenuDice();
        SetPanelState(mainMenuPanel, false);
        SetPanelState(optionsPanel, true);
        SetPanelState(collectionsPanel, false);
    }

    private void ShowCollections()
    {
        HideMainMenuDice();
        SetPanelState(mainMenuPanel, false);
        SetPanelState(optionsPanel, false);
        SetPanelState(collectionsPanel, true);
    }

    private void PlayMainMenuIntroAnimation()
    {
        int index = 0;
        foreach (UI_MenuButtonController button in mainMenuButtons)
        {
            if (button == null) continue;
            button.Show(introInitialDelay + introButtonStagger * index);
            index++;
        }
    }

    private void InitializeMenuButtons()
    {
        foreach (UI_MenuButtonController button in mainMenuButtons)
        {
            if (button != null)
                button.Initialize();
        }

        // Wire up the new game button click handler
        if (newGameButton != null)
        {
            newGameButton.onButtonClick.RemoveListener(OnNewGameButtonClicked);
            newGameButton.onButtonClick.AddListener(OnNewGameButtonClicked);
        }

        if (optionsButton != null)
        {
            optionsButton.onButtonClick.RemoveListener(OnOptionsButtonClicked);
            optionsButton.onButtonClick.AddListener(OnOptionsButtonClicked);
        }

        if (collectionsButton != null)
        {
            collectionsButton.onButtonClick.RemoveListener(OnCollectionsButtonClicked);
            collectionsButton.onButtonClick.AddListener(OnCollectionsButtonClicked);
        }

        if (exitButton != null)
        {
            exitButton.onButtonClick.RemoveListener(OnExitButtonClicked);
            exitButton.onButtonClick.AddListener(OnExitButtonClicked);
        }

        if (optionsPanel != null)
        {
            foreach (UI_MenuButtonController button in optionsPanel.GetComponentsInChildren<UI_MenuButtonController>(true))
            {
                if (button != null)
                    button.Initialize();
            }
        }
    }

    private UI_MenuButtonController[] CreateMainMenuButtonsArray()
    {
        return new[] { continueButton, newGameButton, collectionsButton, optionsButton, exitButton };
    }

    private void HideMainMenuButtons()
    {
        foreach (UI_MenuButtonController button in mainMenuButtons)
        {
            if (button != null)
                button.Hide();
        }
    }

    private void SetupMainMenuDice()
    {
        if (mainMenuDice != null)
            mainMenuDice.SetupForMainMenu();
    }

    private void HideMainMenuDice()
    {
        if (mainMenuDice != null)
            mainMenuDice.HideFromMainMenu();
    }

    private void SetPanelState(GameObject panel, bool isActive)
    {
        if (panel != null)
            panel.SetActive(isActive);
    }
    
    #endregion
}
