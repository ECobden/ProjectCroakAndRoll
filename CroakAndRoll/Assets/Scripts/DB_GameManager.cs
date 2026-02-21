using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

/// <summary>
/// Core game manager for Croak and Roll.
/// Handles game state transitions, dice rolling coordination, win/loss conditions, 
/// and alternating turn system (advantage, turn switching, equal opportunity).
/// 
/// Game Structure:
/// - Game contains multiple rounds
/// - Each round has alternating turns between player and house
/// - Each turn involves rolling dice and taking actions
/// </summary>
public class DB_GameManager : MonoBehaviour
{
    #region Enums
    
    public enum GameState
    {
        AlternatingTurns,  // Both players alternating rolls
        PlayerStanding,    // Player has stood, transitioning to house solo
        RoundOver,         // Round ending, determining winner
        GameOver           // Game completely over
    }
    
    public enum TurnMode
    {
        PlayerTurn,           // Player's turn to roll
        HouseTurn,            // House's turn to roll (alternating)
        HouseSolo,            // House rolling solo after player stood
        WaitingForEquality    // Opponent getting equal opportunity after win/bust
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
    [SerializeField] private DB_RoundManager roundManager;
    [SerializeField] private DB_DiceManager diceManager;
    [SerializeField] private DB_UIManager uiManager;
    [SerializeField] private DB_DiceRuleSystem ruleSystem;

    [Header("Game Settings")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private float newRoundDelay = 1.5f;
    
    #endregion

    #region Private Fields
    
    private GameState currentState = GameState.RoundOver;
    
    // Game state tracking
    private bool playerWonCurrentRound = false;
    private bool buttonsInitialized = false;
    
    // Rule decision system
    private bool isWaitingForPlayerRuleDecision = false;
    private List<int> currentMatchingDice = new List<int>();
    private List<int> currentSwappableDice = new List<int>();
    private List<DB_DiceController> diceBeingFlipped = new List<DB_DiceController>();
    
    // Turn System State (condensed)
    private TurnMode currentTurnMode = TurnMode.PlayerTurn;
    private bool playerHasAdvantage = true;        // Who gets first turn at round start
    private bool isWaitingForHouseRoll = false;    // Prevents duplicate house roll triggers
    
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
        StartNewRoundInternal();
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
            UpdateRoundTotals();
            uiManager.UpdatePlayerRoundTotal(playerRoundTotal);
            Debug.Log($"Player score updated: {playerRoundTotal}");
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
            UpdateRoundTotals();
            uiManager.UpdateHouseRoundTotal(houseRoundTotal);
            Debug.Log($"House score updated: {houseRoundTotal}");
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
            case GameState.AlternatingTurns:
                StartAlternatingTurns();
                break;
                
            case GameState.PlayerStanding:
                SetPlayerStood();
                StartCoroutine(ContinueHouseSolo());
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
        EndRound(playerWon: false);
    }

    public void HouseBust()
    {
        Debug.Log("PLAYER WINS - House busted!");
        EndRound(playerWon: true);
    }
    
    public void PlayerWinsWith21()
    {
        Debug.Log("PLAYER WINS - Hit 21!");
        
        if (uiManager != null)
            uiManager.ShowPlayerWins();
        
        EndRound(playerWon: true);
    }

    public void HouseWins()
    {
        Debug.Log("HOUSE WINS - House beat player's score!");
        EndRound(playerWon: false);
    }

    public void PlayerOutOfMoney()
    {
        Debug.Log("GAME OVER - Player is out of money!");
        TransitionToState(GameState.GameOver);
    }

    private void EndRound(bool playerWon)
    {
        playerWonCurrentRound = playerWon;
        
        if (uiManager != null)
            uiManager.ClearScoreText();
        
        TransitionToState(GameState.RoundOver);
    }

    private void HandleRoundOver()
    {
        if (playerWonCurrentRound)
            Debug.Log("Player won! Starting new round...");
        else
            Debug.Log("Player lost. Starting new round...");
        
        StartCoroutine(StartNewRoundAfterDelay());
    }
    
    #endregion

    #region Round Management

    private void StartNewRoundInternal()
    {
        Debug.Log("=== Starting New Round ===");
        
        // 1. Clear previous round data
        ClearRoundData();
        
        // 2. Update round UI
        UpdateRoundUI();
        
        // 3. Prepare UI for new round
        PrepareRoundUI();
        
        // 4. Initialize round through alternating round manager
        InitializeRound();
        
        // 5. Start the alternating turn system
        TransitionToState(GameState.AlternatingTurns);
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
        if (roundManager != null)
            roundManager.InitializeRound();
    }
    
    private void PrepareRoundUI()
    {
        if (uiManager == null) return;
        
        // Clear all UI from previous round
        uiManager.ClearRoundTotals();
        uiManager.ClearScoreText();
        uiManager.HideStandValue();
        uiManager.ResetGoalRollProgress();
    }

    public void OnStartNewRound()
    {
        Debug.Log("[NEW ROUND] Starting next round");
        
        // Increment round counter
        if (roundManager != null)
            roundManager.CountUpRound();
        
        // Start new round (all UI clearing happens in StartNewRoundInternal)
        StartNewRoundInternal();
    }

    private IEnumerator StartNewRoundAfterDelay()
    {
        yield return new WaitForSeconds(newRoundDelay);
        OnStartNewRound();
    }
    
    #endregion

    #region Alternating Turn System
    
    // ========== Core Round Management (Merged from DB_AlternatingRoundManager) ==========
    
    /// <summary>
    /// Initialize a new round - resets all state and determines advantage
    /// </summary>
    private void InitializeRound()
    {
        Debug.Log("=== AlternatingRoundManager: Initializing Round ===");
        
        // Reset all state
        ResetRoundState();
        
        // Determine who goes first
        DetermineAdvantage();
        
        // Initialize player/house for new round
        InitializePlayers();
    }
    
    /// <summary>
    /// Reset all round state variables
    /// </summary>
    private void ResetRoundState()
    {
        playerRollRows.Clear();
        houseRollRows.Clear();
        playerRoundTotal = 0;
        houseRoundTotal = 0;
        isWaitingForHouseRoll = false;
        
        // Note: playerHasAdvantage and currentTurnMode will be set by DetermineAdvantage()
    }
    
    /// <summary>
    /// Determine who has advantage this round (goes first)
    /// </summary>
    private void DetermineAdvantage()
    {
        playerHasAdvantage = Random.value < 0.5f;
        currentTurnMode = playerHasAdvantage ? TurnMode.PlayerTurn : TurnMode.HouseTurn;
        
        Debug.Log($"[ADVANTAGE] {(playerHasAdvantage ? "PLAYER" : "HOUSE")} gets first turn");
        
        if (uiManager != null)
        {
            string advantageText = playerHasAdvantage ? "You have advantage!" : "House has advantage!";
            uiManager.UpdateGoalText(advantageText);
        }
    }
    
    /// <summary>
    /// Initialize players for the round
    /// </summary>
    private void InitializePlayers()
    {
        if (player != null)
            player.OnRoundStart(0);
        if (house != null)
            house.OnRoundStart();
    }
    
    /// <summary>
    /// Prepare UI for alternating turns at round start
    /// </summary>
    private void PrepareAlternatingTurnsUI()
    {
        if (uiManager == null) return;
        
        bool isPlayerTurn = (currentTurnMode == TurnMode.PlayerTurn);
        Debug.Log($"[TURN START] {(isPlayerTurn ? "Player" : "House")}'s turn begins");
        
        // Update UI based on whose turn it is
        if (isPlayerTurn)
        {
            uiManager.SetTurnMarkerToPlayer();
            uiManager.EnableRollButton();
            uiManager.UpdateGoalText("Your turn - Roll closest to 21");
        }
        else
        {
            uiManager.SetTurnMarkerToHouse();
            uiManager.DisableGameplayButtons();
            uiManager.UpdateGoalText("House's turn");
        }
    }
    
    /// <summary>
    /// Add a roll to the current roller's history
    /// </summary>
    private void AddRoll(int diceA, int diceB, bool isPlayer)
    {
        if (isPlayer)
        {
            playerRollRows.Add(new RollRow(diceA, diceB));
            
            // Enable Stand button after first roll
            if (uiManager != null && playerRollRows.Count == 1)
            {
                uiManager.EnableStandButton();
            }
        }
        else
        {
            houseRollRows.Add(new RollRow(diceA, diceB));
        }
    }
    
    /// <summary>
    /// Update round totals from dice manager
    /// </summary>
    private void UpdateRoundTotals()
    {
        if (diceManager == null) return;
        
        var playerPos = diceManager.GetPlayerScoringPositioner();
        var housePos = diceManager.GetHouseScoringPositioner();
        
        if (playerPos != null)
        {
            playerRoundTotal = playerPos.GetTotalScore();
        }
        
        if (housePos != null)
        {
            houseRoundTotal = housePos.GetTotalScore();
        }
    }
    
    /// <summary>
    /// Switch to the other player's turn
    /// </summary>
    private void SwitchTurn()
    {
        // Toggle between player and house turns
        currentTurnMode = (currentTurnMode == TurnMode.PlayerTurn) ? TurnMode.HouseTurn : TurnMode.PlayerTurn;
        
        if (uiManager == null) return;
        
        bool isPlayerTurn = (currentTurnMode == TurnMode.PlayerTurn);
        Debug.Log($"[TURN SWITCH] {(isPlayerTurn ? "Player" : "House")}'s turn begins");
        
        if (isPlayerTurn)
        {
            uiManager.SetTurnMarkerToPlayer();
            uiManager.EnableRollButton();
            
            // Enable stand button if player has already rolled
            if (playerRollRows.Count > 0)
            {
                uiManager.EnableStandButton();
            }
            
            uiManager.UpdateGoalText("Your turn - Roll closest to 21");
        }
        else
        {
            uiManager.SetTurnMarkerToHouse();
            uiManager.DisableGameplayButtons();
            uiManager.UpdateGoalText("House's turn");
        }
    }
    
    /// <summary>
    /// Mark that player has stood - house continues solo
    /// </summary>
    private void SetPlayerStood()
    {
        currentTurnMode = TurnMode.HouseSolo;
        Debug.Log("Player has stood - house continues solo");
    }
    
    /// <summary>
    /// Start waiting for house roll (prevents duplicate triggers)
    /// </summary>
    private IEnumerator WaitForHouseRoll(float delay = 1f)
    {
        if (isWaitingForHouseRoll)
        {
            Debug.Log("Already waiting for house roll, skipping duplicate trigger");
            yield break;
        }
        
        isWaitingForHouseRoll = true;
        yield return new WaitForSeconds(delay);
        isWaitingForHouseRoll = false;
        
        if (house != null)
            house.RollDice();
    }
    
    /// <summary>
    /// Check if the round should end based on current totals and equal opportunity
    /// </summary>
    private RoundResult CheckRoundResult(bool isPlayer)
    {
        int currentTotal = isPlayer ? playerRoundTotal : houseRoundTotal;
        int currentRollCount = isPlayer ? playerRollRows.Count : houseRollRows.Count;
        int opponentRollCount = isPlayer ? houseRollRows.Count : playerRollRows.Count;
        bool inHouseSolo = (currentTurnMode == TurnMode.HouseSolo);
        
        // Check for bust
        if (currentTotal > 21)
        {
            Debug.Log($"{(isPlayer ? "Player" : "House")} BUSTED! Rolls: {currentRollCount} vs {opponentRollCount}");
            
            // Check if opponent needs equal opportunity (only in alternating mode)
            if (opponentRollCount < currentRollCount && !inHouseSolo)
            {
                Debug.Log($"{(isPlayer ? "House" : "Player")} needs equal opportunity - giving another turn");
                currentTurnMode = TurnMode.WaitingForEquality;
                return RoundResult.Continue;
            }
            
            // Round ends - opponent wins (or house wins if both bust)
            return isPlayer ? RoundResult.HouseWins : RoundResult.PlayerWins;
        }
        
        // Check for 21
        if (currentTotal == 21)
        {
            Debug.Log($"{(isPlayer ? "Player" : "House")} hit 21! Rolls: {currentRollCount} vs {opponentRollCount}");
            
            // Check if opponent needs equal opportunity (only in alternating mode)
            if (opponentRollCount < currentRollCount && !inHouseSolo)
            {
                Debug.Log($"{(isPlayer ? "House" : "Player")} needs equal opportunity - giving another turn");
                currentTurnMode = TurnMode.WaitingForEquality;
                return RoundResult.Continue;
            }
            
            // Round ends - current player wins
            return isPlayer ? RoundResult.PlayerWins : RoundResult.HouseWins;
        }
        
        // Check if equal opportunity has been satisfied
        if (currentTurnMode == TurnMode.WaitingForEquality)
        {
            bool equalityAchieved = isPlayer ? 
                (playerRollRows.Count >= houseRollRows.Count) : 
                (houseRollRows.Count >= playerRollRows.Count);
            
            if (equalityAchieved)
            {
                Debug.Log("Equal opportunity achieved - checking final conditions");
                
                // Determine winner after equal opportunity
                if (houseRoundTotal > 21 && playerRoundTotal > 21)
                {
                    Debug.Log($"Both bust - House: {houseRoundTotal}, Player: {playerRoundTotal}");
                    return RoundResult.HouseWins; // House wins by default if both bust
                }
                else if (houseRoundTotal > 21)
                {
                    Debug.Log($"House confirmed bust: {houseRoundTotal}, Player: {playerRoundTotal}");
                    return RoundResult.PlayerWins;
                }
                else if (playerRoundTotal > 21)
                {
                    Debug.Log($"Player confirmed bust: {playerRoundTotal}, House: {houseRoundTotal}");
                    return RoundResult.HouseWins;
                }
                else if (playerRoundTotal == 21)
                {
                    Debug.Log($"Player confirmed 21: {playerRoundTotal}");
                    return RoundResult.PlayerWins;
                }
                else if (houseRoundTotal == 21)
                {
                    Debug.Log($"House confirmed 21: {houseRoundTotal}");
                    return RoundResult.HouseWins;
                }
            }
        }
        
        // Check if player wins because house is bust after player stood
        if (inHouseSolo && houseRoundTotal > 21 && playerRoundTotal <= 21)
        {
            Debug.Log($"[PLAYER WINS] House is bust ({houseRoundTotal}), Player stood at {playerRoundTotal}");
            return RoundResult.PlayerWins;
        }
        
        // Check if house has won after player stood
        if (inHouseSolo && !isPlayer && houseRoundTotal <= 21 && playerRoundTotal <= 21)
        {
            if (houseRoundTotal >= playerRoundTotal)
            {
                Debug.Log($"House wins with {houseRoundTotal} vs Player's {playerRoundTotal}");
                return RoundResult.HouseWins;
            }
        }
        
        return RoundResult.Continue;
    }
    
    // ========== Alternating Turn Flow ==========

    private void StartAlternatingTurns()
    {
        // Initialize buttons and prepare UI in proper sequence
        if (!buttonsInitialized)
        {
            StartCoroutine(InitializeAlternatingTurnsUI());
            buttonsInitialized = true;
        }
        else
        {
            // Buttons already initialized, just prepare UI for this round
            PrepareAlternatingTurnsUI();
            
            // If house has advantage, trigger house roll
            if (currentTurnMode == TurnMode.HouseTurn)
            {
                StartCoroutine(WaitForHouseRoll());
            }
        }
    }
    
    private IEnumerator InitializeAlternatingTurnsUI()
    {
        if (uiManager == null) yield break;
        
        // Show and activate buttons first
        yield return StartCoroutine(uiManager.ShowGameplayButtonsDirectly(
            () => { if (player != null) player.Stand(); },
            () => { if (player != null) player.RollDice(); }
        ));
        
        // THEN prepare UI based on who has advantage (after buttons are shown)
        PrepareAlternatingTurnsUI();
        
        // If house has advantage, trigger house roll
        if (currentTurnMode == TurnMode.HouseTurn)
        {
            StartCoroutine(WaitForHouseRoll());
        }
    }

    public void OnAlternatingRoll(int diceA, int diceB, bool isPlayer)
    {
        Debug.Log($"[ROLL] {(isPlayer ? "Player" : "House")} rolled: {diceA} + {diceB} = {diceA + diceB}");
        
        // Start coroutine to handle roll with rule checks
        StartCoroutine(ProcessAlternatingRollWithRules(diceA, diceB, isPlayer));
    }
    
    private IEnumerator ProcessAlternatingRollWithRules(int diceA, int diceB, bool isPlayer)
    {
        // Add roll to round manager
        AddRoll(diceA, diceB, isPlayer);
        
        // Check for rule actions BEFORE calculating final total
        yield return StartCoroutine(CheckAndExecuteRuleActions(diceA, diceB, isPlayer));
        
        // Update totals from dice manager
        UpdateRoundTotals();
        
        Debug.Log($"{(isPlayer ? "Player" : "House")} round total after rule actions: {(isPlayer ? playerRoundTotal : houseRoundTotal)}");
        
        // Check round result
        var result = CheckRoundResult(isPlayer);
        
        switch (result)
        {
            case RoundResult.PlayerWins:
                playerWonCurrentRound = true;
                TransitionToState(GameState.RoundOver);
                yield break;
                
            case RoundResult.HouseWins:
                playerWonCurrentRound = false;
                TransitionToState(GameState.RoundOver);
                yield break;
                
            case RoundResult.Continue:
                // Continue with turn logic below
                break;
        }
        
        // Handle turn progression based on current mode
        if (currentTurnMode == TurnMode.HouseSolo)
        {
            // House continues rolling solo
            if (houseRoundTotal <= 21)
            {
                StartCoroutine(WaitForHouseRoll());
            }
            else
            {
                // House is bust and player has stood - player wins
                Debug.Log($"House is bust ({houseRoundTotal}), player stood at {playerRoundTotal} - Player wins!");
                playerWonCurrentRound = true;
                TransitionToState(GameState.RoundOver);
            }
        }
        else if (currentTurnMode == TurnMode.WaitingForEquality)
        {
            // Give opponent their equal opportunity turn (opposite of who just rolled)
            currentTurnMode = isPlayer ? TurnMode.HouseTurn : TurnMode.PlayerTurn;
            Debug.Log($"[EQUAL OPPORTUNITY] Giving {(currentTurnMode == TurnMode.HouseTurn ? "House" : "Player")} their equal opportunity turn");
            
            if (currentTurnMode == TurnMode.HouseTurn)
            {
                StartCoroutine(WaitForHouseRoll());
            }
        }
        else
        {
            // Normal alternating turns
            SwitchTurn();
            if (currentTurnMode == TurnMode.HouseTurn)
            {
                StartCoroutine(WaitForHouseRoll());
            }
        }
    }
    
    private IEnumerator CheckAndExecuteRuleActions(int diceA, int diceB, bool isPlayer)
    {
        if (diceManager == null || ruleSystem == null) yield break;
        
        ScoredDicePositioner currentPos = isPlayer ? diceManager.GetPlayerScoringPositioner() : diceManager.GetHouseScoringPositioner();
        ScoredDicePositioner opponentPos = isPlayer ? diceManager.GetHouseScoringPositioner() : diceManager.GetPlayerScoringPositioner();
        
        if (currentPos == null || opponentPos == null) yield break;
        
        // Check for available rule actions
        var (matchingDice, swappableDice) = ruleSystem.CheckAvailableRules(diceA, diceB, opponentPos);
        
        // If no rule actions available, continue
        if (matchingDice.Count == 0 && swappableDice.Count == 0)
            yield break;
        
        // Present choices and execute action
        if (isPlayer)
        {
            // Player gets to make decision via UI
            yield return StartCoroutine(PresentPlayerRuleChoices(matchingDice, swappableDice, opponentPos));
        }
        else
        {
            // House AI decision
            yield return StartCoroutine(ruleSystem.ExecuteHouseAIDecision(matchingDice, swappableDice, opponentPos));
            UpdatePlayerScoreDisplay(); // Update after house destroys/swaps
        }
    }
    
    private IEnumerator PresentPlayerRuleChoices(List<int> matchingDice, List<int> swappableDice, ScoredDicePositioner opponentPos)
    {
        if (ruleSystem == null) yield break;
        
        Debug.Log($"Presenting player choices - Matching: {string.Join(",", matchingDice)}, Swappable: {string.Join(",", swappableDice)}");
        
        // Store current choices
        currentMatchingDice = new List<int>(matchingDice);
        currentSwappableDice = new List<int>(swappableDice);
        
        // Highlight available actions using rule system
        ruleSystem.HighlightAvailableActions(matchingDice, swappableDice, opponentPos, OnPlayerClickedOpponentDie);
        
        // Change button to "End Turn"
        isWaitingForPlayerRuleDecision = true;
        if (uiManager != null)
        {
            uiManager.SetRollButtonText("End Turn");
            uiManager.EnableRollButton();
            uiManager.UpdateGoalText("Click dice to destroy or End Turn");
        }
        
        // Wait for player to make choice or end turn
        while (isWaitingForPlayerRuleDecision)
        {
            yield return null;
        }
        
        // If swap occurred, wait for flip animations to complete
        if (diceBeingFlipped.Count > 0)
        {
            Debug.Log("Waiting for dice flip animations to complete...");
            while (diceBeingFlipped.Any(die => die != null && die.IsFlipping()))
            {
                yield return null;
            }
            Debug.Log("Dice flip animations completed");
            diceBeingFlipped.Clear();
            
            // Small delay for visual clarity
            yield return new WaitForSeconds(0.3f);
        }
        
        // Clear highlights
        ruleSystem.ClearHighlights(opponentPos);
        
        // Restore button text
        if (uiManager != null)
        {
            uiManager.SetRollButtonText("Roll");
        }
    }
    
    private void OnPlayerClickedOpponentDie(DB_DiceController clickedDie)
    {
        if (!isWaitingForPlayerRuleDecision || diceManager == null || ruleSystem == null) return;
        
        int dieValue = clickedDie.GetLastRollValue();
        Debug.Log($"Player clicked opponent's die with value {dieValue}");
        
        var playerPos = diceManager.GetPlayerScoringPositioner();
        var opponentPos = diceManager.GetHouseScoringPositioner();
        
        // Check if this die can be destroyed (matching dice rule)
        if (ruleSystem.CanDestroyDie(dieValue, currentMatchingDice))
        {
            ruleSystem.DestroyDie(clickedDie, opponentPos);
            isWaitingForPlayerRuleDecision = false;
            return;
        }
        
        // Check if this die can be swapped (±1 dice rule)
        if (currentSwappableDice.Count > 0)
        {
            DB_DiceController playerDieToSwap = ruleSystem.FindSwappablePlayerDie(clickedDie, playerPos, currentSwappableDice);
            
            if (playerDieToSwap != null)
            {
                // Perform the swap
                ruleSystem.SwapDice(playerDieToSwap, clickedDie, diceBeingFlipped);
                
                // Update both score displays immediately (values already updated in dice)
                UpdatePlayerScoreDisplay();
                UpdateHouseScoreDisplay();
                
                // End rule decision phase
                isWaitingForPlayerRuleDecision = false;
            }
        }
    }
    
    public void OnPlayerEndTurnDuringRuleDecision()
    {
        if (!isWaitingForPlayerRuleDecision) return;
        
        Debug.Log("Player ended turn without using rule action");
        isWaitingForPlayerRuleDecision = false;
    }

    public void OnPlayerStandInAlternating()
    {
        Debug.Log($"[STAND] Player stands with {playerRoundTotal}");
        SetPlayerStood();
        
        // Check if house is already bust - if so, player wins immediately
        if (houseRoundTotal > 21)
        {
            Debug.Log($"[PLAYER WINS] House already bust ({houseRoundTotal}), player stood at {playerRoundTotal}");
            playerWonCurrentRound = true;
            TransitionToState(GameState.RoundOver);
            return;
        }
        
        // Check if player is bust - if so, house wins
        if (playerRoundTotal > 21)
        {
            Debug.Log($"[HOUSE WINS] Player stood while bust ({playerRoundTotal})");
            playerWonCurrentRound = false;
            TransitionToState(GameState.RoundOver);
            return;
        }
        
        // Both valid scores - continue with house solo
        TransitionToState(GameState.PlayerStanding);
    }

    public void OnHouseStandInAlternating()
    {
        Debug.Log($"[STAND] House stands with {houseRoundTotal}");
        
        // Check for bust conditions
        if (houseRoundTotal > 21)
        {
            Debug.Log($"[PLAYER WINS] House stood while bust ({houseRoundTotal})");
            playerWonCurrentRound = true;
            TransitionToState(GameState.RoundOver);
            return;
        }
        
        if (playerRoundTotal > 21)
        {
            Debug.Log($"[HOUSE WINS] Player bust ({playerRoundTotal}), house stood at {houseRoundTotal}");
            playerWonCurrentRound = false;
            TransitionToState(GameState.RoundOver);
            return;
        }
        
        // Both valid - check scores to determine winner
        int playerScore = playerRoundTotal;
        int houseScore = houseRoundTotal;
        
        if (houseScore > playerScore)
        {
            Debug.Log($"[HOUSE WINS] House {houseScore} beats Player {playerScore}");
            playerWonCurrentRound = false;
        }
        else if (playerScore > houseScore)
        {
            Debug.Log($"[PLAYER WINS] Player {playerScore} beats House {houseScore}");
            playerWonCurrentRound = true;
        }
        else
        {
            Debug.Log($"[TIE] Both at {playerScore}");
            playerWonCurrentRound = false; // House wins ties
        }
        
        TransitionToState(GameState.RoundOver);
    }

    private IEnumerator ContinueHouseSolo()
    {
        // Safety check: if house is already bust, player wins
        if (houseRoundTotal > 21)
        {
            Debug.Log($"[SAFETY CHECK] House already bust in ContinueHouseSolo - player wins");
            playerWonCurrentRound = true;
            TransitionToState(GameState.RoundOver);
            yield break;
        }
        
        Debug.Log($"[HOUSE SOLO] House continues, must beat {playerRoundTotal}");
        
        if (uiManager != null)
        {
            uiManager.DisableGameplayButtons();
            uiManager.UpdateGoalText($"House must beat {playerRoundTotal}");
        }
        
        yield return new WaitForSeconds(1f);
        
        // House continues rolling
        if (house != null)
            house.RollDice();
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
        if (roundManager != null)
        {
            roundManager.ResetRounds();
        }
        
        // Start first round (uses same flow as initial Start)
        StartNewRoundInternal();
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

    #region Dice Management

    public void RollSharedDice(System.Action<int, int> onComplete, bool isPlayerTurn)
    {
        if (diceManager == null || diceManager.IsDiceRolling()) 
            return;
            
        StartCoroutine(HandleDiceRoll(onComplete, isPlayerTurn));
    }

    private IEnumerator HandleDiceRoll(System.Action<int, int> onComplete, bool isPlayerTurn)
    {
        yield return StartCoroutine(diceManager.RollDiceAndGetResults(onComplete, isPlayerTurn));
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
