using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class UI_MenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Main Menu Intro")]
    [SerializeField] private List<UI_MenuButtonController> mainMenuButtons = new List<UI_MenuButtonController>();
    [SerializeField] private float introInitialDelay = 0.1f;
    [SerializeField] private float introButtonStagger = 0.12f;
    
    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";
    
    private void Start()
    {
        // Initialize menu state
        ShowMainMenu();
        PlayMainMenuIntroAnimation();
    }
    
    #region Main Menu Buttons
    
    /// <summary>
    /// Starts the game by loading the gameplay scene
    /// </summary>
    public void OnPlayButtonClicked()
    {
        Debug.Log("Loading gameplay scene...");
        SceneManager.LoadScene(gameplaySceneName);
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
        List<UI_MenuButtonController> orderedButtons = GetOrderedMainMenuButtons();

        for (int i = 0; i < orderedButtons.Count; i++)
        {
            UI_MenuButtonController button = orderedButtons[i];
            if (button == null)
                continue;

            float delay = introInitialDelay + (introButtonStagger * i);
            button.Show(delay);
        }
    }

    private List<UI_MenuButtonController> GetOrderedMainMenuButtons()
    {
        List<UI_MenuButtonController> buttons = new List<UI_MenuButtonController>();

        if (mainMenuButtons != null && mainMenuButtons.Count > 0)
        {
            for (int i = 0; i < mainMenuButtons.Count; i++)
            {
                if (mainMenuButtons[i] != null)
                    buttons.Add(mainMenuButtons[i]);
            }
        }
        else if (mainMenuPanel != null)
        {
            UI_MenuButtonController[] panelButtons = mainMenuPanel.GetComponentsInChildren<UI_MenuButtonController>(true);
            for (int i = 0; i < panelButtons.Length; i++)
            {
                if (panelButtons[i] != null)
                    buttons.Add(panelButtons[i]);
            }
        }

        buttons.Sort((a, b) =>
        {
            RectTransform aRect = a != null ? a.GetComponent<RectTransform>() : null;
            RectTransform bRect = b != null ? b.GetComponent<RectTransform>() : null;

            float aY = aRect != null ? aRect.anchoredPosition.y : float.MinValue;
            float bY = bRect != null ? bRect.anchoredPosition.y : float.MinValue;

            return bY.CompareTo(aY);
        });

        return buttons;
    }
    
    #endregion
}
