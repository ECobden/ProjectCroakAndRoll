using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class DB_GameManager : MonoBehaviour
{
    #region Enums
    
    public enum GameState
    {
        BettingPhase,      // Player selecting bet
        PlayerTurn,        // Player rolling dice
        PlayerStanding,    // Player has stood, transitioning to house
        HouseTurn,         // House rolling dice
        RoundOver,         // Round ending, determining winner
        GameOver           // Game completely over
    }
    
    #endregion

    #region Serialized Fields

    [Header("Player References")]
    [SerializeField] private Player player;
    [SerializeField] private House house;

    [Header("Round Manager")]
    [SerializeField] private DB_RoundManager roundManager;

    [Header("Dice Manager")]
    [SerializeField] private DB_DiceManager diceManager;

    [Header("UI Manager")]
    [SerializeField] private DB_UIManager uiManager;

    [Header("Game Settings")]
    [SerializeField] private int smallBetAmount = 50;
    [SerializeField] private int largeBetAmount = 200;
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private float newRoundDelay = 1.5f;
    
    #endregion

    #region Private Fields
    
    private GameState currentState = GameState.BettingPhase;
    
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeDice();
        InitializeUI();

        roundManager.InitializeRound();
        ShowBetSelectionPanel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(restartKey))
        {
            RestartGame();
        }
    }
    
    #endregion

    #region Initialization

    private void InitializeDice()
    {
        if (diceManager != null)
            diceManager.Initialize();
    }

    private void InitializeUI()
    {
        if (uiManager != null)
            uiManager.Initialize(RestartGame);
    }
    
    #endregion

    #region State Management

    private void TransitionToState(GameState newState)
    {
        if (currentState == newState)
        {
            Debug.LogWarning($"Already in state {newState}");
            return;
        }

        Debug.Log($"State Transition: {currentState} -> {newState}");
        
        // Exit current state
        ExitState(currentState);
        
        // Update state
        GameState previousState = currentState;
        currentState = newState;
        
        // Enter new state
        EnterState(newState, previousState);
    }

    private void ExitState(GameState state)
    {
        switch (state)
        {
            case GameState.BettingPhase:
                // Clear bet UI
                break;
                
            case GameState.PlayerTurn:
                if (uiManager != null)
                    uiManager.DisableGameplayButtons();
                break;
                
            case GameState.HouseTurn:
                if (uiManager != null)
                    uiManager.ClearScoreText();
                break;
                
            case GameState.RoundOver:
                // Reset goal text state for new round
                if (uiManager != null)
                    uiManager.ResetGoalRollProgress();
                break;
        }
    }

    private void EnterState(GameState state, GameState previousState)
    {
        switch (state)
        {
            case GameState.BettingPhase:
                ShowBetSelectionPanel();
                break;
                
            case GameState.PlayerTurn:
                // If coming from betting, need to switch UI first
                if (previousState == GameState.BettingPhase)
                {
                    StartCoroutine(TransitionFromBettingToPlayerTurn());
                }
                else
                {
                    StartPlayerTurnInternal();
                }
                break;
                
            case GameState.PlayerStanding:
                // Brief transition state before house turn
                TransitionToState(GameState.HouseTurn);
                break;
                
            case GameState.HouseTurn:
                StartHouseTurnInternal();
                break;
                
            case GameState.RoundOver:
                HandleRoundOver();
                break;
                
            case GameState.GameOver:
                if (uiManager != null)
                    uiManager.ShowGameOverPanel();
                break;
        }
    }
    
    private IEnumerator TransitionFromBettingToPlayerTurn()
    {
        if (uiManager == null) yield break;
        
        // Switch UI buttons with animation
        yield return StartCoroutine(uiManager.SwitchToGameplayButtons(
            () => { if (player != null) player.Stand(); },
            () => { if (player != null) player.RollDice(); }
        ));
        
        // UI is ready, now start the turn
        StartPlayerTurnInternal();
    }
    
    #endregion

    #region Turn Management

    private void StartPlayerTurnInternal()
    {
        if (uiManager != null)
        {
            uiManager.UpdateGoalText("Roll Closest to 21");
            uiManager.SetTurnMarkerToPlayer();
        }
    }

    public void StartPlayerTurn()
    {
        TransitionToState(GameState.PlayerTurn);
    }

    public void EndPlayerTurn()
    {
        if (currentState != GameState.PlayerTurn)
        {
            Debug.LogWarning($"EndPlayerTurn called but not in PlayerTurn state. Current: {currentState}");
            return;
        }

        Debug.Log("Player's turn ended");
        TransitionToState(GameState.PlayerStanding);
    }

    private void StartHouseTurnInternal()
    {
        if (player != null && uiManager != null)
        {
            int playerScore = player.GetTurnValue();
            uiManager.UpdateGoalText($"House must roll {playerScore} to win");
        }
        
        if (uiManager != null)
            uiManager.SetTurnMarkerToHouse();

        if (house != null)
        {
            house.OnTurnStart();
        }
        else
        {
            Debug.LogError("House is null! Cannot start house turn.");
        }
    }

    public void StartHouseTurn()
    {
        TransitionToState(GameState.HouseTurn);
    }

    public void EndHouseTurn()
    {
        if (currentState != GameState.HouseTurn)
        {
            Debug.LogWarning($"EndHouseTurn called but not in HouseTurn state. Current: {currentState}");
            return;
        }

        Debug.Log("House's turn ended");
        TransitionToState(GameState.RoundOver);
    }
    
    #endregion

    #region Game Outcomes

    public void PlayerBust()
    {
        Debug.Log("GAME OVER - Player busted!");
        
        if (uiManager != null)
            uiManager.ClearScoreText();
        
        TransitionToState(GameState.RoundOver);
    }

    public void HouseBust()
    {
        Debug.Log("PLAYER WINS - House busted!");
        
        if (player != null && house != null)
        {
            int betAmount = player.GetBetAmount();
            int totalPayout = house.PayWinnings(betAmount);
            player.AddMoney(totalPayout);
        }
        
        if (uiManager != null)
            uiManager.ClearScoreText();
        
        TransitionToState(GameState.RoundOver);
    }

    public void HouseWins()
    {
        Debug.Log("HOUSE WINS - House beat player's score!");
        
        if (uiManager != null)
            uiManager.ClearScoreText();
        
        TransitionToState(GameState.RoundOver);
    }

    public void PlayerOutOfMoney()
    {
        Debug.Log("GAME OVER - Player is out of money!");
        TransitionToState(GameState.GameOver);
    }

    private void HandleRoundOver()
    {
        // Check if game should end completely
        if (player != null && player.GetCurrentMoney() < smallBetAmount)
        {
            Debug.Log("GAME OVER - Player cannot afford even the smallest bet!");
            TransitionToState(GameState.GameOver);
            return;
        }

        if (house != null && house.GetCurrentMoney() <= 0)
        {
            Debug.Log("GAME OVER - Player wins! House is out of money!");
            TransitionToState(GameState.GameOver);
            return;
        }

        // Game continues - start a new round after delay
        StartCoroutine(StartNewRoundAfterDelay());
    }
    
    #endregion

    #region Betting

    public void OnSmallBetSelected()
    {
        if (currentState != GameState.BettingPhase)
        {
            Debug.LogWarning($"Bet selected but not in betting phase. Current state: {currentState}");
            return;
        }

        if (player == null) return;

        if (player.GetCurrentMoney() < smallBetAmount)
        {
            Debug.Log($"Cannot afford small bet of {smallBetAmount}!");
            PlayerOutOfMoney();
            return;
        }

        Debug.Log($"Player selected small bet: {smallBetAmount}");
        
        // Start player's turn with bet amount
        if (player != null)
            player.OnTurnStart(smallBetAmount);
        
        // Transition to PlayerTurn (which will handle UI switch)
        TransitionToState(GameState.PlayerTurn);
    }

    public void OnLargeBetSelected()
    {
        if (currentState != GameState.BettingPhase)
        {
            Debug.LogWarning($"Bet selected but not in betting phase. Current state: {currentState}");
            return;
        }

        if (player == null) return;

        if (player.GetCurrentMoney() < largeBetAmount)
        {
            Debug.Log($"Cannot afford large bet of {largeBetAmount}!");
            return;
        }

        Debug.Log($"Player selected large bet: {largeBetAmount}");
        
        // Start player's turn with bet amount
        if (player != null)
            player.OnTurnStart(largeBetAmount);
        
        // Transition to PlayerTurn (which will handle UI switch)
        TransitionToState(GameState.PlayerTurn);
    }

    public void OnStartNewRound()
    {
        Debug.Log("Starting new round...");

        if (uiManager != null)
            uiManager.HideStandValue();
            
        if (roundManager != null)
            roundManager.CountUpRound();
               
        if (house != null)
            house.ResetTurnValue();
        
        if (uiManager != null)
            uiManager.ClearScoreText();

        // Transition to betting phase
        TransitionToState(GameState.BettingPhase);
    }

    private IEnumerator StartNewRoundAfterDelay()
    {
        yield return new WaitForSeconds(newRoundDelay);
        OnStartNewRound();
    }
    
    #endregion

    #region Game Control

    public void RestartGame()
    {
        Debug.Log("Restarting game...");
        
        ResetGameState();
        ResetPlayers();
        
        if (diceManager != null)
            diceManager.RefreshDiceIdlePositions();
        
        // Reinitialize and show bet selection
        roundManager.InitializeRound();
        ShowBetSelectionPanel();
    }

    private void ResetGameState()
    {
        currentState = GameState.BettingPhase;
        
        if (uiManager != null)
        {
            uiManager.HideGameOverPanel();
            uiManager.DeactivateButtons();
        }
    }

    private void ResetPlayers()
    {
        if (player != null)
            player.ResetMoney();

        if (house != null)
        {
            house.ResetMoney();
            house.ResetTurnValue();
        }
    }
    
    #endregion

    #region UI Helper

    private void ShowBetSelectionPanel()
    {
        if (uiManager != null)
        {
            uiManager.ShowBetSelection(smallBetAmount, largeBetAmount, OnSmallBetSelected, OnLargeBetSelected);
            uiManager.UpdateGoalText("Select your bet");
        }
    }
    
    #endregion

    #region Dice Management

    public void RollSharedDice(System.Action<int, int> onComplete, bool isPlayerTurn)
    {
        if (diceManager == null || diceManager.IsDiceRolling()) return;
        StartCoroutine(HandleDiceRoll(onComplete, isPlayerTurn));
    }

    private IEnumerator HandleDiceRoll(System.Action<int, int> onComplete, bool isPlayerTurn)
    {
        // Disable buttons before rolling (player turn only)
        if (currentState == GameState.PlayerTurn && isPlayerTurn && uiManager != null)
        {
            uiManager.DisableGameplayButtons();
        }

        // Let DiceManager handle the rolling and waiting
        yield return StartCoroutine(diceManager.RollDiceAndGetResults(onComplete, isPlayerTurn));

        // Re-enable buttons after rolling (player turn only)
        if (currentState == GameState.PlayerTurn && isPlayerTurn && uiManager != null)
        {
            // Only enable Stand button if player has rolled this turn
            if (player != null && player.HasRolledThisTurn())
                uiManager.EnableStandButton();
            uiManager.EnableRollButton();
        }
    }
    
    #endregion

    #region Public API

    public GameState GetCurrentState() => currentState;
    
    public bool IsPlayerTurn() => currentState == GameState.PlayerTurn;
    
    public bool IsHouseTurn() => currentState == GameState.HouseTurn;
    
    public bool IsBettingPhase() => currentState == GameState.BettingPhase;

    public bool IsDiceRolling() => diceManager != null && diceManager.IsDiceRolling();
    
    public void DisableGameplayButtons()
    {
        if (currentState == GameState.BettingPhase) return;
        
        if (uiManager != null)
            uiManager.DisableGameplayButtons();
    }
    
    #endregion
}
