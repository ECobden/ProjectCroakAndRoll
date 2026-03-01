using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using DG.Tweening;

// UI Manager for dice battle game
public class DB_UIManager : MonoBehaviour
{
    public static DB_UIManager Instance { get; private set; }

    #region Serialized Fields

    [Header("UI References")]
    [SerializeField] private GameObject buttonPanel;
    [SerializeField] private UI_ButtonController buttonLeft;
    [SerializeField] private UI_ButtonController buttonRight;
    [SerializeField] private UI_FloatingScoreController floatingScoreController;
    [SerializeField] private UI_StandValueController standValueController;
    [SerializeField] private UI_GoalTextController goalTextController;
    [SerializeField] private UI_RoundResultController roundResultController;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private TurnMarker turnMarker;
    
    [Header("Round Total Display")]
    [SerializeField] private TextMeshProUGUI playerRoundTotalText;
    [SerializeField] private TextMeshProUGUI houseRoundTotalText;
    
    [Header("Round Counter Display")]
    [SerializeField] private CanvasGroup roundCountCanvasGroup;
    [SerializeField] private TextMeshProUGUI roundCountText;
    [SerializeField] private float deleteSpeed = 0.05f;
    [SerializeField] private float typeSpeed = 0.05f;

    [Header("Dice Info Display")]
    [SerializeField] private DiceInfoPanel diceInfoPanel;

    #endregion

    #region Private Fields

    private Coroutine activeRoundTextAnimation;
    private WaitForSeconds deleteWait;
    private WaitForSeconds typeWait;
    private char[] textBuffer;

    #endregion

    #region Initialization

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Initialize(Action onRestartClicked)
    {
        SetupButtonListeners(onRestartClicked);
        HideAllPanels();
        ClearRollScoreText();
        CacheWaitForSeconds();
    }

    private void CacheWaitForSeconds()
    {
        deleteWait = new WaitForSeconds(deleteSpeed);
        typeWait = new WaitForSeconds(typeSpeed);
    }

    private void SetupButtonListeners(Action onRestartClicked)
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(() => onRestartClicked?.Invoke());
    }

    private void HideAllPanels()
    {
        if (buttonPanel != null)
            buttonPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    #endregion

    #region Dice Info Display

    public void ShowDiceInfoPanel(DieData dieData, Vector3 worldPosition)
    {
        if (diceInfoPanel == null)
        {
            Debug.LogWarning("[DB_UIManager] DiceInfoPanel reference is not assigned.");
            return;
        }

        diceInfoPanel.gameObject.SetActive(true);
        diceInfoPanel.SetDiceInfo(dieData);
        diceInfoPanel.SetCloseCallback(HideDiceInfoPanel);
        PositionDiceInfoPanel(worldPosition);
    }

    public void HideDiceInfoPanel()
    {
        if (diceInfoPanel != null)
        {
            diceInfoPanel.gameObject.SetActive(false);
        }
    }

    public bool IsDiceInfoPanelVisible()
    {
        return diceInfoPanel != null && diceInfoPanel.gameObject.activeSelf;
    }

    private void PositionDiceInfoPanel(Vector3 worldPosition)
    {
        if (diceInfoPanel == null)
            return;

        RectTransform panelRect = diceInfoPanel.transform as RectTransform;
        Canvas canvas = diceInfoPanel.GetComponentInParent<Canvas>();

        if (panelRect == null || canvas == null)
        {
            Debug.LogWarning("[DB_UIManager] Dice info panel must be under a Canvas with RectTransform.");
            return;
        }

        Camera worldCamera = Camera.main;
        Vector3 screenPoint = worldCamera != null
            ? worldCamera.WorldToScreenPoint(worldPosition)
            : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPoint,
                uiCamera,
                out Vector2 localPoint))
        {
            panelRect.anchoredPosition = localPoint;
        }
    }

    #endregion

    #region Button Panel

    public void ShowButtonPanel()
    {
        if (buttonPanel != null)
            buttonPanel.SetActive(true);
    }

    public void HideButtonPanel()
    {
        if (buttonPanel != null)
            buttonPanel.SetActive(false);
    }
    
    public IEnumerator ShowGameplayButtonsDirectly(Action onStandAction, Action onRollAction)
    {
        // Show button panel
        ShowButtonPanel();
        
        // Update button texts
        if (buttonLeft != null)
            buttonLeft.SetButtonText("Stand");
        if (buttonRight != null)
            buttonRight.SetButtonText("Roll");

        // Set button actions for gameplay
        if (buttonLeft != null)
            buttonLeft.SetButtonAction(onStandAction);
        if (buttonRight != null)
            buttonRight.SetButtonAction(onRollAction);

        // Activate buttons
        if (buttonLeft != null)
            buttonLeft.ActivateButton();
        if (buttonRight != null)
            buttonRight.ActivateButton();

        // Stand button should start disabled (player hasn't rolled yet)
        yield return new WaitForSeconds(0.6f); // Wait for activation animation
        if (buttonLeft != null)
            buttonLeft.DisableButton();
    }

    #endregion

    #region Gameplay Buttons

    public void DisableGameplayButtons()
    {
        if (buttonLeft != null)
            buttonLeft.DisableButton();
        if (buttonRight != null)
            buttonRight.DisableButton();
    }

    public void EnableStandButton()
    {
        if (buttonLeft != null)
            buttonLeft.EnableButton();
    }

    public void EnableRollButton()
    {
        if (buttonRight != null)
            buttonRight.EnableButton();
    }

    public void SetRollButtonText(string text)
    {
        if (buttonRight != null)
            buttonRight.SetButtonText(text);
    }

    public void DeactivateButtons()
    {
        if (buttonLeft != null)
            buttonLeft.DeactivateButton();
        if (buttonRight != null)
            buttonRight.DeactivateButton();
    }

    #endregion

    #region Game Over Panel

    public void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void HideGameOverPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    #endregion

    #region Turn Marker

    public void SetTurnMarkerToPlayer()
    {
        if (turnMarker != null)
            turnMarker.SetPlayerTurnPosition();
    }

    public void SetTurnMarkerToHouse()
    {
        if (turnMarker != null)
            turnMarker.SetHouseTurnPosition();
    }

    #endregion

    #region Text Updates

    public void UpdateGoalText(string text)
    {
        if (goalTextController != null)
            goalTextController.SetGoalText(text);
    }

    public void UpdateGoalRollProgress(int currentValue, bool isPlayerTurn)
    {
        if (goalTextController != null)
            goalTextController.UpdateRollProgress(currentValue, isPlayerTurn);
    }

    public void ResetGoalRollProgress()
    {
        if (goalTextController != null)
            goalTextController.ResetRollProgress();
    }

    public void ClearRollScoreText()
    {
        if (floatingScoreController != null)
            floatingScoreController.ClearScore();
    }

    public void UpdateRollScoreText(int rollTotal)
    {
        if (floatingScoreController != null)
            floatingScoreController.UpdateScore(rollTotal);
    }

    public void ShowStandValue(string value)
    {
        if (standValueController != null)
            standValueController.Show(value);
    }

    public void HideStandValue()
    {
        if (standValueController != null)
            standValueController.Hide();
    }

    public float GetScoreAnimationDuration(int rollValue)
    {
        if (floatingScoreController != null)
            return floatingScoreController.GetScoreTransferDuration(rollValue);
        return 0f;
    }

    public bool IsScoreAnimating()
    {
        if (floatingScoreController != null)
            return floatingScoreController.IsAnimating();
        return false;
    }

    #endregion

    #region Round Result Messages

    public void ShowPlayerBust()
    {
        if (roundResultController != null)
            roundResultController.ShowPlayerBust();
    }

    public void ShowHouseBust()
    {
        if (roundResultController != null)
            roundResultController.ShowHouseBust();
    }

    public void ShowPlayer21()
    {
        if (roundResultController != null)
            roundResultController.ShowPlayer21();
    }

    public void ShowHouse21()
    {
        if (roundResultController != null)
            roundResultController.ShowHouse21();
    }

    public void ShowHouseWins()
    {
        if (roundResultController != null)
            roundResultController.ShowHouseWins();
    }

    public void ShowPlayerWins()
    {
        if (roundResultController != null)
            roundResultController.ShowPlayerWins();
    }

    public void ShowHouseCheated()
    {
        if (roundResultController != null)
            roundResultController.ShowHouseCheated();
    }

    #endregion
    
    #region Round Total Display
    
    public void UpdatePlayerRoundTotal(int total)
    {
        if (playerRoundTotalText != null)
            playerRoundTotalText.text = total.ToString();
    }
    
    public void UpdateHouseRoundTotal(int total)
    {
        if (houseRoundTotalText != null)
            houseRoundTotalText.text = total.ToString();
    }
    
    public void ClearRoundTotals()
    {
        if (playerRoundTotalText != null)
            playerRoundTotalText.text = " ";
        if (houseRoundTotalText != null)
            houseRoundTotalText.text = " ";
    }
    
    #endregion

    #region Round Counter Display

    /// <summary>
    /// Update the round counter display with animation.
    /// </summary>
    public void UpdateRoundDisplay(int roundNumber)
    {
        if (roundCountText == null) return;
        
        // Stop any existing animation to prevent overlapping
        if (activeRoundTextAnimation != null)
        {
            StopCoroutine(activeRoundTextAnimation);
        }
        
        activeRoundTextAnimation = StartCoroutine(AnimateRoundTextChange($"Round {roundNumber}"));
    }

    /// <summary>
    /// Shows the round UI by setting canvas group alpha to 1.
    /// </summary>
    public void ShowRoundCounter()
    {
        if (roundCountCanvasGroup != null)
        {
            roundCountCanvasGroup.alpha = 1f;
        }
    }

    /// <summary>
    /// Hides the round UI by setting canvas group alpha to 0.
    /// </summary>
    public void HideRoundCounter()
    {
        if (roundCountCanvasGroup != null)
        {
            roundCountCanvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// Animates the round text change by deleting old text and typing new text character by character.
    /// Optimized to reduce garbage allocation by using char arrays instead of substring operations.
    /// </summary>
    private IEnumerator AnimateRoundTextChange(string newText)
    {
        // Delete current text character by character
        string currentText = roundCountText.text;
        int currentLength = currentText.Length;
        
        // Ensure buffer is large enough
        int maxLength = Mathf.Max(currentLength, newText.Length);
        if (textBuffer == null || textBuffer.Length < maxLength)
        {
            textBuffer = new char[maxLength];
        }
        
        // Delete phase - work backwards through current text
        for (int i = currentLength - 1; i >= 0; i--)
        {
            roundCountText.text = currentText.Substring(0, i);
            yield return deleteWait;
        }
        
        // Ensure text is empty
        roundCountText.text = string.Empty;
        
        // Type phase - copy characters from new text to buffer
        newText.CopyTo(0, textBuffer, 0, newText.Length);
        
        for (int i = 1; i <= newText.Length; i++)
        {
            roundCountText.text = new string(textBuffer, 0, i);
            yield return typeWait;
        }
        
        activeRoundTextAnimation = null;
    }
    
}
    
    #endregion
