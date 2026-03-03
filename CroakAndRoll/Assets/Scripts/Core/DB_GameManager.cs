using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

/// <summary>
///GameManager operates as a State Machine.
///It will control the main loop.
///It will manage the turn order, the player will always go first.
///It will track the roll limits, allowing up to 5 rolls each per round.
///It will constantly check for win conditions, such as determining if a side hits exactly 21 at the end of both sides' turns , or evaluating the highest score without busting at the end of the 5 rolls or when both sides stand.
/// </summary>
public class DB_GameManager : MonoBehaviour
{
    #region Enums
    
    public enum GameState
    {
        PlayRound,          // Both players alternating turns, rolling dice
        RoundOver,         // Round ending, determining winner
        GameOver           // Game completely over
    }
    
    public enum TurnMode
    {
        PlayerTurn,           // Player's turn to roll
        HouseTurn,            // House's turn to roll (alternating)
    }
    
    /// <summary>
    /// Represents a single roll in the round history
    /// </summary>
    [System.Serializable]
    public class RollRow
    {
        public int diceA;
        public int diceB;
        public int rollTotal;
        
        public RollRow(int a, int b)
        {
            diceA = a;
            diceB = b;
            rollTotal = a + b;
        }
    }
    
    /// <summary>
    /// Result of checking round win conditions
    /// </summary>
    public enum RoundResult
    {
        None,
        PlayerWins,
        HouseWins,
        Continue
    }
    
    #endregion

    #region Serialized Fields

    [Header("Player References")]
    [SerializeField] private Player player;
    [SerializeField] private House house;

    [Header("Managers")]
    [SerializeField] private DB_DiceManager diceManager;
    [SerializeField] private DB_UIManager uiManager;
    [SerializeField] private ShopManager shopManager;

    [Header("Game Settings")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private float newRoundDelay = 1.5f;
    [SerializeField] private int baseRoundWinReward = 100;
    [SerializeField] private int gameSeed = 12345;
    
    #endregion

    #region Private Fields
    
    private GameState currentState = GameState.RoundOver;

    // Round counter
    private int currentRound = 1;
    
    // Game state tracking
    private bool playerWonCurrentRound = false;
    private bool roundWasTie = false;
    private bool buttonsInitialized = false;
    private int pendingRoundReward = 0;
    
    // Rule decision system
    private bool isWaitingForPlayerRuleDecision = false;
    private List<int> currentMatchingDice = new List<int>();
    private List<int> currentSwappableDice = new List<int>();
    private List<DB_DiceController> diceBeingFlipped = new List<DB_DiceController>();
    
    // Turn System State (condensed)
    private TurnMode currentTurnMode = TurnMode.PlayerTurn;
    private bool playerHasAdvantage = true;        // Who gets first turn at round start
    
    // Round Tracking
    private int playerRoundTotal = 0;              // Player's total score this round
    private int houseRoundTotal = 0;               // House's total score this round
    private List<RollRow> playerRollRows = new List<RollRow>();  // Player's roll history
    private List<RollRow> houseRollRows = new List<RollRow>();   // House's roll history
    
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // One-time initialization only
        InitializeDice();
        InitializeUI();
        
        // Start first round (round counter starts at 1)
        StartRoundOne();
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
        {
            // Set up score change callbacks
            var playerPos = diceManager.GetPlayerScoringPositioner();
            var housePos = diceManager.GetHouseScoringPositioner();
            
            if (playerPos != null)
                playerPos.SetScoreChangedCallback(UpdatePlayerScoreDisplay);
            if (housePos != null)
                housePos.SetScoreChangedCallback(UpdateHouseScoreDisplay);
        }
    }

    private void InitializeUI()
    {
        if (uiManager != null)
            uiManager.Initialize(RestartGame);
    }
    
    /// <summary>
    /// Update player score display from current dice positions
    /// </summary>
    private void UpdatePlayerScoreDisplay()
    {
        if (diceManager == null || uiManager == null) return;
        
        var playerPos = diceManager.GetPlayerScoringPositioner();
        if (playerPos != null)
        {
            int actualTotal = playerPos.GetTotalScore();
            playerRoundTotal = actualTotal; // Sync cached value with actual
            uiManager.UpdatePlayerRoundTotal(actualTotal);
            Debug.Log($"Player score updated: {actualTotal}");
        }
    }
    
    /// <summary>
    /// Update house score display from current dice positions
    /// </summary>
    private void UpdateHouseScoreDisplay()
    {
        if (diceManager == null || uiManager == null) return;
        
        var housePos = diceManager.GetHouseScoringPositioner();
        if (housePos != null)
        {
            int actualTotal = housePos.GetTotalScore();
            houseRoundTotal = actualTotal; // Sync cached value with actual
            uiManager.UpdateHouseRoundTotal(actualTotal);
            Debug.Log($"House score updated: {actualTotal}");
        }
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
            case GameState.PlayRound:
                StartTurnSystemForRound();
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
    
    #endregion

    #region Game Outcomes

    public void PlayerBust()
    {
        Debug.Log("PLAYER BUSTED!");

        if (uiManager != null)
            uiManager.ShowPlayerBust();

        EndRound(playerWon: false);
    }

    public void HouseBust()
    {
        Debug.Log("PLAYER WINS - House busted!");

        if (uiManager != null)
            uiManager.ShowHouseBust();

        EndRound(playerWon: true);
    }
    
    public void PlayerWinsWith21()
    {
        Debug.Log("PLAYER WINS - Hit 21!");
        
        if (uiManager != null)
            uiManager.ShowPlayer21();
        
        EndRound(playerWon: true);
    }

    private void HouseWinsWith21()
    {
        Debug.Log("HOUSE WINS - Hit 21!");

        if (uiManager != null)
            uiManager.ShowHouse21();

        EndRound(playerWon: false);
    }

    public void HouseWins()
    {
        Debug.Log("HOUSE WINS - House beat player's score!");

        if (uiManager != null)
            uiManager.ShowHouseWins();

        EndRound(playerWon: false);
    }

    private void EndRound(bool playerWon)
    {
        playerWonCurrentRound = playerWon;
        roundWasTie = false;

        pendingRoundReward = playerWon && baseRoundWinReward > 0 ? baseRoundWinReward : 0;
        
        if (uiManager != null)
            uiManager.ClearRollScoreText();
        
        TransitionToState(GameState.RoundOver);
    }

    private void HandleRoundOver()
    {
        if (roundWasTie)
            Debug.Log("Round tied. Starting new round...");
        else if (playerWonCurrentRound)
            Debug.Log("Player won! Starting new round...");
        else
            Debug.Log("Player lost. Starting new round...");

        StartCoroutine(HandleRoundOverSequence());
    }

    private IEnumerator HandleRoundOverSequence()
    {
        if (pendingRoundReward > 0 && player != null)
        {
            player.AddMoney(pendingRoundReward);
            pendingRoundReward = 0;
            yield return null;
        }

        if (diceManager != null)
            diceManager.ClearScoredDice();
        
        // Open shop between rounds if available
        if (shopManager != null)
        {
            shopManager.OpenShop(gameSeed + currentRound);
        }
        else
        {
            // No shop available, proceed directly to next round
            StartCoroutine(StartNewRoundAfterDelay());
        }
    }

    /// <summary>
    /// Called by ShopManager when the player closes the shop.
    /// </summary>
    public void OnShopClosed()
    {
        StartCoroutine(StartNewRoundAfterDelay());
    }
    
    #endregion

    #region Round Management

    private void StartRoundOne()
    {
        Debug.Log("=== Starting New Round ===");
        
        // 1. Clear previous round data
        ClearRoundData();

        // 2. Reset participant and round state
        if (player != null)
        {
            player.ResetRound();
            player.OnRoundStart();
        }
        if (house != null)
        {
            house.ResetRound();
            house.OnRoundStart();
        }

        playerRoundTotal = 0;
        houseRoundTotal = 0;
        playerRollRows.Clear();
        houseRollRows.Clear();
        currentTurnMode = TurnMode.PlayerTurn;
        playerHasAdvantage = true;
        roundWasTie = false;
        
        // 3. Update round UI
        UpdateRoundUI();
        
        // 4. Prepare UI for new round
        PrepareRoundUI();
        
        // 5. Start the alternating turn system
        TransitionToState(GameState.PlayRound);
    }

    private void ClearRoundData()
    {
        // Clear scored dice from previous round
        if (diceManager != null)
            diceManager.ClearScoredDice();
        
        // Clear rule decision state
        isWaitingForPlayerRuleDecision = false;
        currentMatchingDice.Clear();
        currentSwappableDice.Clear();
        diceBeingFlipped.Clear();
    }
    
    private void UpdateRoundUI()
    {
        // Update round counter display (only increments after first round)
        if (uiManager != null)
        {
            uiManager.UpdateRoundDisplay(currentRound);
            uiManager.ShowRoundCounter();
        }
    }
    
    private void PrepareRoundUI()
    {
        if (uiManager == null) return;
        
        // Clear all UI from previous round
        uiManager.ClearRoundTotals();
        uiManager.ClearRollScoreText();
        uiManager.HideStandValue();
        uiManager.ResetGoalRollProgress();
    }

    public void OnStartNewRound()
    {
        Debug.Log("[NEW ROUND] Starting next round");
        
        // Increment round counter
        currentRound++;
        StartRoundOne();
    }

    private IEnumerator StartNewRoundAfterDelay()
    {
        yield return new WaitForSeconds(newRoundDelay);
        OnStartNewRound();
    }
    
    #endregion

    #region Turn Management

    private void StartTurnSystemForRound()
    {
        currentTurnMode = playerHasAdvantage ? TurnMode.PlayerTurn : TurnMode.HouseTurn;
        BeginTurn();
    }

    private void BeginTurn()
    {
        if (currentState != GameState.PlayRound)
            return;

        if (BothParticipantsStood())
        {
            ResolveRoundByTotals();
            return;
        }

        bool isPlayerTurn = currentTurnMode == TurnMode.PlayerTurn;
        if (isPlayerTurn)
        {
            if (player != null && player.HasStood())
            {
                currentTurnMode = TurnMode.HouseTurn;
                BeginTurn();
                return;
            }
        }
        else
        {
            if (house != null && house.HasStood())
            {
                currentTurnMode = TurnMode.PlayerTurn;
                BeginTurn();
                return;
            }
        }

        SetTurnActive(isPlayerTurn, !isPlayerTurn);

        if (uiManager != null)
        {
            if (isPlayerTurn)
                uiManager.SetTurnMarkerToPlayer();
            else
                uiManager.SetTurnMarkerToHouse();
        }

        if (isPlayerTurn)
        {
            if (uiManager != null)
            {
                if (!buttonsInitialized)
                {
                    StartCoroutine(uiManager.ShowGameplayButtonsDirectly(OnPlayerStandPressed, OnPlayerRollPressed));
                    buttonsInitialized = true;
                }
                else
                {
                    uiManager.DisableGameplayButtons();
                    uiManager.EnableRollButton();

                    if (playerRollRows.Count > 0)
                        uiManager.EnableStandButton();
                }
            }
        }
        else
        {
            if (uiManager != null)
                uiManager.DisableGameplayButtons();

            if (house != null)
                house.BeginTurn();
        }
    }

    private void AdvanceTurn()
    {
        if (currentState != GameState.PlayRound)
            return;

        if (BothParticipantsStood())
        {
            ResolveRoundByTotals();
            return;
        }

        currentTurnMode = currentTurnMode == TurnMode.PlayerTurn ? TurnMode.HouseTurn : TurnMode.PlayerTurn;

        if (currentTurnMode == TurnMode.PlayerTurn && player != null && player.HasStood())
            currentTurnMode = TurnMode.HouseTurn;
        else if (currentTurnMode == TurnMode.HouseTurn && house != null && house.HasStood())
            currentTurnMode = TurnMode.PlayerTurn;

        if (BothParticipantsStood())
        {
            ResolveRoundByTotals();
            return;
        }

        BeginTurn();
    }

    private void SetTurnActive(bool playerActive, bool houseActive)
    {
        if (player != null)
            player.SetTurnActive(playerActive);
        if (house != null)
            house.SetTurnActive(houseActive);
    }

    private bool BothParticipantsStood()
    {
        bool playerStood = player != null && player.HasStood();
        bool houseStood = house != null && house.HasStood();
        return playerStood && houseStood;
    }

    private void ResolveRoundByTotals()
    {
        int playerTotal = playerRoundTotal;
        int houseTotal = houseRoundTotal;

        if (playerTotal > 21 && houseTotal > 21)
        {
            EndRoundTie();
            return;
        }

        if (playerTotal > 21)
        {
            if (uiManager != null)
                uiManager.ShowHouseWins();

            EndRound(playerWon: false);
            return;
        }

        if (houseTotal > 21)
        {
            if (uiManager != null)
                uiManager.ShowPlayerWins();

            EndRound(playerWon: true);
            return;
        }

        if (playerTotal > houseTotal)
        {
            if (uiManager != null)
                uiManager.ShowPlayerWins();

            EndRound(playerWon: true);
        }
        else if (houseTotal > playerTotal)
        {
            if (uiManager != null)
                uiManager.ShowHouseWins();

            EndRound(playerWon: false);
        }
        else
        {
            EndRoundTie();
        }
    }

    private void EndRoundTie()
    {
        playerWonCurrentRound = false;
        roundWasTie = true;
        pendingRoundReward = 0;

        if (uiManager != null)
            uiManager.ClearRollScoreText();

        TransitionToState(GameState.RoundOver);
    }

    private bool CheckImmediateWinOrBust(bool isPlayer)
    {
        int total = isPlayer ? playerRoundTotal : houseRoundTotal;

        if (total > 21)
        {
            if (isPlayer)
                PlayerBust();
            else
                HouseBust();

            return true;
        }

        if (total == 21)
        {
            if (isPlayer)
                PlayerWinsWith21();
            else
                HouseWinsWith21();

            return true;
        }

        return false;
    }

    private void OnPlayerRollPressed()
    {
        if (currentState != GameState.PlayRound)
            return;
        if (currentTurnMode != TurnMode.PlayerTurn)
            return;

        if (uiManager != null)
            uiManager.DisableGameplayButtons();

        if (player != null)
            player.RollDice();
    }

    private void OnPlayerStandPressed()
    {
        if (currentState != GameState.PlayRound)
            return;
        if (currentTurnMode != TurnMode.PlayerTurn)
            return;

        if (uiManager != null)
            uiManager.DisableGameplayButtons();

        if (player != null)
            player.Stand();
    }

    public Participant GetPlayerParticipant()
    {
        return player;
    }

    public Participant GetHouseParticipant()
    {
        return house;
    }

    public void OnParticipantRolled(bool isPlayer, int diceAValue, int diceBValue)
    {
        if (currentState != GameState.PlayRound)
            return;

        int rollTotal = diceAValue + diceBValue;

        if (isPlayer)
        {
            // Don't add to playerRoundTotal here - it's handled by UpdatePlayerScoreDisplay via callback
            playerRollRows.Add(new RollRow(diceAValue, diceBValue));

            if (uiManager != null)
            {
                uiManager.EnableStandButton();
            }
        }
        else
        {
            // Don't add to houseRoundTotal here - it's handled by UpdateHouseScoreDisplay via callback
            houseRollRows.Add(new RollRow(diceAValue, diceBValue));
        }

        if (CheckImmediateWinOrBust(isPlayer))
            return;

        AdvanceTurn();
    }

    public void OnParticipantStood(bool isPlayer)
    {
        if (currentState != GameState.PlayRound)
            return;

        UpdateStandValueDisplay();

        if (BothParticipantsStood())
        {
            ResolveRoundByTotals();
            return;
        }

        AdvanceTurn();
    }

    private void UpdateStandValueDisplay()
    {
        if (uiManager == null)
            return;

        bool playerStood = player != null && player.HasStood();
        bool houseStood = house != null && house.HasStood();

        if (!playerStood && !houseStood)
            return;

        string playerText = playerStood ? $"Player: {playerRoundTotal}" : "Player: --";
        string houseText = houseStood ? $"House: {houseRoundTotal}" : "House: --";
        uiManager.ShowStandValue($"{playerText}  |  {houseText}");
    }

    #endregion

    #region Game Control

    public void RestartGame()
    {
        Debug.Log("=== Restarting Game ===");
        
        // Reset game-level state
        ResetGameState();
        
        // Reset player/house money and stats
        ResetPlayers();
        
        // Reset round counter to 1
        currentRound = 1;
        UpdateRoundUI();
    }

    private void ResetGameState()
    {
        // Reset state machine
        currentState = GameState.RoundOver;
        buttonsInitialized = false;
        
        // Hide game over UI
        if (uiManager != null)
        {
            uiManager.HideGameOverPanel();
            uiManager.DeactivateButtons();
        }
    }

    private void ResetPlayers()
    {
        if (player != null)
        {
            player.ResetMoney();
        }

        if (house != null)
        {
            house.ResetMoney();
            house.ResetTurnValue();
        }
    }
    
    #endregion

    #region Public API

    // State queries
    public GameState GetCurrentState() => currentState;
    public bool IsDiceRolling() => diceManager != null && diceManager.IsDiceRolling();
    public bool IsWaitingForPlayerRuleDecision() => isWaitingForPlayerRuleDecision;
    
    // UI control
    public void DisableGameplayButtons()
    {
        if (uiManager != null)
            uiManager.DisableGameplayButtons();
    }
    
    // Alternating turn system data - direct accessors
    public int GetPlayerRoundTotal() => playerRoundTotal;
    public int GetHouseRoundTotal() => houseRoundTotal;
    public bool PlayerHasAdvantage() => playerHasAdvantage;
    public TurnMode GetCurrentTurnMode() => currentTurnMode;
    public bool IsPlayerTurn() => currentTurnMode == TurnMode.PlayerTurn;
    public List<RollRow> GetPlayerRollRows() => playerRollRows;
    public List<RollRow> GetHouseRollRows() => houseRollRows;
    
    #endregion
}
