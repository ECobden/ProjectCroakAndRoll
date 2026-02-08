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
    private int targetValue = 0;
    private DB_GameManager gameManager;
    private DB_DiceManager diceManager;
    private DB_UIManager uiManager;

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
        lastRollValue = diceAValue + diceBValue;
        turnValue += lastRollValue;

        Debug.Log($"House rolled: {lastRollValue} (Dice: {diceAValue} + {diceBValue}). Turn total: {turnValue}");

        UpdateTurnValueUI();

        // Check win/bust conditions
        if (turnValue > 21)
        {
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
        UpdateTurnValueUI();
    }
}
