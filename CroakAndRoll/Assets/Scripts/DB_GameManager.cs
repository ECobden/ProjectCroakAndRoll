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
        HeatDecision,      // Player choosing to increase heat or cash out
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
    private int heatLevel = 0;
    private const int MAX_HEAT = 8;
    
    // Lives system
    private int currentLives = 3;
    private const int MAX_LIVES = 3;
    
    // Reward system (replaces betting)
    private const int BASE_WIN_REWARD = 100;
    private const int HEAT_BONUS_MULTIPLIER = 50;
    
    // Track if player won current round
    private bool playerWonCurrentRound = false;
    
    // Track if buttons have been initialized
    private bool buttonsInitialized = false;
    
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeDice();
        InitializeUI();

        roundManager.InitializeRound();
        UpdateHeatDisplay();
        UpdateLivesDisplay();
        
        // BETTING DISABLED: Skip bet selection and start first round  
        // Call EnterState directly for initial setup (can't transition from BettingPhase to BettingPhase)
        EnterState(GameState.BettingPhase, GameState.BettingPhase);
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
                // BETTING DISABLED: Auto-start round with 0 bet
                StartRoundWithoutBetting();
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
                
            case GameState.HeatDecision:
                ShowHeatDecisionPanel();
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
        
        // Use appropriate method based on whether buttons have been initialized
        if (buttonsInitialized)
        {
            // Switch UI buttons with animation
            yield return StartCoroutine(uiManager.SwitchToGameplayButtons(
                () => { if (player != null) player.Stand(); },
                () => { if (player != null) player.RollDice(); }
            ));
        }
        else
        {
            // First time - show buttons directly
            yield return StartCoroutine(uiManager.ShowGameplayButtonsDirectly(
                () => { if (player != null) player.Stand(); },
                () => { if (player != null) player.RollDice(); }
            ));
            buttonsInitialized = true;
        }
        
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
        Debug.Log("PLAYER BUSTED!");
        
        // Player lost - no reward, no heat decision
        playerWonCurrentRound = false;
        
        if (uiManager != null)
            uiManager.ClearScoreText();
        
        TransitionToState(GameState.RoundOver);
    }

    public void HouseBust()
    {
        Debug.Log("PLAYER WINS - House busted!");
        
        // Mark that player won this round
        playerWonCurrentRound = true;
        
        if (uiManager != null)
            uiManager.ClearScoreText();
        
        TransitionToState(GameState.RoundOver);
    }
    
    public void PlayerWinsWith21()
    {
        Debug.Log("PLAYER WINS - Hit 21!");
        
        // Mark that player won this round
        playerWonCurrentRound = true;
        
        // Show player wins message
        if (uiManager != null)
        {
            uiManager.ClearScoreText();
            uiManager.ShowPlayerWins();
        }
        
        TransitionToState(GameState.RoundOver);
    }

    public void HouseWins()
    {
        Debug.Log("HOUSE WINS - House beat player's score!");
        
        // Player lost - no reward, no heat decision
        playerWonCurrentRound = false;
        
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
        // Check if player won - if so, give them heat decision choice
        if (playerWonCurrentRound)
        {
            Debug.Log("Player won! Increasing heat...");
            // Refill lives on win
            currentLives = MAX_LIVES;
            UpdateLivesDisplay();
            
            // Increment heat on win
            IncrementHeat();
            
            // Check if player reached max heat
            if (heatLevel >= MAX_HEAT)
            {
                Debug.Log($"Player reached maximum heat level {MAX_HEAT}! Must cash out.");
                // At max heat, force cash out
                OnCashOut();
                return;
            }
            
            StartCoroutine(ShowHeatDecisionAfterDelay());
            return;
        }
        
        // Player lost - decrease lives
        Debug.Log("Player lost. Decreasing lives...");
        currentLives--;
        UpdateLivesDisplay();
        
        // Check if player is out of lives
        if (currentLives <= 0)
        {
            Debug.Log("Player is out of lives! Resetting heat and lives...");
            // Reset heat to 0 when player loses all lives
            heatLevel = 0;
            UpdateHeatDisplay();
            // Reset lives back to max
            currentLives = MAX_LIVES;
            UpdateLivesDisplay();
        }
        
        // Start new round after delay
        Debug.Log($"Lives remaining: {currentLives}. Starting new round...");
        StartCoroutine(StartNewRoundAfterDelay());
    }
    
    #endregion

    #region Betting (Currently Disabled)

    private void StartRoundWithoutBetting()
    {
        Debug.Log("Starting round without betting (free play)");
        
        // Update goal text
        if (uiManager != null)
        {
            uiManager.UpdateGoalText("Get Ready!");
        }
        
        // Start player's turn with 0 bet
        if (player != null)
            player.OnTurnStart(0);
        
        // Transition to PlayerTurn (will show gameplay buttons automatically)
        TransitionToState(GameState.PlayerTurn);
    }

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
    
    private IEnumerator ShowHeatDecisionAfterDelay()
    {
        yield return new WaitForSeconds(newRoundDelay);
        TransitionToState(GameState.HeatDecision);
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
        
        // Reinitialize and start round without betting
        roundManager.InitializeRound();
        // Call EnterState directly since we just reset to BettingPhase
        EnterState(GameState.BettingPhase, GameState.GameOver);
    }

    private void ResetGameState()
    {
        currentState = GameState.BettingPhase;
        heatLevel = 0;
        currentLives = MAX_LIVES;
        buttonsInitialized = false;
        
        if (uiManager != null)
        {
            uiManager.HideGameOverPanel();
            uiManager.DeactivateButtons();
        }
        
        UpdateHeatDisplay();
        UpdateLivesDisplay();
    }

    private void ResetPlayers()
    {
        if (player != null)
        {
            player.ResetMoney();
            player.ClearPerks();
        }

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

    #region Heat Decision

    private void ShowHeatDecisionPanel()
    {
        int potentialReward = CalculateWinReward();
        int nextHeatReward = CalculateNextHeatReward();
        
        if (uiManager != null)
        {
            uiManager.ShowHeatDecisionPanel(
                heatLevel, 
                potentialReward, 
                nextHeatReward,
                OnIncreaseHeat, 
                OnCashOut
            );
        }
    }

    public void OnIncreaseHeat()
    {
        Debug.Log($"Player chose to continue playing at heat {heatLevel}!");
        
        if (uiManager != null)
            uiManager.HideHeatDecisionPanel();
        
        // Reset win flag and start new round
        playerWonCurrentRound = false;
        OnStartNewRound();
    }

    public void OnCashOut()
    {
        int reward = CalculateWinReward();
        Debug.Log($"Player cashed out! Reward: {reward}");
        
        if (uiManager != null)
            uiManager.HideHeatDecisionPanel();
        
        // Give player their reward
        if (player != null)
        {
            player.AddMoney(reward);
            Debug.Log($"Player earned {reward} (Base: {BASE_WIN_REWARD}, Heat Bonus: {heatLevel * HEAT_BONUS_MULTIPLIER})");
        }
        
        // Reset win flag
        playerWonCurrentRound = false;
        
        // Open shop
        OpenShop();
    }

    private void OpenShop()
    {
        Debug.Log("Opening shop...");
        
        if (uiManager != null && player != null)
        {
            uiManager.ShowShop(player, OnCloseShop);
        }
    }

    public void OnCloseShop()
    {
        Debug.Log("Shop closed, starting new round...");
        
        if (uiManager != null)
            uiManager.HideShop();
        
        // Start new round
        OnStartNewRound();
    }

    #endregion

    #region Reward System

    private int CalculateWinReward()
    {
        int baseReward = BASE_WIN_REWARD;
        int heatBonus = heatLevel * HEAT_BONUS_MULTIPLIER;
        return baseReward + heatBonus;
    }
    
    private int CalculateNextHeatReward()
    {
        int baseReward = BASE_WIN_REWARD;
        int nextHeatBonus = (heatLevel + 1) * HEAT_BONUS_MULTIPLIER;
        return baseReward + nextHeatBonus;
    }

    #endregion

    #region Heat Management

    private void IncrementHeat()
    {
        heatLevel++;
        Debug.Log($"Heat level increased to {heatLevel}");
        UpdateHeatDisplay();
        
        // Check for victory at heat 8
        if (heatLevel >= MAX_HEAT)
        {
            Debug.Log($"Player reached Heat {MAX_HEAT}! Victory condition met!");
        }
    }

    private void UpdateHeatDisplay()
    {
        if (uiManager != null)
        {
            uiManager.UpdateHeatDisplay(heatLevel, MAX_HEAT);
        }
    }
    
    private void UpdateLivesDisplay()
    {
        if (uiManager != null)
        {
            uiManager.UpdateLivesDisplay(currentLives, MAX_LIVES);
        }
    }
    
    public int GetHeatLevel() => heatLevel;
    
    public int GetCurrentLives() => currentLives;

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
