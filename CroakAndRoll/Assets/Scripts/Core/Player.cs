using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Player entity - inherits common game logic from Participant base class.
/// Listens for user input and passes commands to GameManager.
/// </summary>
public class Player : Participant
{
    [Header("UI Elements")]
    [SerializeField] private UI_MoneyController moneyController;

    [Header("Turn State")]
    private int turnValue = 0;
    private bool hasRolledThisTurn = false;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        
        if (moneyController != null)
            moneyController.SetMoneyValue(currentMoney);
    }

    void Update()
    {
        if (!canAct || gameManager == null) return;
    }

    public override void RollDice()
    {
        if (!canAct || diceManager == null || diceManager.IsDiceRolling()) return;

        int drawnCount = ConsumeRoundDiceForRoll(2, out List<DieData> selectedDice);
        if (drawnCount == 0)
        {
            Debug.Log("[PLAYER] No dice available in round pool. Auto-standing.");
            Stand();
            return;
        }
        
        StartCoroutine(diceManager.RollDiceAndGetResults(OnDiceRolled, true, selectedDice)); // true = player turn
    }

    private void OnDiceRolled(int diceAValue, int diceBValue)
    {
        // Handle roll through game manager for alternating mode
        if (gameManager != null)
        {
            hasRolledThisTurn = true;
            RecordRoll(diceAValue, diceBValue);
            ExecuteLastRollAbilities(diceAValue, diceBValue, gameManager.GetHouseParticipant());
            gameManager.OnParticipantRolled(true, diceAValue, diceBValue);
        }
    }

    public override void OnRoundStart()
    {
        turnValue = 0;
        rollCount = 0;
        canAct = true;
        hasRolledThisTurn = false;
        InitializeRoundDiceAvailability();
        
        // Hide stand value UI and reset progress at start of new round
        if (uiManager != null)
        {
            uiManager.HideStandValue();
            uiManager.ResetGoalRollProgress();
            // Note: Goal text will be updated by GameManager's state transition
        }
        
        Debug.Log("[ROUND START] Player ready - Roll or Stand. Target: Get close to 21!");
        
        // Execute OnRoundStart abilities
        ExecuteOnRoundStartAbilities(gameManager.GetHouseParticipant());
    }

    public int GetTurnValue()
    {
        return turnValue;
    }
    
    public new int GetRollCount()
    {
        return rollCount;
    }

    public bool HasRolledThisTurn()
    {
        return hasRolledThisTurn;
    }

    public override void Stand()
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

        SetHasStood(true);
        gameManager.OnParticipantStood(true);
        
    }

    private void UpdateMoneyUI()
    {
        if (moneyController != null)
        {
            moneyController.SetMoneyValue(currentMoney);
        }
    }

    public new int GetCurrentMoney()
    {
        return currentMoney;
    }

    public override void AddMoney(int amount)
    {
        base.AddMoney(amount);
        UpdateMoneyUI();
    }

    public override bool SpendMoney(int amount)
    {
        bool spent = base.SpendMoney(amount);
        if (spent)
            UpdateMoneyUI();
        return spent;
    }

    public override void ResetMoney()
    {
        base.ResetMoney();
        UpdateMoneyUI();
    }
}
