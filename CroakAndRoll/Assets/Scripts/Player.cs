using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private UI_MoneyController moneyController;

    [Header("Money System")]
    [SerializeField] private int startingMoney = 1000;
    private int betAmount = 100;
    private int currentMoney;

    [Header("Turn State")]
    private int turnValue = 0;
    private int lastRollValue = 0;
    private bool canAct = false;
    private bool hasRolledThisTurn = false;
    private DB_GameManager gameManager;
    private DB_DiceManager diceManager;
    private DB_UIManager uiManager;

    void Start()
    {
        gameManager = FindFirstObjectByType<DB_GameManager>();
        diceManager = FindFirstObjectByType<DB_DiceManager>();
        uiManager = FindFirstObjectByType<DB_UIManager>();
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
        hasRolledThisTurn = true;

        Debug.Log($"Player rolled: {lastRollValue} (Dice: {diceAValue} + {diceBValue}). Turn total: {turnValue}");

        UpdateTurnValueUI();

        // Check for bust
        if (turnValue > 21)
        {
            canAct = false;
            Debug.Log("Player BUST! Turn value exceeded 21.");
            
            // Disable buttons on bust
            if (gameManager != null)
                gameManager.DisableGameplayButtons();
            
            // Delay before ending turn to let UI animation finish
            StartCoroutine(DelayedBust());
        }
        else if (turnValue == 21)
        {
            Debug.Log("Player hit 21! Perfect score. Auto-standing...");
            // Delay before standing to let UI animation finish
            StartCoroutine(DelayedStand());
        }
    }

    private IEnumerator DelayedBust()
    {
        // Wait to allow score callout animation to finish
        yield return new WaitForSeconds(0.8f);
        OnBust();
    }

    private IEnumerator DelayedStand()
    {
        // Wait to allow score callout animation to finish
        yield return new WaitForSeconds(0.8f);
        Stand();
    }

    public void OnTurnStart(int selectedBetAmount)
    {
        turnValue = 0;
        lastRollValue = 0;
        canAct = true;
        hasRolledThisTurn = false;
        
        // Hide stand value UI at start of new turn
        if (uiManager != null)
            uiManager.HideStandValue();
        
        //UpdateTurnValueUI();
        
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

    public bool HasRolledThisTurn()
    {
        return hasRolledThisTurn;
    }

    public void Stand()
    {
        if (!canAct)
        {
            Debug.LogWarning("Stand called but canAct is false. Ignoring.");
            return;
        }
        
        if (gameManager != null && gameManager.IsDiceRolling())
        {
            Debug.LogWarning("Stand called but dice are still rolling. Ignoring.");
            return;
        }

        /* Player must have rolled at least once before standing to prevent accidental stands at the start of the turn.
        THIS CURRENTLY CAUSES ISSUES WITH BUTTONS
        if (!hasRolledThisTurn)
        {
            Debug.LogWarning("Player must roll at least once before standing. Ignoring stand action.");
            return;
        }
        */

        canAct = false;
        Debug.Log($"Player stands with {turnValue}");
        
        // Show stand value UI
        if (uiManager != null)
            uiManager.ShowStandValue($"{turnValue}");
        
        // Disable buttons when standing
        if (gameManager != null)
        {
            gameManager.DisableGameplayButtons();
            
            if (diceManager != null)
                diceManager.RefreshDiceIdlePositions();
            
            gameManager.EndPlayerTurn();
        }
        else
        {
            Debug.LogError("GameManager is null! Cannot end turn.");
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
        if (uiManager != null)
        {
            uiManager.UpdateScoreText(turnValue, true); // true = player turn
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
