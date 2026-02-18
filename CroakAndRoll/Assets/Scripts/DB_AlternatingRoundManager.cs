using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the alternating turn round system for Croak and Roll.
/// 
/// TERMINOLOGY:
/// - Round: Complete game cycle from start to winner determination
/// - Advantage: Determines which player gets the first turn in a round
/// - Turn: One player's opportunity to roll dice and take action (turns alternate between players)
/// - Roll: The actual dice rolling that happens during a turn
/// 
/// FLOW:
/// 1. Round starts → advantage determined → first player's turn begins
/// 2. Player rolls dice → takes action (if applicable) → turn ends
/// 3. Turns alternate until round ends (bust, 21, or stand)
/// 4. Equal opportunity rule: Both players get same number of turns before round ends
/// </summary>
public class DB_AlternatingRoundManager : MonoBehaviour
{
    #region Nested Classes
    
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
    
    public enum RoundResult
    {
        None,
        PlayerWins,
        HouseWins,
        Continue
    }
    
    #endregion
    
    #region Serialized Fields
    
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private House house;
    [SerializeField] private DB_DiceManager diceManager;
    [SerializeField] private DB_UIManager uiManager;
    
    #endregion
    
    #region Public Properties
    
    // Round state
    public bool PlayerHasAdvantage { get; private set; } = true; // Who gets first turn in the round
    public bool IsPlayerCurrentRoller { get; private set; } = true; // Whose turn it currently is
    public bool PlayerHasStood { get; private set; } = false; // Has player ended their turns for this round
    public int PlayerRoundTotal { get; private set; } = 0; // Player's total score this round
    public int HouseRoundTotal { get; private set; } = 0; // House's total score this round
    public bool IsWaitingForHouseRoll { get; private set; } = false; // Prevents duplicate house roll triggers
    public bool WaitingForEqualOpportunity { get; private set; } = false; // Waiting for equal turn count
    
    // Roll tracking (each turn produces one roll row)
    public List<RollRow> PlayerRollRows { get; private set; } = new List<RollRow>();
    public List<RollRow> HouseRollRows { get; private set; } = new List<RollRow>();
    
    #endregion
    
    #region Round Lifecycle
    
    /// <summary>
    /// Initialize a new round - resets all state and determines advantage
    /// </summary>
    public void InitializeRound()
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
        PlayerRollRows.Clear();
        HouseRollRows.Clear();
        PlayerRoundTotal = 0;
        HouseRoundTotal = 0;
        PlayerHasStood = false;
        IsWaitingForHouseRoll = false;
        WaitingForEqualOpportunity = false;
        
        // Note: PlayerHasAdvantage and IsPlayerCurrentRoller will be set by DetermineAdvantage()
    }
    
    /// <summary>
    /// Determine who has advantage this round (goes first)
    /// </summary>
    private void DetermineAdvantage()
    {
        PlayerHasAdvantage = Random.value < 0.5f;
        IsPlayerCurrentRoller = PlayerHasAdvantage;
        
        Debug.Log($"[ADVANTAGE] {(PlayerHasAdvantage ? "PLAYER" : "HOUSE")} gets first turn");
        
        if (uiManager != null)
        {
            string advantageText = PlayerHasAdvantage ? "You have advantage!" : "House has advantage!";
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
    
    #endregion
    
    #region Turn Management
    
    /// <summary>
    /// Prepare UI for alternating turns at round start
    /// </summary>
    public void PrepareAlternatingTurnsUI()
    {
        if (uiManager == null) return;
        
        Debug.Log($"[TURN START] {(IsPlayerCurrentRoller ? "Player" : "House")}'s turn begins");
        
        // Update UI based on whose turn it is
        if (IsPlayerCurrentRoller)
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
    public void AddRoll(int diceA, int diceB, bool isPlayer)
    {
        if (isPlayer)
        {
            PlayerRollRows.Add(new RollRow(diceA, diceB));
            
            // Enable Stand button after first roll
            if (uiManager != null && PlayerRollRows.Count == 1)
            {
                uiManager.EnableStandButton();
            }
        }
        else
        {
            HouseRollRows.Add(new RollRow(diceA, diceB));
        }
    }
    
    /// <summary>
    /// Update round totals from dice manager
    /// </summary>
    public void UpdateRoundTotals()
    {
        if (diceManager == null) return;
        
        var playerPos = diceManager.GetPlayerScoringPositioner();
        var housePos = diceManager.GetHouseScoringPositioner();
        
        if (playerPos != null)
        {
            PlayerRoundTotal = playerPos.GetTotalScore();
        }
        
        if (housePos != null)
        {
            HouseRoundTotal = housePos.GetTotalScore();
        }
    }
    
    /// <summary>
    /// Switch to the other player's turn
    /// </summary>
    public void SwitchTurn()
    {
        IsPlayerCurrentRoller = !IsPlayerCurrentRoller;
        
        if (uiManager == null) return;
        
        Debug.Log($"[TURN SWITCH] {(IsPlayerCurrentRoller ? "Player" : "House")}'s turn begins");
        
        if (IsPlayerCurrentRoller)
        {
            uiManager.SetTurnMarkerToPlayer();
            uiManager.EnableRollButton();
            
            // Enable stand button if player has already rolled
            if (PlayerRollRows.Count > 0)
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
    /// Mark that player has stood
    /// </summary>
    public void SetPlayerStood()
    {
        PlayerHasStood = true;
        IsPlayerCurrentRoller = false;
        Debug.Log("Player has stood - house continues solo");
    }
    
    /// <summary>
    /// Start waiting for house roll (prevents duplicate triggers)
    /// </summary>
    public IEnumerator WaitForHouseRoll(float delay = 1f)
    {
        if (IsWaitingForHouseRoll)
        {
            Debug.Log("Already waiting for house roll, skipping duplicate trigger");
            yield break;
        }
        
        IsWaitingForHouseRoll = true;
        yield return new WaitForSeconds(delay);
        IsWaitingForHouseRoll = false;
        
        if (house != null)
            house.RollDice();
    }
    
    #endregion
    
    #region Win Conditions
    
    /// <summary>
    /// Check if the round should end based on current totals and equal opportunity
    /// </summary>
    public RoundResult CheckRoundResult(bool isPlayer)
    {
        int currentTotal = isPlayer ? PlayerRoundTotal : HouseRoundTotal;
        int opponentTotal = isPlayer ? HouseRoundTotal : PlayerRoundTotal;
        int currentRollCount = isPlayer ? PlayerRollRows.Count : HouseRollRows.Count;
        int opponentRollCount = isPlayer ? HouseRollRows.Count : PlayerRollRows.Count;
        
        // Check for bust
        if (currentTotal > 21)
        {
            Debug.Log($"{(isPlayer ? "Player" : "House")} BUSTED! Rolls: {currentRollCount} vs {opponentRollCount}");
            
            // Check if opponent needs equal opportunity
            if (opponentRollCount < currentRollCount && !PlayerHasStood)
            {
                Debug.Log($"{(isPlayer ? "House" : "Player")} needs equal opportunity - giving another turn");
                WaitingForEqualOpportunity = true;
                return RoundResult.Continue;
            }
            
            // Round ends - opponent wins (or house wins if both bust)
            return isPlayer ? RoundResult.HouseWins : RoundResult.PlayerWins;
        }
        
        // Check for 21
        if (currentTotal == 21)
        {
            Debug.Log($"{(isPlayer ? "Player" : "House")} hit 21! Rolls: {currentRollCount} vs {opponentRollCount}");
            
            // Check if opponent needs equal opportunity
            if (opponentRollCount < currentRollCount && !PlayerHasStood)
            {
                Debug.Log($"{(isPlayer ? "House" : "Player")} needs equal opportunity - giving another turn");
                WaitingForEqualOpportunity = true;
                return RoundResult.Continue;
            }
            
            // Round ends - current player wins
            return isPlayer ? RoundResult.PlayerWins : RoundResult.HouseWins;
        }
        
        // Check if equal opportunity has been satisfied
        if (WaitingForEqualOpportunity)
        {
            bool equalityAchieved = isPlayer ? 
                (PlayerRollRows.Count >= HouseRollRows.Count) : 
                (HouseRollRows.Count >= PlayerRollRows.Count);
            
            if (equalityAchieved)
            {
                Debug.Log("Equal opportunity achieved - checking final conditions");
                WaitingForEqualOpportunity = false;
                
                // Determine winner after equal opportunity
                if (HouseRoundTotal > 21 && PlayerRoundTotal > 21)
                {
                    Debug.Log($"Both bust - House: {HouseRoundTotal}, Player: {PlayerRoundTotal}");
                    return RoundResult.HouseWins; // House wins by default if both bust
                }
                else if (HouseRoundTotal > 21)
                {
                    Debug.Log($"House confirmed bust: {HouseRoundTotal}, Player: {PlayerRoundTotal}");
                    return RoundResult.PlayerWins;
                }
                else if (PlayerRoundTotal > 21)
                {
                    Debug.Log($"Player confirmed bust: {PlayerRoundTotal}, House: {HouseRoundTotal}");
                    return RoundResult.HouseWins;
                }
                else if (PlayerRoundTotal == 21)
                {
                    Debug.Log($"Player confirmed 21: {PlayerRoundTotal}");
                    return RoundResult.PlayerWins;
                }
                else if (HouseRoundTotal == 21)
                {
                    Debug.Log($"House confirmed 21: {HouseRoundTotal}");
                    return RoundResult.HouseWins;
                }
            }
        }
        
        // Check if player wins because house is bust after player stood
        if (PlayerHasStood && HouseRoundTotal > 21 && PlayerRoundTotal <= 21)
        {
            Debug.Log($"[PLAYER WINS] House is bust ({HouseRoundTotal}), Player stood at {PlayerRoundTotal}");
            return RoundResult.PlayerWins;
        }
        
        // Check if house has won after player stood
        if (PlayerHasStood && !isPlayer && HouseRoundTotal <= 21 && PlayerRoundTotal <= 21)
        {
            if (HouseRoundTotal >= PlayerRoundTotal)
            {
                Debug.Log($"House wins with {HouseRoundTotal} vs Player's {PlayerRoundTotal}");
                return RoundResult.HouseWins;
            }
        }
        
        return RoundResult.Continue;
    }
    
    #endregion
}
