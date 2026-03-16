using UnityEngine;

public class UI_MenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private UI_MenuButtonController continueButton;
    [SerializeField] private UI_MenuButtonController newGameButton;
    [SerializeField] private UI_MenuButtonController collectionsButton;
    [SerializeField] private UI_MenuButtonController optionsButton;
    [SerializeField] private UI_MenuButtonController exitButton;

    [Header("Main Menu Intro")]
    [SerializeField] private float introInitialDelay = 0.1f;
    [SerializeField] private float introButtonStagger = 0.12f;

    [Header("Debug\n" + 
        "F1 - Show main menu + replay intro\n" +
        "F2 - Show options panel\n" +
        "F3 - Hide all menu panels")]
    [SerializeField] private bool enableDebugKeys = true;


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
        InitializeMenuButtons();

        // Initialize menu state
        ShowMainMenu();
        PlayMainMenuIntroAnimation();
    }
    
    #region Main Menu Buttons
    
    /// <summary>
    /// Starts the game by closing the menu
    /// </summary>
    public void OnPlayButtonClicked()
    {
        Debug.Log("Closing menu and starting gameplay...");

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);
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
        UI_MenuButtonController[] buttons = { continueButton, newGameButton, collectionsButton, optionsButton, exitButton };
        foreach (UI_MenuButtonController btn in buttons)
        {
            if (btn != null)
            {
                btn.Hide();
            }
        }

        ShowMainMenu();
        PlayMainMenuIntroAnimation();
    }

    [ContextMenu("Debug: Hide All")]
    public void DebugHideAll()
    {
        UI_MenuButtonController[] buttons = { continueButton, newGameButton, collectionsButton, optionsButton, exitButton };
        foreach (UI_MenuButtonController btn in buttons)
        {
            if (btn != null)
                btn.Hide();
        }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    private void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }
    
    private void ShowOptions()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    private void PlayMainMenuIntroAnimation()
    {
        UI_MenuButtonController[] buttons = { continueButton, newGameButton, collectionsButton, optionsButton, exitButton };

        int index = 0;
        foreach (UI_MenuButtonController button in buttons)
        {
            if (button == null) continue;
            button.Show(introInitialDelay + introButtonStagger * index);
            index++;
        }
    }

    private void InitializeMenuButtons()
    {
        UI_MenuButtonController[] buttons = { continueButton, newGameButton, collectionsButton, optionsButton, exitButton };

        foreach (UI_MenuButtonController button in buttons)
        {
            if (button != null)
                button.Initialize();
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
    
    #endregion
}
