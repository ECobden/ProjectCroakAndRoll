using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// House (Opponent) entity - inherits common game logic from Participant base class.
/// Contains AI logic to make decisions whether to roll or stand.
/// </summary>
public class House : Participant
{
    [Header("Roll Settings")]
    [SerializeField] private float autoRollDelay = 1f;

    [Header("Turn State")]
    private int turnValue = 0;
    private int lastRollValue = 0;
    private int lastDiceA = 0;
    private int lastDiceB = 0;
    private int targetValue = 0;
    
    [Header("AI Settings")]
    [SerializeField] [Range(0f, 1f)] private float cautiousness = 0.7f; // How risk-averse (0=reckless, 1=very cautious)
    [SerializeField] private int safeThreshold = 17; // Total at which house becomes more cautious

    [Header("Stand Value")]
    [SerializeField] private int defaultStandValue = 17; // Default value the house must stand on
    private int currentStandValue = 17; // Current round's stand value

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
    }

    public override void OnRoundStart()
    {
        turnValue = 0;
        lastRollValue = 0;
        lastDiceA = 0;
        lastDiceB = 0;
        currentStandValue = defaultStandValue;
        
        // Reset roll progress at round start
        if (uiManager != null)
        {
            uiManager.ResetGoalRollProgress();
        }
        
        // Target value is not used in alternating mode
        targetValue = 0;
        
        Debug.Log($"[ROUND START] House ready - Stand value: {currentStandValue}");
        
        if (gameManager == null)
        {
            Debug.LogError("GameManager is null in House.OnRoundStart! Cannot proceed.");
            return;
        }
    }

    public void BeginTurn()
    {
        if (!HasStood())
        {
            StartCoroutine(AutoRollAfterDelay());
        }
    }

    private IEnumerator AutoRollAfterDelay()
    {
        Debug.Log($"House will roll after {autoRollDelay} seconds");
        yield return new WaitForSeconds(autoRollDelay);
        Debug.Log("House is now rolling dice");
        RollDice();
    }

    public override void RollDice()
    {
        if (gameManager == null)
        {
            Debug.LogError("GameManager is null in House.RollDice!");
            return;
        }
        
        // Silently skip if dice are already rolling - this is normal during overlapping coroutines
        if (diceManager != null && diceManager.IsDiceRolling())
            return;
        
        // use AI to decide whether to roll or stand
        if (gameManager.GetCurrentState() == DB_GameManager.GameState.PlayRound)
        {
            if (ShouldHouseStand())
            {
                Debug.Log($"[HOUSE AI] Deciding to STAND with {GetTurnValue()}");
                Stand();
                return;
            }
            
            Debug.Log($"[HOUSE AI] Deciding to ROLL (current: {GetTurnValue()}, target: {GetPlayerScore()})");
        }
        
        Debug.Log("House calling diceManager.RollDiceAndGetResults");
        StartCoroutine(diceManager.RollDiceAndGetResults(OnDiceRolled, false)); // false = house turn
    }

    private void OnDiceRolled(int diceAValue, int diceBValue)
    {
        lastDiceA = diceAValue;
        lastDiceB = diceBValue;
        lastRollValue = diceAValue + diceBValue;
        
        // Handle roll through game manager for alternating mode
        if (gameManager != null)
        {

            RecordRoll(diceAValue, diceBValue);
            gameManager.OnParticipantRolled(false, diceAValue, diceBValue);
        }
    }

    public int GetTurnValue()
    {
        return turnValue;
    }

    public void ResetTurnValue()
    {
        turnValue = 0;
        lastRollValue = 0;
        lastDiceA = 0;
        lastDiceB = 0;
    }

    /// <summary>
    /// Set the stand value for this round. This is the threshold at which the house must stand.
    /// </summary>
    public void SetStandValue(int newStandValue)
    {
        currentStandValue = newStandValue;
        Debug.Log($"[HOUSE] Stand value set to {currentStandValue}");
    }

    /// <summary>
    /// Get the current stand value.
    /// </summary>
    public int GetStandValue()
    {
        return currentStandValue;
    }
    
    #region AI Decision Making
    
    /// <summary>
    /// AI logic to decide if house should stand based on risk assessment
    /// </summary>
    private bool ShouldHouseStand()
    {
        if (gameManager == null) return false;
        
        int houseTotal = GetTurnValue();
        int playerTotal = GetPlayerScore();
        
        // Always roll if we have nothing
        if (houseTotal == 0) return false;

        // Mandatory stand at stand value threshold
        if (houseTotal >= currentStandValue)
        {
            Debug.Log($"[HOUSE AI] Reached stand value threshold ({houseTotal} >= {currentStandValue}) - MUST STAND");
            return true;
        }
        
        // Already won - stand
        if (houseTotal >= playerTotal && houseTotal <= 21)
        {
            Debug.Log($"[HOUSE AI] Already beating player ({houseTotal} >= {playerTotal}) - STAND");
            return true;
        }
        
        // Can't possibly win without rolling
        if (houseTotal < playerTotal)
        {
            // Calculate bust probability
            float bustProbability = CalculateBustProbability(houseTotal);
            
            // Calculate win probability (need to beat player without busting)
            float winProbability = CalculateWinProbability(houseTotal, playerTotal);
            
            Debug.Log($"[HOUSE AI] Analysis - Bust: {bustProbability:P0}, Win: {winProbability:P0}");
            
            // Decision based on risk tolerance
            // If bust probability is too high relative to cautiousness, stand
            if (bustProbability > (1f - cautiousness) && houseTotal >= safeThreshold)
            {
                Debug.Log($"[HOUSE AI] Too risky to continue (bust prob: {bustProbability:P0}) - STAND");
                return true;
            }
            
            // If we're close and the gap is small, be more cautious
            int gap = playerTotal - houseTotal;
            if (houseTotal >= safeThreshold && gap <= 3 && bustProbability > 0.5f)
            {
                Debug.Log($"[HOUSE AI] Close to player, high risk, standing - STAND");
                return true;
            }
            
            // Otherwise, take the risk and roll
            return false;
        }
        
        // Default: stand if at safe threshold or above
        return houseTotal >= safeThreshold;
    }
    
    /// <summary>
    /// Calculate probability of busting on next roll
    /// </summary>
    private float CalculateBustProbability(int currentTotal)
    {
        if (currentTotal >= 21) return 1f;
        if (currentTotal <= 10) return 0f; // Can't bust with lowest roll (2)
        
        int maxSafeValue = 21 - currentTotal;
        
        // Count how many dice combinations would bust
        // Possible rolls: 2-12 (36 combinations total)
        int bustCombinations = 0;
        int totalCombinations = 0;
        
        for (int diceA = 1; diceA <= 6; diceA++)
        {
            for (int diceB = 1; diceB <= 6; diceB++)
            {
                totalCombinations++;
                if (diceA + diceB > maxSafeValue)
                {
                    bustCombinations++;
                }
            }
        }
        
        return (float)bustCombinations / totalCombinations;
    }
    
    /// <summary>
    /// Calculate probability of winning (beating player without busting)
    /// </summary>
    private float CalculateWinProbability(int currentTotal, int playerTotal)
    {
        if (currentTotal >= playerTotal && currentTotal <= 21) return 1f; // Already winning
        if (currentTotal > 21) return 0f; // Already bust
        
        int neededMin = playerTotal - currentTotal + 1; // Minimum to beat player
        int maxSafe = 21 - currentTotal; // Maximum without busting
        
        if (neededMin > maxSafe) return 0f; // Can't win without busting
        
        // Count combinations that would win
        int winCombinations = 0;
        int totalCombinations = 0;
        
        for (int diceA = 1; diceA <= 6; diceA++)
        {
            for (int diceB = 1; diceB <= 6; diceB++)
            {
                totalCombinations++;
                int rollTotal = diceA + diceB;
                int newTotal = currentTotal + rollTotal;
                
                // Wins if: beats player and doesn't bust
                if (newTotal >= playerTotal && newTotal <= 21)
                {
                    winCombinations++;
                }
            }
        }
        
        return (float)winCombinations / totalCombinations;
    }
    
    /// <summary>
    /// Get the player's current score
    /// </summary>
    private int GetPlayerScore()
    {
        if (gameManager == null) return 0;
        
        // Try to get from alternating round manager first
        int playerScore = gameManager.GetPlayerRoundTotal();
        if (playerScore > 0) return playerScore;
        
        // Fallback to legacy target value
        return targetValue;
    }
    
    #endregion
    
    public override void Stand()
    {
        Debug.Log($"[HOUSE STAND] House stands with {GetTurnValue()}");
        SetHasStood(true);
        gameManager.OnParticipantStood(false);
    }
}
