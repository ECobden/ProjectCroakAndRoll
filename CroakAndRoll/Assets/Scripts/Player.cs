using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private UI_MoneyController moneyController;

    [Header("Input Settings")]
    [SerializeField] private KeyCode rollKey = KeyCode.Space;
    [SerializeField] private KeyCode standKey = KeyCode.S;

    [Header("Money System")]
    [SerializeField] private int startingMoney = 1000;
    private int betAmount = 100;
    private int currentMoney;

    [Header("Turn State")]
    private int turnValue = 0;
    private int lastRollValue = 0;
    private bool canAct = false;
    private DB_GameManager gameManager;

    void Start()
    {
        gameManager = FindFirstObjectByType<DB_GameManager>();
        currentMoney = startingMoney;
        
        if (moneyController != null)
            moneyController.SetMoneyValue(currentMoney);
    }

    void Update()
    {
        if (!canAct || gameManager == null) return;
    }

    public void RollDice()
    {
        if (!canAct || gameManager == null || gameManager.IsDiceRolling()) return;
        
        gameManager.RollSharedDice(OnDiceRolled, true); // true = player turn
    }

    private void OnDiceRolled(int diceAValue, int diceBValue)
    {
        lastRollValue = diceAValue + diceBValue;
        turnValue += lastRollValue;

        Debug.Log($"Player rolled: {lastRollValue} (Dice: {diceAValue} + {diceBValue}). Turn total: {turnValue}");

        UpdateTurnValueUI();

        // Check for bust
        if (turnValue > 21)
        {
            canAct = false;
            Debug.Log("Player BUST! Turn value exceeded 21.");
            OnBust();
        }
        else if (turnValue == 21)
        {
            Debug.Log("Player hit 21! Perfect score. Auto-standing...");
            Stand();
        }
    }

    public void OnTurnStart(int selectedBetAmount)
    {
        turnValue = 0;
        lastRollValue = 0;
        canAct = true;
        UpdateTurnValueUI();
        
        // Update the bet amount
        betAmount = selectedBetAmount;
        
        // Check if player can afford the bet
        if (currentMoney < betAmount)
        {
            Debug.Log($"Player cannot afford bet of {betAmount}! Game Over!");
            if (gameManager != null)
            {
                gameManager.PlayerOutOfMoney();
            }
            canAct = false;
            return;
        }
        
        // Place bet - deduct from player and add to house immediately
        currentMoney -= betAmount;
        UpdateMoneyUI();
        
        // Transfer bet to house
        House house = FindFirstObjectByType<House>();
        if (house != null)
        {
            house.ReceiveBet(betAmount);
        }
        
        Debug.Log($"Player placed bet of {betAmount}. Remaining money: {currentMoney}");
        Debug.Log("Player turn started - Roll (Space) or Stand (S). Target: Get close to 21 without going over!");
    }

    public int GetTurnValue()
    {
        return turnValue;
    }

    public void Stand()
    {
        if (!canAct || (gameManager != null && gameManager.IsDiceRolling())) return;

        canAct = false;
        Debug.Log($"Player stands with {turnValue}");

        // Move dice back to idle positions
        if (gameManager != null)
        {
            gameManager.RefreshDiceIdlePositions();
            gameManager.EndPlayerTurn();
        }
    }

    private void OnBust()
    {
        if (gameManager != null)
        {
            gameManager.PlayerBust();
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
        if (moneyController != null)
        {
            moneyController.SetMoneyValue(currentMoney);
        }
    }

    public int GetCurrentMoney()
    {
        return currentMoney;
    }

    public int GetBetAmount()
    {
        return betAmount;
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateMoneyUI();
        Debug.Log($"Player received {amount}. Total money: {currentMoney}");
    }

    public void ResetMoney()
    {
        currentMoney = startingMoney;
        UpdateMoneyUI();
    }

}
