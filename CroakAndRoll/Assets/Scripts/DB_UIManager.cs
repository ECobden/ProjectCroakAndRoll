using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using DG.Tweening;

public class DB_UIManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("UI References")]
    [SerializeField] private GameObject buttonPanel;
    [SerializeField] private UI_ButtonController buttonLeft;
    [SerializeField] private UI_ButtonController buttonRight;
    [SerializeField] private UI_FloatingScoreController floatingScoreController;
    [SerializeField] private TextMeshProUGUI goalText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private TurnMarker turnMarker;

    #endregion

    #region Private Fields

    #endregion

    #region Initialization

    public void Initialize(Action onRestartClicked)
    {
        SetupButtonListeners(onRestartClicked);
        HideAllPanels();
        ClearScoreText();
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

    #endregion

    #region Betting UI

    public void ShowBetSelection(int smallBetAmount, int largeBetAmount, Action onSmallBetSelected, Action onLargeBetSelected)
    {
        // Position turn marker to player
        if (turnMarker != null)
            turnMarker.SetPlayerTurnPosition();

        // Show button panel first
        ShowButtonPanel();

        // Update button texts for betting
        if (buttonLeft != null)
            buttonLeft.SetButtonText("Small Bet\n$" + smallBetAmount);
        if (buttonRight != null)
            buttonRight.SetButtonText("Large Bet\n$" + largeBetAmount);

        // Set button actions for betting
        if (buttonLeft != null)
            buttonLeft.SetButtonAction(onSmallBetSelected);
        if (buttonRight != null)
            buttonRight.SetButtonAction(onLargeBetSelected);

        // Activate buttons
        if (buttonLeft != null)
            buttonLeft.ActivateButton();
        if (buttonRight != null)
            buttonRight.ActivateButton();

        // Ensure buttons are enabled (in case they were already active from previous round)
        if (buttonLeft != null)
            buttonLeft.EnableButton();
        if (buttonRight != null)
            buttonRight.EnableButton();
    }

    public IEnumerator SwitchToGameplayButtons(Action onStandAction, Action onRollAction)
    {
        // Deactivate buttons
        if (buttonLeft != null)
            buttonLeft.DeactivateButton();
        if (buttonRight != null)
            buttonRight.DeactivateButton();

        // Wait a bit for deactivation animation
        yield return new WaitForSeconds(0.6f);

        // Update button texts
        if (buttonLeft != null)
            buttonLeft.SetButtonText("Stand");
        if (buttonRight != null)
            buttonRight.SetButtonText("Roll");

        // Set new button actions for gameplay
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
        yield return new WaitForSeconds(0.1f); // Wait for activation animation
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
        if (goalText != null)
            goalText.text = text;
    }

    public void ClearScoreText()
    {
        if (floatingScoreController != null)
            floatingScoreController.ClearScore();
    }

    public void UpdateScoreText(int turnTotal, bool isPlayerTurn)
    {
        if (floatingScoreController != null)
            floatingScoreController.UpdateScore(turnTotal, isPlayerTurn);
    }

    #endregion
}
