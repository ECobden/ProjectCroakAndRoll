using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_MenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    
    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";
    
    private void Start()
    {
        // Initialize menu state
        ShowMainMenu();
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
    
    #endregion
}
