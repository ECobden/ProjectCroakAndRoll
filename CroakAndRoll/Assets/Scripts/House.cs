using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class House : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("Roll Settings")]
    [SerializeField] private float autoRollDelay = 1f;

    [Header("Money System")]
    [SerializeField] private int startingMoney = 1000;
    [SerializeField] private float winMultiplier = 1.5f;
    private int currentMoney;

    [Header("Manager References")]
    [SerializeField] private DB_GameManager gameManager;
    [SerializeField] private DB_DiceManager diceManager;
    [SerializeField] private DB_UIManager uiManager;

    [Header("Turn State")]
    private int turnValue = 0;
    private int lastRollValue = 0;
    private int lastDiceA = 0;
    private int lastDiceB = 0;
    private int targetValue = 0;
    
    [Header("AI Settings")]
    [SerializeField] [Range(0f, 1f)] private float cautiousness = 0.7f; // How risk-averse (0=reckless, 1=very cautious)
    [SerializeField] private int safeThreshold = 17; // Total at which house becomes more cautious

    private void Awake()
    {
        // Validate references
        if (gameManager == null) Debug.LogError("GameManager not assigned to House!");
        if (diceManager == null) Debug.LogError("DiceManager not assigned to House!");
        if (uiManager == null) Debug.LogError("UIManager not assigned to House!");
    }

    void Start()
    {
        currentMoney = startingMoney;
        UpdateMoneyUI();
    }

    public void OnRoundStart()
    {
        turnValue = 0;
        lastRollValue = 0;
        lastDiceA = 0;
        lastDiceB = 0;
        
        // Reset roll progress at round start
        if (uiManager != null)
        {
            uiManager.ResetGoalRollProgress();
        }
        
        // Target value is not used in alternating mode
        targetValue = 0;
        
        Debug.Log($"[ROUND START] House ready - Must beat {targetValue}");
        
        if (gameManager == null)
        {
            Debug.LogError("GameManager is null in House.OnRoundStart! Cannot proceed.");
            return;
        }
        
        StartCoroutine(AutoRollAfterDelay());
    }

    private IEnumerator AutoRollAfterDelay()
    {
        Debug.Log($"House will roll after {autoRollDelay} seconds");
        yield return new WaitForSeconds(autoRollDelay);
        Debug.Log("House is now rolling dice");
        RollDice();
    }

    public void RollDice()
    {
        if (gameManager == null)
        {
            Debug.LogError("GameManager is null in House.RollDice!");
            return;
        }
        
        // Silently skip if dice are already rolling - this is normal during overlapping coroutines
        if (gameManager.IsDiceRolling())
            return;
        
        // In alternating mode, use AI to decide whether to roll or stand
        if (gameManager.GetCurrentState() == DB_GameManager.GameState.AlternatingTurns)
        {
            if (ShouldHouseStand())
            {
                Debug.Log($"[HOUSE AI] Deciding to STAND with {GetTurnValue()}");
                Stand();
                return;
            }
            
            Debug.Log($"[HOUSE AI] Deciding to ROLL (current: {GetTurnValue()}, target: {GetPlayerScore()})");
        }
        
        Debug.Log("House calling RollSharedDice");
        gameManager.RollSharedDice(OnDiceRolled, false); // false = house turn
    }

    private void OnDiceRolled(int diceAValue, int diceBValue)
    {
        lastDiceA = diceAValue;
        lastDiceB = diceBValue;
        lastRollValue = diceAValue + diceBValue;
        
        // Handle roll through game manager for alternating mode
        if (gameManager != null)
        {
            gameManager.OnAlternatingRoll(diceAValue, diceBValue, false);
        }
    }

    public int GetTurnValue()
    {
        return turnValue;
    }

    private void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"${currentMoney}";
        }
    }

    public int GetCurrentMoney()
    {
        return currentMoney;
    }

    public int ReceiveBet(int betAmount)
    {
        currentMoney += betAmount;
        UpdateMoneyUI();
        Debug.Log($"House received bet of {betAmount}. Total money: {currentMoney}");
        return currentMoney;
    }

    public int PayWinnings(int betAmount)
    {
        // Total payout is bet + winnings
        int winnings = Mathf.RoundToInt(betAmount * winMultiplier);
        int totalPayout = betAmount + winnings;
        
        // Check if house has enough money
        if (currentMoney < totalPayout)
        {
            totalPayout = currentMoney;
            currentMoney = 0;
            Debug.Log($"House paying all remaining money: {totalPayout}");
        }
        else
        {
            currentMoney -= totalPayout;
            Debug.Log($"House paying {totalPayout} (bet {betAmount} + winnings {winnings}). Remaining: {currentMoney}");
        }
        
        UpdateMoneyUI();
        return totalPayout;
    }

    public void ResetMoney()
    {
        currentMoney = startingMoney;
        UpdateMoneyUI();
    }

    public void ResetTurnValue()
    {
        turnValue = 0;
        lastRollValue = 0;
        lastDiceA = 0;
        lastDiceB = 0;
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
    
    /// <summary>
    /// House decides to stand
    /// </summary>
    private void Stand()
    {
        Debug.Log($"[HOUSE STAND] House stands with {GetTurnValue()}");
        
        if (gameManager != null)
        {
            // In alternating mode, notify game manager to check scores and determine winner
            if (gameManager.GetCurrentState() == DB_GameManager.GameState.AlternatingTurns)
            {
                gameManager.OnHouseStandInAlternating();
            }
        }
    }
    
    #endregion
}
