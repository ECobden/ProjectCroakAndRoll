using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class HouseCheat
{
    public enum CheatType
    {
        TableSlam,      // Flip dice to opposite values
        LoadedDice,     // Force specific values
        SecondChance,   // Reroll if busting
        PerfectCount    // Always know exact value needed
    }
    
    public CheatType cheatType;
    public int requiredHeatLevel = 1;
    public bool enabled = true;
    
    [Header("Cheat-Specific Settings")]
    [Tooltip("For LoadedDice: preferred value to load (if possible)")]
    public int preferredValue = 6;
}

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

    [Header("Turn State")]
    private int turnValue = 0;
    private int lastRollValue = 0;
    private int lastDiceA = 0;
    private int lastDiceB = 0;
    private int targetValue = 0;
    private DB_GameManager gameManager;
    private DB_DiceManager diceManager;
    private DB_UIManager uiManager;
    
    [Header("AI Settings")]
    [SerializeField] [Range(0f, 1f)] private float cautiousness = 0.7f; // How risk-averse (0=reckless, 1=very cautious)
    [SerializeField] private int safeThreshold = 17; // Total at which house becomes more cautious
    
    [Header("Cheat System")]
    [SerializeField] private bool enableCheats = false;  // DISABLED FOR TESTING
    [SerializeField] private List<HouseCheat> availableCheats = new List<HouseCheat>();
    [SerializeField] private AudioClip tableSlamSound;
    private HashSet<HouseCheat.CheatType> usedCheatTypes = new HashSet<HouseCheat.CheatType>();

    void Start()
    {
        gameManager = FindFirstObjectByType<DB_GameManager>();
        diceManager = FindFirstObjectByType<DB_DiceManager>();
        uiManager = FindFirstObjectByType<DB_UIManager>();
        currentMoney = startingMoney;
        UpdateMoneyUI();
    }

    public void OnRoundStart()
    {
        turnValue = 0;
        lastRollValue = 0;
        lastDiceA = 0;
        lastDiceB = 0;
        usedCheatTypes.Clear();
        
        // Reset roll progress at round start
        if (uiManager != null)
        {
            uiManager.ResetGoalRollProgress();
        }
        
        // Get player's final score as target (legacy mode only)
        Player player = FindFirstObjectByType<Player>();
        targetValue = player != null ? player.GetTurnValue() : 0;
        
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
        
        // Check if we're in alternating turn mode
        if (gameManager != null && gameManager.GetCurrentState() == DB_GameManager.GameState.AlternatingTurns)
        {
            // Handle roll through game manager for alternating mode
            gameManager.OnAlternatingRoll(diceAValue, diceBValue, false);
            return;
        }
        
        // Legacy single-turn mode below
        turnValue += lastRollValue;

        Debug.Log($"House rolled: {lastRollValue} (Dice: {diceAValue} + {diceBValue}). Turn total: {turnValue}");

        UpdateTurnValueUI();

        // Check win/bust conditions
        if (turnValue > 21)
        {
            // Try to cheat before busting
            if (TryUseCheats())
            {
                Debug.Log("House used cheat to avoid bust!");
                // Recalculate with altered dice
                return; // Exit early - cheating changes the flow
            }
            
            Debug.Log("House BUST! House exceeded 21.");
            
            // Show bust message
            if (uiManager != null)
                uiManager.ShowHouseBust();
            
            // Delay before ending turn to let UI animation finish
            StartCoroutine(DelayedBust());
        }
        else if (turnValue == 21)
        {
            Debug.Log($"House hits 21!");
            
            // Show 21 message
            if (uiManager != null)
                uiManager.ShowHouse21();
            
            // Delay before ending turn to let UI animation finish
            StartCoroutine(DelayedWin());
        }
        else if (turnValue >= targetValue)
        {
            Debug.Log($"House wins with {turnValue} (matched or beat player's {targetValue})");
            
            // Show house wins message
            if (uiManager != null)
                uiManager.ShowHouseWins();
            
            // Delay before ending turn to let UI animation finish
            StartCoroutine(DelayedWin());
        }
        else
        {
            // House must keep rolling
            Debug.Log($"House has {turnValue}, needs to match or beat {targetValue}. Rolling again...");
            StartCoroutine(DelayedRoll());
        }
    }

    private IEnumerator DelayedRoll()
    {
        yield return new WaitForSeconds(autoRollDelay);
        RollDice();
    }

    private IEnumerator DelayedBust()
    {
        // Wait for score animation to complete
        float animationDuration = uiManager != null ? uiManager.GetScoreAnimationDuration(lastRollValue) : 0.8f;
        yield return new WaitForSeconds(animationDuration);
        OnBust();
    }

    private IEnumerator DelayedWin()
    {
        // Wait for score animation to complete
        float animationDuration = uiManager != null ? uiManager.GetScoreAnimationDuration(lastRollValue) : 0.8f;
        yield return new WaitForSeconds(animationDuration);
        OnWin();
    }

    public int GetTurnValue()
    {
        return turnValue;
    }

    private void OnWin()
    {
        // Move dice back to idle positions
        if (diceManager != null)
            diceManager.RefreshDiceIdlePositions();
        
        if (gameManager != null)
            gameManager.HouseWins();
    }

    private void OnBust()
    {
        // Move dice back to idle positions
        if (diceManager != null)
            diceManager.RefreshDiceIdlePositions();
        
        // Show player wins message
        if (uiManager != null)
            uiManager.ShowPlayerWins();
        
        if (gameManager != null)
            gameManager.HouseBust();
    }

    private void UpdateTurnValueUI()
    {
        if (uiManager != null)
        {
            // Only update the floating score animation
            // The goal text will be updated during the score transfer animation
            uiManager.UpdateScoreText(turnValue, false); // false = house turn
        }
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
        usedCheatTypes.Clear();
        UpdateTurnValueUI();
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
    
    #region Cheat System
    
    /// <summary>
    /// Get opposite face value of a die (1↔6, 2↔5, 3↔4)
    /// </summary>
    private int GetOppositeFace(int faceValue)
    {
        return 7 - faceValue;
    }
    
    /// <summary>
    /// Try to use any available cheat
    /// </summary>
    private bool TryUseCheats()
    {
        if (!enableCheats || gameManager == null)
            return false;
        
        // Try each available cheat in order
        foreach (var cheat in availableCheats)
        {
            if (!cheat.enabled)
                continue;
            
            // Check if this cheat type was already used this round
            if (usedCheatTypes.Contains(cheat.cheatType))
                continue;
            
            // Try to use the cheat
            bool success = false;
            switch (cheat.cheatType)
            {
                case HouseCheat.CheatType.TableSlam:
                    success = TryTableSlamCheat();
                    break;
                    
                case HouseCheat.CheatType.LoadedDice:
                    success = TryLoadedDiceCheat(cheat.preferredValue);
                    break;
                    
                case HouseCheat.CheatType.SecondChance:
                    success = TrySecondChanceCheat();
                    break;
                    
                case HouseCheat.CheatType.PerfectCount:
                    success = TryPerfectCountCheat();
                    break;
            }
            
            if (success)
            {
                usedCheatTypes.Add(cheat.cheatType);
                Debug.Log($"House used {cheat.cheatType} cheat");
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Table Slam Cheat: Flip dice to opposite values to avoid busting
    /// </summary>
    private bool TryTableSlamCheat()
    {
        // We're busting - try to flip dice to avoid it
        int currentTurnValueBeforeRoll = turnValue - lastRollValue;
        
        // Try flipping both dice
        int flippedA = GetOppositeFace(lastDiceA);
        int flippedB = GetOppositeFace(lastDiceB);
        int newTotal = currentTurnValueBeforeRoll + flippedA + flippedB;
        
        if (newTotal <= 21)
        {
            // Both dice flip works!
            Debug.Log($"Table Slam! Flipping both dice: ({lastDiceA},{lastDiceB}) -> ({flippedA},{flippedB})");
            StartCoroutine(ApplyTableSlam(flippedA, flippedB));
            return true;
        }
        
        // Try flipping only dice A
        newTotal = currentTurnValueBeforeRoll + flippedA + lastDiceB;
        if (newTotal <= 21)
        {
            Debug.Log($"Table Slam! Flipping dice A: {lastDiceA} -> {flippedA}");
            StartCoroutine(ApplyTableSlam(flippedA, lastDiceB));
            return true;
        }
        
        // Try flipping only dice B
        newTotal = currentTurnValueBeforeRoll + lastDiceA + flippedB;
        if (newTotal <= 21)
        {
            Debug.Log($"Table Slam! Flipping dice B: {lastDiceB} -> {flippedB}");
            StartCoroutine(ApplyTableSlam(lastDiceA, flippedB));
            return true;
        }
        
        // No flip combination saves us from busting
        Debug.Log("Table Slam failed - no combination prevents bust");
        return false;
    }
    
    /// <summary>
    /// Apply the table slam cheat with new dice values and flip animation
    /// </summary>
    private IEnumerator ApplyTableSlam(int newDiceA, int newDiceB)
    {
        // Play table slam sound if available
        if (tableSlamSound != null)
        {
            AudioSource.PlayClipAtPoint(tableSlamSound, Camera.main.transform.position);
        }
        
        // Show cheating visual feedback
        if (uiManager != null)
        {
            uiManager.ShowHouseCheated();
        }
        
        // Trigger dice flip animation through dice manager
        if (diceManager != null)
        {
            diceManager.FlipBothDice(newDiceA, newDiceB);
        }
        
        // Wait for flip animation to complete
        yield return new WaitForSeconds(0.5f);
        
        // Update dice values
        lastDiceA = newDiceA;
        lastDiceB = newDiceB;
        int newRollValue = newDiceA + newDiceB;
        
        // Recalculate turn value
        turnValue = turnValue - lastRollValue + newRollValue;
        lastRollValue = newRollValue;
        
        Debug.Log($"After table slam - Roll: {lastRollValue} (Dice: {lastDiceA} + {lastDiceB}). New turn total: {turnValue}");
        
        // Update UI with corrected value
        UpdateTurnValueUI();
        
        // Continue with normal flow - check new conditions
        if (turnValue == 21)
        {
            Debug.Log($"House hits 21 after cheat!");
            
            if (uiManager != null)
                uiManager.ShowHouse21();
            
            StartCoroutine(DelayedWin());
        }
        else if (turnValue >= targetValue)
        {
            Debug.Log($"House wins with {turnValue} after cheat (matched or beat player's {targetValue})");
            
            if (uiManager != null)
                uiManager.ShowHouseWins();
            
            StartCoroutine(DelayedWin());
        }
        else
        {
            // Still need to keep rolling
            Debug.Log($"House has {turnValue} after cheat, needs to match or beat {targetValue}. Rolling again...");
            StartCoroutine(DelayedRoll());
        }
    }
    
    /// <summary>
    /// Loaded Dice Cheat: Force dice to specific values to avoid busting
    /// </summary>
    private bool TryLoadedDiceCheat(int preferredValue)
    {
        int currentTurnValueBeforeRoll = turnValue - lastRollValue;
        int targetNeeded = 21 - currentTurnValueBeforeRoll;
        
        // Try to get as close to target as possible without going over
        int bestDiceA = 1;
        int bestDiceB = 1;
        int bestTotal = 2;
        
        for (int a = 1; a <= 6; a++)
        {
            for (int b = 1; b <= 6; b++)
            {
                int total = a + b;
                if (currentTurnValueBeforeRoll + total <= 21 && total > bestTotal)
                {
                    bestDiceA = a;
                    bestDiceB = b;
                    bestTotal = total;
                }
            }
        }
        
        // If we found a better combination than what we rolled
        if (bestTotal > lastRollValue || currentTurnValueBeforeRoll + lastRollValue > 21)
        {
            Debug.Log($"Loaded Dice! Changing ({lastDiceA},{lastDiceB}) to ({bestDiceA},{bestDiceB})");
            StartCoroutine(ApplyLoadedDice(bestDiceA, bestDiceB));
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Second Chance Cheat: Reroll the dice to avoid busting
    /// </summary>
    private bool TrySecondChanceCheat()
    {
        // Simply reroll - there's a chance we get better values
        Debug.Log($"Second Chance! Rerolling bust ({lastDiceA},{lastDiceB})");
        StartCoroutine(ApplySecondChance());
        return true;
    }
    
    /// <summary>
    /// Perfect Count Cheat: Know exactly what value is needed and get it
    /// </summary>
    private bool TryPerfectCountCheat()
    {
        int currentTurnValueBeforeRoll = turnValue - lastRollValue;
        int exactNeeded = targetValue - currentTurnValueBeforeRoll;
        
        // Can we make this exact value with dice?
        if (exactNeeded >= 2 && exactNeeded <= 12)
        {
            // Find dice combination that makes exact value
            for (int a = 1; a <= 6; a++)
            {
                int b = exactNeeded - a;
                if (b >= 1 && b <= 6)
                {
                    Debug.Log($"Perfect Count! Getting exact value needed: ({a},{b}) = {exactNeeded}");
                    StartCoroutine(ApplyPerfectCount(a, b));
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Apply loaded dice cheat
    /// </summary>
    private IEnumerator ApplyLoadedDice(int newDiceA, int newDiceB)
    {
        if (tableSlamSound != null)
            AudioSource.PlayClipAtPoint(tableSlamSound, Camera.main.transform.position);
        
        if (uiManager != null)
            uiManager.ShowHouseCheated();
        
        if (diceManager != null)
            diceManager.FlipBothDice(newDiceA, newDiceB);
        
        yield return new WaitForSeconds(0.5f);
        
        // Update values and continue
        lastDiceA = newDiceA;
        lastDiceB = newDiceB;
        int newRollValue = newDiceA + newDiceB;
        turnValue = turnValue - lastRollValue + newRollValue;
        lastRollValue = newRollValue;
        
        UpdateTurnValueUI();
        CheckTurnConditions();
    }
    
    /// <summary>
    /// Apply second chance cheat (reroll)
    /// </summary>
    private IEnumerator ApplySecondChance()
    {
        if (uiManager != null)
            uiManager.ShowHouseCheated();
        
        yield return new WaitForSeconds(0.3f);
        
        // Undo the bust roll
        turnValue -= lastRollValue;
        
        // Reroll dice
        RollDice();
    }
    
    /// <summary>
    /// Apply perfect count cheat
    /// </summary>
    private IEnumerator ApplyPerfectCount(int newDiceA, int newDiceB)
    {
        if (tableSlamSound != null)
            AudioSource.PlayClipAtPoint(tableSlamSound, Camera.main.transform.position);
        
        if (uiManager != null)
            uiManager.ShowHouseCheated();
        
        if (diceManager != null)
            diceManager.FlipBothDice(newDiceA, newDiceB);
        
        yield return new WaitForSeconds(0.5f);
        
        // Update values
        lastDiceA = newDiceA;
        lastDiceB = newDiceB;
        int newRollValue = newDiceA + newDiceB;
        turnValue = turnValue - lastRollValue + newRollValue;
        lastRollValue = newRollValue;
        
        UpdateTurnValueUI();
        CheckTurnConditions();
    }
    
    /// <summary>
    /// Check turn conditions after cheat application
    /// </summary>
    private void CheckTurnConditions()
    {
        if (turnValue == 21)
        {
            Debug.Log($"House hits 21 after cheat!");
            if (uiManager != null)
                uiManager.ShowHouse21();
            StartCoroutine(DelayedWin());
        }
        else if (turnValue >= targetValue)
        {
            Debug.Log($"House wins with {turnValue} after cheat (matched or beat player's {targetValue})");
            if (uiManager != null)
                uiManager.ShowHouseWins();
            StartCoroutine(DelayedWin());
        }
        else if (turnValue > 21)
        {
            Debug.Log($"House still busting after cheat!");
            if (uiManager != null)
                uiManager.ShowHouseBust();
            StartCoroutine(DelayedBust());
        }
        else
        {
            Debug.Log($"House has {turnValue} after cheat, needs to match or beat {targetValue}. Rolling again...");
            StartCoroutine(DelayedRoll());
        }
    }
    
    #endregion
}
