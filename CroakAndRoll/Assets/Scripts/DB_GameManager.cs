using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

/// <summary>
/// Core game manager for Croak and Roll.
/// Handles game state, round management, and the alternating turn system.
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
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private float newRoundDelay = 1.5f;
    
    #endregion

    #region Private Fields
    
    private GameState currentState = GameState.RoundOver;
    
    // Round state tracking
    private bool playerWonCurrentRound = false;
    private bool buttonsInitialized = false;
    
    // Alternating turn system
    private List<RollRow> playerRollRows = new List<RollRow>();
    private List<RollRow> houseRollRows = new List<RollRow>();
    private bool playerHasAdvantage = true;
    private bool isPlayerCurrentRoller = true;
    private bool playerHasStood = false;
    private int playerRoundTotal = 0;
    private int houseRoundTotal = 0;
    private bool isWaitingForHouseRoll = false;
    private bool waitingForEqualOpportunity = false;
    
    // Rule decision system
    private bool isWaitingForPlayerRuleDecision = false;
    private List<int> currentMatchingDice = new List<int>();
    private List<int> currentSwappableDice = new List<int>();
    private List<DB_DiceController> diceBeingFlipped = new List<DB_DiceController>();
    
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeDice();
        InitializeUI();

        roundManager.InitializeRound();
        
        // Start first round
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
            diceManager.Initialize();
            
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
            playerRoundTotal = playerPos.GetTotalScore();
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
            houseRoundTotal = housePos.GetTotalScore();
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
                    playerHasStood = true;
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
            house.OnTurnStart();
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
        isWaitingForHouseRoll = false;
        
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
        Debug.Log("Starting new round");
        
        // Clear previous round data
        if (diceManager != null)
            diceManager.ClearScoredDice();
        
        ResetRoundState();
        DetermineAdvantage();
        InitializePlayers();
        
        TransitionToState(GameState.AlternatingTurns);
    }

    private void ResetRoundState()
    {
        playerRollRows.Clear();
        houseRollRows.Clear();
        playerRoundTotal = 0;
        houseRoundTotal = 0;
        playerHasStood = false;
        isWaitingForHouseRoll = false;
        
        if (uiManager != null)
            uiManager.ClearRoundTotals();
    }

    private void DetermineAdvantage()
    {
        playerHasAdvantage = Random.value < 0.5f;
        isPlayerCurrentRoller = playerHasAdvantage;
        
        Debug.Log($"Advantage: {(playerHasAdvantage ? "PLAYER" : "HOUSE")} goes first");
        
        if (uiManager != null)
        {
            string advantageText = playerHasAdvantage ? "You have advantage!" : "House has advantage!";
            uiManager.UpdateGoalText(advantageText);
        }
    }

    private void InitializePlayers()
    {
        if (player != null)
            player.OnTurnStart(0);
        if (house != null)
            house.OnTurnStart();
    }

    public void OnStartNewRound()
    {
        if (uiManager != null)
            uiManager.HideStandValue();
            
        if (roundManager != null)
            roundManager.CountUpRound();
               
        if (house != null)
            house.ResetTurnValue();
        
        if (uiManager != null)
            uiManager.ClearScoreText();

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
        Debug.Log($"Starting alternating turns - {(isPlayerCurrentRoller ? "Player" : "House")} rolls first");
        
        if (uiManager != null)
        {
            // Always initialize buttons at start of alternating turns
            if (!buttonsInitialized)
            {
                StartCoroutine(uiManager.ShowGameplayButtonsDirectly(
                    () => { if (player != null) player.Stand(); },
                    () => { if (player != null) player.RollDice(); }
                ));
                buttonsInitialized = true;
            }
            
            // Update UI based on who goes first
            if (isPlayerCurrentRoller)
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
        
        // If house has advantage, trigger house roll
        if (!isPlayerCurrentRoller)
        {
            StartCoroutine(TriggerHouseRollAfterDelay());
        }
    }

    private IEnumerator TriggerHouseRollAfterDelay()
    {
        // Prevent multiple overlapping coroutines
        if (isWaitingForHouseRoll)
        {
            Debug.Log("Already waiting for house roll, skipping duplicate trigger");
            yield break;
        }
        
        isWaitingForHouseRoll = true;
        yield return new WaitForSeconds(1f);
        isWaitingForHouseRoll = false;
        
        if (house != null)
            house.RollDice();
    }

    public void OnAlternatingRoll(int diceA, int diceB, bool isPlayer)
    {
        Debug.Log($"{(isPlayer ? "Player" : "House")} rolled: {diceA} + {diceB} = {diceA + diceB}");
        
        // Start coroutine to handle roll with rule checks
        StartCoroutine(ProcessAlternatingRollWithRules(diceA, diceB, isPlayer));
    }
    
    private IEnumerator ProcessAlternatingRollWithRules(int diceA, int diceB, bool isPlayer)
    {
        // Add to appropriate roll rows
        if (isPlayer)
        {
            playerRollRows.Add(new RollRow(diceA, diceB));
            
            // Enable Stand button after first roll
            if (uiManager != null && playerRollRows.Count == 1)
            {
                uiManager.EnableStandButton();
            }
            
            // Check for rule actions BEFORE calculating final total
            yield return StartCoroutine(CheckAndExecuteRuleActions(diceA, diceB, true));
            
            // Total has already been updated via callbacks, just ensure we have latest value
            if (diceManager != null)
            {
                var playerPos = diceManager.GetPlayerScoringPositioner();
                if (playerPos != null)
                {
                    playerRoundTotal = playerPos.GetTotalScore();
                    Debug.Log($"Player round total after rule actions: {playerRoundTotal}");
                }
            }
            
            // Check for bust or 21, but respect equal opportunity
            if (playerRoundTotal > 21)
            {
                Debug.Log($"Player BUSTED! Player rolls: {playerRollRows.Count}, House rolls: {houseRollRows.Count}");
                
                // Check if house has had equal opportunity
                if (houseRollRows.Count < playerRollRows.Count && !playerHasStood)
                {
                    Debug.Log("House needs equal opportunity - giving house another turn");
                    waitingForEqualOpportunity = true;
                }
                else
                {
                    playerWonCurrentRound = false;
                    TransitionToState(GameState.RoundOver);
                    yield break;
                }
            }
            else if (playerRoundTotal == 21)
            {
                Debug.Log($"Player hit 21! Player rolls: {playerRollRows.Count}, House rolls: {houseRollRows.Count}");
                
                // Check if house has had equal opportunity
                if (houseRollRows.Count < playerRollRows.Count && !playerHasStood)
                {
                    Debug.Log("House needs equal opportunity - giving house another turn");
                    waitingForEqualOpportunity = true;
                }
                else
                {
                    playerWonCurrentRound = true;
                    TransitionToState(GameState.RoundOver);
                    yield break;
                }
            }
            
            // If we were waiting for equal opportunity and now have it, check final conditions
            if (waitingForEqualOpportunity && playerRollRows.Count >= houseRollRows.Count)
            {
                Debug.Log("Equal opportunity achieved - checking final conditions");
                waitingForEqualOpportunity = false;
                
                // Recheck house's total (might have changed due to player's rule actions)
                if (diceManager != null)
                {
                    var housePos = diceManager.GetHouseScoringPositioner();
                    if (housePos != null)
                    {
                        houseRoundTotal = housePos.GetTotalScore();
                    }
                }
                
                // Determine winner after equal opportunity
                if (houseRoundTotal > 21)
                {
                    // House is bust
                    if (playerRoundTotal > 21)
                    {
                        Debug.Log($"Both bust - House: {houseRoundTotal}, Player: {playerRoundTotal}");
                        // Both bust - house wins by default
                        playerWonCurrentRound = false;
                    }
                    else
                    {
                        Debug.Log($"House confirmed bust: {houseRoundTotal}, Player: {playerRoundTotal}");
                        playerWonCurrentRound = true;
                    }
                    TransitionToState(GameState.RoundOver);
                    yield break;
                }
                else if (houseRoundTotal == 21)
                {
                    Debug.Log($"House confirmed 21: {houseRoundTotal}");
                    playerWonCurrentRound = false;
                    TransitionToState(GameState.RoundOver);
                    yield break;
                }
            }
            
            // Switch to house's turn (unless player has stood)
            if (!playerHasStood)
            {
                isPlayerCurrentRoller = false;
                if (uiManager != null)
                {
                    uiManager.SetTurnMarkerToHouse();
                    uiManager.DisableGameplayButtons();
                    uiManager.UpdateGoalText("House's turn");
                }
                StartCoroutine(TriggerHouseRollAfterDelay());
            }
        }
        else
        {
            houseRollRows.Add(new RollRow(diceA, diceB));
            
            // Check for rule actions BEFORE calculating final total
            yield return StartCoroutine(CheckAndExecuteRuleActions(diceA, diceB, false));
            
            // Total has already been updated via callbacks, just ensure we have latest value
            if (diceManager != null)
            {
                var housePos = diceManager.GetHouseScoringPositioner();
                if (housePos != null)
                {
                    houseRoundTotal = housePos.GetTotalScore();
                    Debug.Log($"House round total after rule actions: {houseRoundTotal}");
                }
            }
            
            // Check for bust or 21, but respect equal opportunity
            if (houseRoundTotal > 21)
            {
                Debug.Log($"House BUSTED! Player rolls: {playerRollRows.Count}, House rolls: {houseRollRows.Count}");
                
                // Check if player has had equal opportunity
                if (playerRollRows.Count < houseRollRows.Count && !playerHasStood)
                {
                    Debug.Log("Player needs equal opportunity - giving player another turn");
                    waitingForEqualOpportunity = true;
                }
                else
                {
                    playerWonCurrentRound = true;
                    TransitionToState(GameState.RoundOver);
                    yield break;
                }
            }
            else if (houseRoundTotal == 21)
            {
                Debug.Log($"House hit 21! Player rolls: {playerRollRows.Count}, House rolls: {houseRollRows.Count}");
                
                // Check if player has had equal opportunity
                if (playerRollRows.Count < houseRollRows.Count && !playerHasStood)
                {
                    Debug.Log("Player needs equal opportunity - giving player another turn");
                    waitingForEqualOpportunity = true;
                }
                else
                {
                    playerWonCurrentRound = false;
                    TransitionToState(GameState.RoundOver);
                    yield break;
                }
            }
            
            // If we were waiting for equal opportunity and now have it, check final conditions
            if (waitingForEqualOpportunity && houseRollRows.Count >= playerRollRows.Count)
            {
                Debug.Log("Equal opportunity achieved - checking final conditions");
                waitingForEqualOpportunity = false;
                
                // Recheck player's total (might have changed due to house's rule actions)
                if (diceManager != null)
                {
                    var playerPos = diceManager.GetPlayerScoringPositioner();
                    if (playerPos != null)
                    {
                        playerRoundTotal = playerPos.GetTotalScore();
                    }
                }
                
                // Determine winner after equal opportunity
                if (playerRoundTotal > 21)
                {
                    // Player is bust
                    if (houseRoundTotal > 21)
                    {
                        Debug.Log($"Both bust - House: {houseRoundTotal}, Player: {playerRoundTotal}");
                        // Both bust - house wins by default
                        playerWonCurrentRound = false;
                    }
                    else
                    {
                        Debug.Log($"Player confirmed bust: {playerRoundTotal}, House: {houseRoundTotal}");
                        playerWonCurrentRound = false;
                    }
                    TransitionToState(GameState.RoundOver);
                    yield break;
                }
                else if (playerRoundTotal == 21)
                {
                    Debug.Log($"Player confirmed 21: {playerRoundTotal}");
                    playerWonCurrentRound = true;
                    TransitionToState(GameState.RoundOver);
                    yield break;
                }
            }
            
            // Check if house has won (only if neither side is bust)
            if (playerHasStood && houseRoundTotal <= 21 && playerRoundTotal <= 21)
            {
                if (houseRoundTotal >= playerRoundTotal)
                {
                    Debug.Log($"House wins with {houseRoundTotal} vs Player's {playerRoundTotal}");
                    playerWonCurrentRound = false;
                    TransitionToState(GameState.RoundOver);
                    yield break;
                }
            }
            
            // Switch to player's turn (unless in solo mode)
            if (!playerHasStood)
            {
                isPlayerCurrentRoller = true;
                if (uiManager != null)
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
            }
            else
            {
                // House continues rolling solo - but only if house isn't bust
                if (houseRoundTotal <= 21)
                {
                    StartCoroutine(TriggerHouseRollAfterDelay());
                }
                else
                {
                    // House is bust and player has stood - player wins
                    Debug.Log($"House is bust ({houseRoundTotal}), player stood at {playerRoundTotal} - Player wins!");
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
        Debug.Log($"Player stands with {playerRoundTotal}");
        playerHasStood = true;
        TransitionToState(GameState.PlayerStanding);
    }

    private IEnumerator ContinueHouseSolo()
    {
        Debug.Log($"House continues solo, must beat {playerRoundTotal}");
        
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
        Debug.Log("Restarting game...");
        
        ResetGameState();
        ResetPlayers();
        
        if (diceManager != null)
            diceManager.ClearScoredDice();
        
        roundManager.InitializeRound();
        StartNewRoundInternal();
    }

    private void ResetGameState()
    {
        currentState = GameState.RoundOver;
        buttonsInitialized = false;
        
        ResetRoundState();
        playerHasAdvantage = true;
        isPlayerCurrentRoller = true;
        
        if (uiManager != null)
        {
            uiManager.ClearRoundTotals();
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
    
    // Alternating turn system data
    public int GetPlayerRoundTotal() => playerRoundTotal;
    public int GetHouseRoundTotal() => houseRoundTotal;
    public bool PlayerHasAdvantage() => playerHasAdvantage;
    public List<RollRow> GetPlayerRollRows() => playerRollRows;
    public List<RollRow> GetHouseRollRows() => houseRollRows;
    
    #endregion
}
