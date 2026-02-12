using UnityEngine;
using System.Collections;
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

    [Header("Turn State")]
    private int turnValue = 0;
    private int lastRollValue = 0;
    private int lastDiceA = 0;
    private int lastDiceB = 0;
    private int targetValue = 0;
    private DB_GameManager gameManager;
    private DB_DiceManager diceManager;
    private DB_UIManager uiManager;
    
    [Header("Cheat System")]
    [SerializeField] private bool enableCheats = true;
    [SerializeField] private AudioClip tableSlamSound;
    private bool usedCheatThisRound = false;

    void Start()
    {
        gameManager = FindFirstObjectByType<DB_GameManager>();
        diceManager = FindFirstObjectByType<DB_DiceManager>();
        uiManager = FindFirstObjectByType<DB_UIManager>();
        currentMoney = startingMoney;
        UpdateMoneyUI();
    }

    public void OnTurnStart()
    {
        turnValue = 0;
        lastRollValue = 0;
        lastDiceA = 0;
        lastDiceB = 0;
        usedCheatThisRound = false;
        
        // Reset roll progress for house turn
        if (uiManager != null)
        {
            uiManager.ResetGoalRollProgress();
        }
        
        // Get player's final score as target
        Player player = FindFirstObjectByType<Player>();
        targetValue = player != null ? player.GetTurnValue() : 0;
        
        Debug.Log($"House turn started - Must beat {targetValue}");
        
        if (gameManager == null)
        {
            Debug.LogError("GameManager is null in House.OnTurnStart! Cannot proceed.");
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
        
        if (gameManager.IsDiceRolling())
        {
            Debug.LogWarning("House.RollDice called but dice are already rolling");
            return;
        }
        
        Debug.Log("House calling RollSharedDice");
        gameManager.RollSharedDice(OnDiceRolled, false); // false = house turn
    }

    private void OnDiceRolled(int diceAValue, int diceBValue)
    {
        lastDiceA = diceAValue;
        lastDiceB = diceBValue;
        lastRollValue = diceAValue + diceBValue;
        turnValue += lastRollValue;

        Debug.Log($"House rolled: {lastRollValue} (Dice: {diceAValue} + {diceBValue}). Turn total: {turnValue}");

        UpdateTurnValueUI();

        // Check win/bust conditions
        if (turnValue > 21)
        {
            // Try to cheat before busting
            if (TryTableSlamCheat())
            {
                Debug.Log("House used table slam cheat to avoid bust!");
                // Recalculate with flipped dice
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
        usedCheatThisRound = false;
        UpdateTurnValueUI();
    }
    
    #region Cheat System
    
    /// <summary>
    /// Get opposite face value of a die (1↔6, 2↔5, 3↔4)
    /// </summary>
    private int GetOppositeFace(int faceValue)
    {
        return 7 - faceValue;
    }
    
    /// <summary>
    /// Table Slam Cheat: Flip dice to opposite values to avoid busting
    /// Active at Heat Level 1+
    /// </summary>
    private bool TryTableSlamCheat()
    {
        // Check if cheats are enabled
        if (!enableCheats)
            return false;
        
        // Check if already used cheat this round
        if (usedCheatThisRound)
            return false;
        
        // Check if heat level is high enough (1+)
        if (gameManager == null || gameManager.GetHeatLevel() < 1)
            return false;
        
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
        usedCheatThisRound = true;
        
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
    
    #endregion
}
