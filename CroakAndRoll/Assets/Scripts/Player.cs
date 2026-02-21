using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private UI_MoneyController moneyController;

    [Header("Money System")]
    [SerializeField] private int startingMoney = 1000;
    private int currentMoney;

    [Header("Manager References")]
    [SerializeField] private DB_GameManager gameManager;
    [SerializeField] private DB_DiceManager diceManager;
    [SerializeField] private DB_UIManager uiManager;

    [Header("Turn State")]
    private int turnValue = 0;
    private int rollCount = 0;
    private bool canAct = false;
    private bool hasRolledThisTurn = false;

    private void Awake()
    {
        // Validate references
        if (gameManager == null) Debug.LogError("GameManager not assigned to Player!");
        if (diceManager == null) Debug.LogError("DiceManager not assigned to Player!");
        if (uiManager == null) Debug.LogError("UIManager not assigned to Player!");
    }

    void Start()
    {
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
        
        // Check if we're in rule decision mode
        if (gameManager.IsWaitingForPlayerRuleDecision())
        {
            gameManager.OnPlayerEndTurnDuringRuleDecision();
            return;
        }
        
        gameManager.RollSharedDice(OnDiceRolled, true); // true = player turn
    }

    private void OnDiceRolled(int diceAValue, int diceBValue)
    {
        // Handle roll through game manager for alternating mode
        if (gameManager != null)
        {
            gameManager.OnAlternatingRoll(diceAValue, diceBValue, true);
            hasRolledThisTurn = true;
        }
    }

    public void OnRoundStart(int selectedBetAmount)
    {
        turnValue = 0;
        rollCount = 0;
        canAct = true;
        hasRolledThisTurn = false;
        
        // Hide stand value UI and reset progress at start of new round
        if (uiManager != null)
        {
            uiManager.HideStandValue();
            uiManager.ResetGoalRollProgress();
            // Note: Goal text will be updated by GameManager's state transition
        }
        
        Debug.Log("[ROUND START] Player ready - Roll or Stand. Target: Get close to 21!");
    }

    public int GetTurnValue()
    {
        return turnValue;
    }
    
    public int GetRollCount()
    {
        return rollCount;
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

        if (gameManager != null)
        {
            gameManager.OnPlayerStandInAlternating();
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
