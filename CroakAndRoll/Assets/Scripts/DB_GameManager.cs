using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

/// <summary>
/// Core game manager for Croak and Roll.
/// Handles game state transitions, dice rolling coordination, and win/loss conditions.
/// 
/// Round-specific logic (advantage, turn switching, equal opportunity) is delegated to DB_AlternatingRoundManager.
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
        GameOver,          // Game completely over
        
        // Legacy states - kept for potential future use
        PlayerTurn,        // Legacy: Player rolling dice (not used in alternating system)
        HouseTurn          // Legacy: House rolling dice (not used in alternating system)
    }
    
    #endregion

    #region Serialized Fields

    [Header("Player References")]
    [SerializeField] private Player player;
    [SerializeField] private House house;

    [Header("Managers")]
    [SerializeField] private DB_RoundManager roundManager;
    [SerializeField] private DB_AlternatingRoundManager alternatingRoundManager;
    [SerializeField] private DB_DiceManager diceManager;
    [SerializeField] private DB_UIManager uiManager;

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
        if (diceManager == null || uiManager == null || alternatingRoundManager == null) return;
        
        var playerPos = diceManager.GetPlayerScoringPositioner();
        if (playerPos != null)
        {
            alternatingRoundManager.UpdateRoundTotals();
            uiManager.UpdatePlayerRoundTotal(alternatingRoundManager.PlayerRoundTotal);
            Debug.Log($"Player score updated: {alternatingRoundManager.PlayerRoundTotal}");
        }
    }
    
    /// <summary>
    /// Update house score display from current dice positions
    /// </summary>
    private void UpdateHouseScoreDisplay()
    {
        if (diceManager == null || uiManager == null || alternatingRoundManager == null) return;
        
        var housePos = diceManager.GetHouseScoringPositioner();
        if (housePos != null)
        {
            alternatingRoundManager.UpdateRoundTotals();
            uiManager.UpdateHouseRoundTotal(alternatingRoundManager.HouseRoundTotal);
            Debug.Log($"House score updated: {alternatingRoundManager.HouseRoundTotal}");
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
            case GameState.AlternatingTurns:
                StartAlternatingTurns();
                break;
                
            case GameState.PlayerStanding:
                if (previousState == GameState.AlternatingTurns)
                {
                    if (alternatingRoundManager != null)
                        alternatingRoundManager.SetPlayerStood();
                    StartCoroutine(ContinueHouseSolo());
                }
                else
                {
                    // Fallback to legacy behavior if needed
                    TransitionToState(GameState.HouseTurn);
                }
                break;
                
            case GameState.RoundOver:
                HandleRoundOver();
                break;
                
            case GameState.GameOver:
                if (uiManager != null)
                    uiManager.ShowGameOverPanel();
                break;
                
            // Legacy states - keep for backward compatibility
            case GameState.PlayerTurn:
                StartPlayerTurnInternal();
                break;
                
            case GameState.HouseTurn:
                StartHouseTurnInternal();
                break;
        }
    }
    
    #endregion

    #region Turn Management (Legacy - for backward compatibility)

    private void StartPlayerTurnInternal()
    {
        if (uiManager != null)
        {
            uiManager.UpdateGoalText("Roll Closest to 21");
            uiManager.SetTurnMarkerToPlayer();
        }
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
            house.OnRoundStart(); // Legacy mode - now called OnRoundStart for consistency
        else
            Debug.LogError("House is null! Cannot start house turn.");
    }

    public void StartPlayerTurn() => TransitionToState(GameState.PlayerTurn);

    public void EndPlayerTurn()
    {
        if (currentState != GameState.PlayerTurn)
        {
            Debug.LogWarning($"EndPlayerTurn called but not in PlayerTurn state. Current: {currentState}");
            return;
        }
        TransitionToState(GameState.PlayerStanding);
    }

    public void StartHouseTurn() => TransitionToState(GameState.HouseTurn);

    public void EndHouseTurn()
    {
        if (currentState != GameState.HouseTurn)
        {
            Debug.LogWarning($"EndHouseTurn called but not in HouseTurn state. Current: {currentState}");
            return;
        }
        TransitionToState(GameState.RoundOver);
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
        if (alternatingRoundManager != null)
            alternatingRoundManager.InitializeRound();
        
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

    private void StartAlternatingTurns()
    {
        if (alternatingRoundManager == null)
        {
            Debug.LogError("AlternatingRoundManager is null!");
            return;
        }
        
        // Initialize buttons and prepare UI in proper sequence
        if (!buttonsInitialized)
        {
            StartCoroutine(InitializeAlternatingTurnsUI());
            buttonsInitialized = true;
        }
        else
        {
            // Buttons already initialized, just prepare UI for this round
            alternatingRoundManager.PrepareAlternatingTurnsUI();
            
            // If house has advantage, trigger house roll
            if (!alternatingRoundManager.IsPlayerCurrentRoller)
            {
                StartCoroutine(alternatingRoundManager.WaitForHouseRoll());
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
        if (alternatingRoundManager != null)
        {
            alternatingRoundManager.PrepareAlternatingTurnsUI();
            
            // If house has advantage, trigger house roll
            if (!alternatingRoundManager.IsPlayerCurrentRoller)
            {
                StartCoroutine(alternatingRoundManager.WaitForHouseRoll());
            }
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
        if (alternatingRoundManager == null) yield break;
        
        // Add roll to round manager
        alternatingRoundManager.AddRoll(diceA, diceB, isPlayer);
        
        // Check for rule actions BEFORE calculating final total
        yield return StartCoroutine(CheckAndExecuteRuleActions(diceA, diceB, isPlayer));
        
        // Update totals from dice manager
        alternatingRoundManager.UpdateRoundTotals();
        
        Debug.Log($"{(isPlayer ? "Player" : "House")} round total after rule actions: {(isPlayer ? alternatingRoundManager.PlayerRoundTotal : alternatingRoundManager.HouseRoundTotal)}");
        
        // Check round result
        var result = alternatingRoundManager.CheckRoundResult(isPlayer);
        
        switch (result)
        {
            case DB_AlternatingRoundManager.RoundResult.PlayerWins:
                playerWonCurrentRound = true;
                TransitionToState(GameState.RoundOver);
                yield break;
                
            case DB_AlternatingRoundManager.RoundResult.HouseWins:
                playerWonCurrentRound = false;
                TransitionToState(GameState.RoundOver);
                yield break;
                
            case DB_AlternatingRoundManager.RoundResult.Continue:
                // Continue with turn logic below
                break;
        }
        
        // Switch turns or continue house solo
        if (isPlayer)
        {
            // Switch to house's turn (unless player has stood)
            if (!alternatingRoundManager.PlayerHasStood)
            {
                alternatingRoundManager.SwitchTurn();
                StartCoroutine(alternatingRoundManager.WaitForHouseRoll());
            }
        }
        else
        {
            // Switch to player's turn (unless in solo mode)
            if (!alternatingRoundManager.PlayerHasStood)
            {
                alternatingRoundManager.SwitchTurn();
            }
            else
            {
                // House continues rolling solo - but only if house isn't bust
                if (alternatingRoundManager.HouseRoundTotal <= 21)
                {
                    StartCoroutine(alternatingRoundManager.WaitForHouseRoll());
                }
                else
                {
                    // House is bust and player has stood - player wins
                    Debug.Log($"House is bust ({alternatingRoundManager.HouseRoundTotal}), player stood at {alternatingRoundManager.PlayerRoundTotal} - Player wins!");
                    playerWonCurrentRound = true;
                    TransitionToState(GameState.RoundOver);
                }
            }
        }
    }
    
    private IEnumerator CheckAndExecuteRuleActions(int diceA, int diceB, bool isPlayer)
    {
        if (diceManager == null) yield break;
        
        ScoredDicePositioner currentPos = isPlayer ? diceManager.GetPlayerScoringPositioner() : diceManager.GetHouseScoringPositioner();
        ScoredDicePositioner opponentPos = isPlayer ? diceManager.GetHouseScoringPositioner() : diceManager.GetPlayerScoringPositioner();
        
        if (currentPos == null || opponentPos == null) yield break;
        
        // Get opponent's dice values and last row
        List<int> opponentDiceValues = opponentPos.GetAllDiceValues();
        var opponentLastRow = opponentPos.GetLastRow();
        
        // Check Rule 1: Matching dice
        List<int> matchingDice = new List<int>();
        if (opponentDiceValues.Contains(diceA))
            matchingDice.Add(diceA);
        if (opponentDiceValues.Contains(diceB) && !matchingDice.Contains(diceB))
            matchingDice.Add(diceB);
        
        // Check Rule 2: ±1 dice
        List<int> swappableDice = new List<int>();
        if (opponentLastRow != null)
        {
            int lastDiceA = opponentLastRow.diceA != null ? opponentLastRow.diceA.GetLastRollValue() : -1;
            int lastDiceB = opponentLastRow.diceB != null ? opponentLastRow.diceB.GetLastRollValue() : -1;
            
            if (lastDiceA > 0 && (Mathf.Abs(diceA - lastDiceA) == 1 || Mathf.Abs(diceB - lastDiceA) == 1))
            {
                if (Mathf.Abs(diceA - lastDiceA) == 1 && !swappableDice.Contains(diceA))
                    swappableDice.Add(diceA);
                if (Mathf.Abs(diceB - lastDiceA) == 1 && !swappableDice.Contains(diceB))
                    swappableDice.Add(diceB);
            }
            
            if (lastDiceB > 0 && (Mathf.Abs(diceA - lastDiceB) == 1 || Mathf.Abs(diceB - lastDiceB) == 1))
            {
                if (Mathf.Abs(diceA - lastDiceB) == 1 && !swappableDice.Contains(diceA))
                    swappableDice.Add(diceA);
                if (Mathf.Abs(diceB - lastDiceB) == 1 && !swappableDice.Contains(diceB))
                    swappableDice.Add(diceB);
            }
        }
        
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
            yield return StartCoroutine(ExecuteHouseRuleDecision(matchingDice, swappableDice, opponentPos));
        }
    }
    
    private IEnumerator PresentPlayerRuleChoices(List<int> matchingDice, List<int> swappableDice, ScoredDicePositioner opponentPos)
    {
        Debug.Log($"Presenting player choices - Matching: {string.Join(",", matchingDice)}, Swappable: {string.Join(",", swappableDice)}");
        
        // Store current choices
        currentMatchingDice = new List<int>(matchingDice);
        currentSwappableDice = new List<int>(swappableDice);
        
        // Highlight matching dice in red (destroyable)
        foreach (int value in matchingDice)
        {
            opponentPos.HighlightDiceWithValue(value, Color.red, OnPlayerClickedOpponentDie);
        }
        
        // Highlight swappable dice in blue (opponent's last row that can be swapped)
        if (swappableDice.Count > 0)
        {
            var opponentLastRow = opponentPos.GetLastRow();
            if (opponentLastRow != null)
            {
                // Check which of opponent's last row dice are ±1 from current roll
                bool highlightA = false;
                bool highlightB = false;
                
                if (opponentLastRow.diceA != null)
                {
                    int lastDiceA = opponentLastRow.diceA.GetLastRollValue();
                    // Check if any of our swappable dice are ±1 from this die
                    foreach (int swapValue in swappableDice)
                    {
                        if (Mathf.Abs(swapValue - lastDiceA) == 1)
                        {
                            highlightA = true;
                            break;
                        }
                    }
                }
                
                if (opponentLastRow.diceB != null)
                {
                    int lastDiceB = opponentLastRow.diceB.GetLastRollValue();
                    // Check if any of our swappable dice are ±1 from this die
                    foreach (int swapValue in swappableDice)
                    {
                        if (Mathf.Abs(swapValue - lastDiceB) == 1)
                        {
                            highlightB = true;
                            break;
                        }
                    }
                }
                
                opponentPos.HighlightLastRowDice(highlightA, highlightB, Color.blue, OnPlayerClickedOpponentDie);
            }
        }
        
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
        opponentPos.ClearAllHighlights();
        
        // Restore button text
        if (uiManager != null)
        {
            uiManager.SetRollButtonText("Roll");
        }
    }
    
    private IEnumerator ExecuteHouseRuleDecision(List<int> matchingDice, List<int> swappableDice, ScoredDicePositioner opponentPos)
    {
        yield return new WaitForSeconds(0.5f);
        
        // House AI: Prioritize destroying dice that are high value
        if (matchingDice.Count > 0)
        {
            // Destroy the highest matching die
            int highestMatch = Mathf.Max(matchingDice.ToArray());
            var dieToDestroy = opponentPos.FindDieByValue(highestMatch);
            if (dieToDestroy != null)
            {
                Debug.Log($"House destroys player's die with value {highestMatch}");
                opponentPos.RemoveDie(dieToDestroy);
                if (uiManager != null)
                    uiManager.UpdateGoalText($"House destroyed your {highestMatch}!");
                yield return new WaitForSeconds(1f);
            }
        }
        // Otherwise consider swapping if beneficial
        else if (swappableDice.Count > 0)
        {
            // For now, don't implement swap (more complex logic needed)
            Debug.Log($"House could swap but chooses not to");
        }
        
        yield return new WaitForSeconds(0.3f);
    }
    
    private void OnPlayerClickedOpponentDie(DB_DiceController clickedDie)
    {
        if (!isWaitingForPlayerRuleDecision || diceManager == null) return;
        
        int dieValue = clickedDie.GetLastRollValue();
        Debug.Log($"Player clicked opponent's die with value {dieValue}");
        
        var playerPos = diceManager.GetPlayerScoringPositioner();
        var opponentPos = diceManager.GetHouseScoringPositioner();
        
        // Check if this die can be destroyed (matching dice rule)
        if (currentMatchingDice.Contains(dieValue))
        {
            // Destroy the die
            if (opponentPos != null)
            {
                opponentPos.RemoveDie(clickedDie);
                if (uiManager != null)
                    uiManager.UpdateGoalText($"Destroyed opponent's {dieValue}!");
            }
            
            // End rule decision phase
            isWaitingForPlayerRuleDecision = false;
            return;
        }
        
        // Check if this die can be swapped (±1 dice rule)
        var playerLastRow = playerPos?.GetLastRow();
        
        if (playerLastRow != null && currentSwappableDice.Count > 0)
        {
            // Find which of player's dice can swap with this opponent die
            DB_DiceController playerDieToSwap = null;
            
            if (playerLastRow.diceA != null)
            {
                int playerDiceA = playerLastRow.diceA.GetLastRollValue();
                if (Mathf.Abs(playerDiceA - dieValue) == 1 && currentSwappableDice.Contains(playerDiceA))
                {
                    playerDieToSwap = playerLastRow.diceA;
                }
            }
            
            if (playerDieToSwap == null && playerLastRow.diceB != null)
            {
                int playerDiceB = playerLastRow.diceB.GetLastRollValue();
                if (Mathf.Abs(playerDiceB - dieValue) == 1 && currentSwappableDice.Contains(playerDiceB))
                {
                    playerDieToSwap = playerLastRow.diceB;
                }
            }
            
            if (playerDieToSwap != null)
            {
                // Perform the swap by flipping both dice to opposite values
                int playerValue = playerDieToSwap.GetLastRollValue();
                int opponentValue = clickedDie.GetLastRollValue();
                
                Debug.Log($"Swapping player's {playerValue} with opponent's {opponentValue}");
                
                // Track dice being flipped for animation completion
                diceBeingFlipped.Clear();
                diceBeingFlipped.Add(playerDieToSwap);
                diceBeingFlipped.Add(clickedDie);
                
                playerDieToSwap.FlipToOppositeFace(opponentValue);
                clickedDie.FlipToOppositeFace(playerValue);
                
                // Update both score displays immediately (values already updated in dice)
                UpdatePlayerScoreDisplay();
                UpdateHouseScoreDisplay();
                
                if (uiManager != null)
                    uiManager.UpdateGoalText($"Swapped {playerValue} for {opponentValue}!");
                
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
        if (alternatingRoundManager == null) return;
        
        Debug.Log($"[STAND] Player stands with {alternatingRoundManager.PlayerRoundTotal}");
        alternatingRoundManager.SetPlayerStood();
        
        // Check if house is already bust - if so, player wins immediately
        if (alternatingRoundManager.HouseRoundTotal > 21)
        {
            Debug.Log($"[PLAYER WINS] House already bust ({alternatingRoundManager.HouseRoundTotal}), player stood at {alternatingRoundManager.PlayerRoundTotal}");
            playerWonCurrentRound = true;
            TransitionToState(GameState.RoundOver);
            return;
        }
        
        // Check if player is bust - if so, house wins
        if (alternatingRoundManager.PlayerRoundTotal > 21)
        {
            Debug.Log($"[HOUSE WINS] Player stood while bust ({alternatingRoundManager.PlayerRoundTotal})");
            playerWonCurrentRound = false;
            TransitionToState(GameState.RoundOver);
            return;
        }
        
        // Both valid scores - continue with house solo
        TransitionToState(GameState.PlayerStanding);
    }

    public void OnHouseStandInAlternating()
    {
        if (alternatingRoundManager == null) return;
        
        Debug.Log($"[STAND] House stands with {alternatingRoundManager.HouseRoundTotal}");
        
        // Check for bust conditions
        if (alternatingRoundManager.HouseRoundTotal > 21)
        {
            Debug.Log($"[PLAYER WINS] House stood while bust ({alternatingRoundManager.HouseRoundTotal})");
            playerWonCurrentRound = true;
            TransitionToState(GameState.RoundOver);
            return;
        }
        
        if (alternatingRoundManager.PlayerRoundTotal > 21)
        {
            Debug.Log($"[HOUSE WINS] Player bust ({alternatingRoundManager.PlayerRoundTotal}), house stood at {alternatingRoundManager.HouseRoundTotal}");
            playerWonCurrentRound = false;
            TransitionToState(GameState.RoundOver);
            return;
        }
        
        // Both valid - check scores to determine winner
        int playerScore = alternatingRoundManager.PlayerRoundTotal;
        int houseScore = alternatingRoundManager.HouseRoundTotal;
        
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
        if (alternatingRoundManager == null) yield break;
        
        // Safety check: if house is already bust, player wins
        if (alternatingRoundManager.HouseRoundTotal > 21)
        {
            Debug.Log($"[SAFETY CHECK] House already bust in ContinueHouseSolo - player wins");
            playerWonCurrentRound = true;
            TransitionToState(GameState.RoundOver);
            yield break;
        }
        
        Debug.Log($"[HOUSE SOLO] House continues, must beat {alternatingRoundManager.PlayerRoundTotal}");
        
        if (uiManager != null)
        {
            uiManager.DisableGameplayButtons();
            uiManager.UpdateGoalText($"House must beat {alternatingRoundManager.PlayerRoundTotal}");
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
            player.ClearPerks();
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
        // Disable buttons before rolling (legacy PlayerTurn state only)
        if (currentState == GameState.PlayerTurn && isPlayerTurn && uiManager != null)
            uiManager.DisableGameplayButtons();

        yield return StartCoroutine(diceManager.RollDiceAndGetResults(onComplete, isPlayerTurn));

        // Re-enable buttons after rolling (legacy PlayerTurn state only)
        if (currentState == GameState.PlayerTurn && isPlayerTurn && uiManager != null)
        {
            if (player != null && player.HasRolledThisTurn())
                uiManager.EnableStandButton();
            uiManager.EnableRollButton();
        }
    }
    
    #endregion

    #region Public API

    // State queries
    public GameState GetCurrentState() => currentState;
    public bool IsPlayerTurn() => currentState == GameState.PlayerTurn;
    public bool IsHouseTurn() => currentState == GameState.HouseTurn;
    public bool IsDiceRolling() => diceManager != null && diceManager.IsDiceRolling();
    public bool IsWaitingForPlayerRuleDecision() => isWaitingForPlayerRuleDecision;
    
    // UI control
    public void DisableGameplayButtons()
    {
        if (uiManager != null)
            uiManager.DisableGameplayButtons();
    }
    
    // Alternating turn system data - delegate to round manager
    public int GetPlayerRoundTotal() => alternatingRoundManager != null ? alternatingRoundManager.PlayerRoundTotal : 0;
    public int GetHouseRoundTotal() => alternatingRoundManager != null ? alternatingRoundManager.HouseRoundTotal : 0;
    public bool PlayerHasAdvantage() => alternatingRoundManager != null && alternatingRoundManager.PlayerHasAdvantage;
    public List<DB_AlternatingRoundManager.RollRow> GetPlayerRollRows() => alternatingRoundManager != null ? alternatingRoundManager.PlayerRollRows : new List<DB_AlternatingRoundManager.RollRow>();
    public List<DB_AlternatingRoundManager.RollRow> GetHouseRollRows() => alternatingRoundManager != null ? alternatingRoundManager.HouseRollRows : new List<DB_AlternatingRoundManager.RollRow>();
    
    #endregion
}
