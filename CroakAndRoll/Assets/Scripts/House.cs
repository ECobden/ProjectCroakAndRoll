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

    void Start()
    {
        gameManager = FindFirstObjectByType<DB_GameManager>();
        currentMoney = startingMoney;
        UpdateMoneyUI();
    }

    public void OnTurnStart()
    {
        turnValue = 0;
        lastRollValue = 0;
        UpdateTurnValueUI();
        
        // Get player's final score as target
        Player player = FindFirstObjectByType<Player>();
        targetValue = player != null ? player.GetTurnValue() : 0;
        
        Debug.Log($"House turn started - Must beat {targetValue}");
        StartCoroutine(AutoRollAfterDelay());
    }

    private IEnumerator AutoRollAfterDelay()
    {
        yield return new WaitForSeconds(autoRollDelay);
        RollDice();
    }

    public void RollDice()
    {
        if (gameManager == null || gameManager.IsDiceRolling()) return;
        
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
            OnBust();
        }
        else if (turnValue >= targetValue)
        {
            Debug.Log($"House wins with {turnValue} (matched or beat player's {targetValue})");
            OnWin();
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

    public int GetTurnValue()
    {
        return turnValue;
    }

    private void OnWin()
    {
        // Move dice back to idle positions
        if (gameManager != null)
        {
            gameManager.RefreshDiceIdlePositions();
            gameManager.HouseWins();
        }
    }

    private void OnBust()
    {
        // Move dice back to idle positions
        if (gameManager != null)
        {
            gameManager.RefreshDiceIdlePositions();
            gameManager.HouseBust();
        }
    }

    private void UpdateTurnValueUI()
    {
        if (gameManager != null)
        {
            gameManager.UpdateScoreText(turnValue);
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
