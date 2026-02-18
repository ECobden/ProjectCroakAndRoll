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

    [Header("Turn State")]
    private int turnValue = 0;
    private int lastRollValue = 0;
    private int rollCount = 0;
    private bool canAct = false;
    private bool hasRolledThisTurn = false;
    private DB_GameManager gameManager;
    private DB_DiceManager diceManager;
    private DB_UIManager uiManager;
    
    [Header("Perks")]
    private List<Perk> activePerks = new List<Perk>();

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
        // Check if we're in alternating turn mode
        if (gameManager != null && gameManager.GetCurrentState() == DB_GameManager.GameState.AlternatingTurns)
        {
            // Handle roll through game manager for alternating mode
            gameManager.OnAlternatingRoll(diceAValue, diceBValue, true);
            hasRolledThisTurn = true;
            return;
        }
        
        // Legacy single-turn mode below
        // Increment roll count
        rollCount++;
        
        // Allow perks to modify dice values before adding to turn total
        int modifiedDiceA = diceAValue;
        int modifiedDiceB = diceBValue;
        
        foreach (var perk in activePerks)
        {
            (modifiedDiceA, modifiedDiceB) = perk.ModifyDiceValues(this, modifiedDiceA, modifiedDiceB, turnValue);
        }
        
        lastRollValue = modifiedDiceA + modifiedDiceB;
        turnValue += lastRollValue;
        hasRolledThisTurn = true;

        Debug.Log($"Player rolled: {diceAValue} + {diceBValue} = {diceAValue + diceBValue}");
        if (modifiedDiceA != diceAValue || modifiedDiceB != diceBValue)
        {
            Debug.Log($"Modified by perks to: {modifiedDiceA} + {modifiedDiceB} = {lastRollValue}");
        }
        Debug.Log($"Turn total: {turnValue}");
        
        // Trigger perk hooks for individual dice (use original values)
        foreach (var perk in activePerks)
        {
            perk.OnDiceRolled(this, diceAValue, true);  // Dice A
            perk.OnDiceRolled(this, diceBValue, false); // Dice B
        }

        UpdateTurnValueUI();

        // Check for bust
        if (turnValue > 21)
        {
            canAct = false;
            Debug.Log("Player BUST! Turn value exceeded 21.");
            
            // Show bust message
            if (uiManager != null)
                uiManager.ShowPlayerBust();
            
            // Disable buttons on bust
            if (gameManager != null)
                gameManager.DisableGameplayButtons();
            
            // Delay before ending turn to let UI animation finish
            StartCoroutine(DelayedBust());
        }
        else if (turnValue == 21)
        {
            canAct = false;
            Debug.Log("Player hit 21! Perfect score - Instant Win!");
            
            // Show 21 message
            if (uiManager != null)
                uiManager.ShowPlayer21();
            
            // Disable buttons
            if (gameManager != null)
                gameManager.DisableGameplayButtons();
            
            // Delay before winning to let UI animation finish
            StartCoroutine(DelayedWinWith21());
        }
    }

    private IEnumerator DelayedBust()
    {
        // Wait for score animation to complete
        float animationDuration = uiManager != null ? uiManager.GetScoreAnimationDuration(lastRollValue) : 0.8f;
        yield return new WaitForSeconds(animationDuration);
        OnBust();
    }

    private IEnumerator DelayedStand()
    {
        // Wait for score animation to complete
        float animationDuration = uiManager != null ? uiManager.GetScoreAnimationDuration(lastRollValue) : 0.8f;
        yield return new WaitForSeconds(animationDuration);
        Stand();
    }
    
    private IEnumerator DelayedWinWith21()
    {
        // Wait for score animation to complete
        float animationDuration = uiManager != null ? uiManager.GetScoreAnimationDuration(lastRollValue) : 0.8f;
        yield return new WaitForSeconds(animationDuration);
        OnWinWith21();
    }
    
    private void OnWinWith21()
    {
        // Move dice back to idle positions
        if (diceManager != null)
            diceManager.RefreshDiceIdlePositions();
        
        // Show stand value
        if (uiManager != null)
            uiManager.ShowStandValue($"{turnValue}");
        
        // Trigger instant win
        if (gameManager != null)
        {
            gameManager.PlayerWinsWith21();
        }
    }

    public void OnRoundStart(int selectedBetAmount)
    {
        turnValue = 0;
        lastRollValue = 0;
        rollCount = 0;
        canAct = true;
        hasRolledThisTurn = false;
        
        // Trigger perk hooks
        foreach (var perk in activePerks)
        {
            perk.OnTurnStart(this);
        }
        
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

        // Check if we're in alternating turn mode
        if (gameManager != null && gameManager.GetCurrentState() == DB_GameManager.GameState.AlternatingTurns)
        {
            gameManager.OnPlayerStandInAlternating();
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
        
        // Trigger perk hooks
        foreach (var perk in activePerks)
        {
            perk.OnStand(this);
        }
        
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
        // Trigger perk hooks
        foreach (var perk in activePerks)
        {
            perk.OnBust(this);
        }
        
        if (gameManager != null)
        {
            gameManager.PlayerBust();
        }
    }

    private void UpdateTurnValueUI()
    {
        if (uiManager != null)
        {
            // Only update the floating score animation
            // The goal text will be updated during the score transfer animation
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
    
    #region Perk Management
    
    /// <summary>
    /// Add a perk to the player
    /// </summary>
    public void AddPerk(Perk perk)
    {
        if (perk == null) return;
        
        activePerks.Add(perk);
        perk.OnPerkAdded(this);
        Debug.Log($"Player acquired perk: {perk.perkName}");
    }
    
    /// <summary>
    /// Get all active perks
    /// </summary>
    public List<Perk> GetActivePerks()
    {
        return new List<Perk>(activePerks);
    }
    
    /// <summary>
    /// Check if player has a specific perk type
    /// </summary>
    public bool HasPerk<T>() where T : Perk
    {
        return activePerks.Exists(p => p is T);
    }
    
    /// <summary>
    /// Remove all perks (for reset)
    /// </summary>
    public void ClearPerks()
    {
        activePerks.Clear();
    }
    
    /// <summary>
    /// Trigger instant win from 5 and Under perk
    /// </summary>
    public void TriggerFiveAndUnderWin()
    {
        canAct = false;
        
        // Disable buttons
        if (gameManager != null)
            gameManager.DisableGameplayButtons();
        
        // Show stand value
        if (uiManager != null)
            uiManager.ShowStandValue($"{turnValue}");
        
        // Move dice back to idle
        if (diceManager != null)
            diceManager.RefreshDiceIdlePositions();
        
        // Trigger win
        if (gameManager != null)
        {
            gameManager.PlayerWinsWith21(); // Reusing this method for instant win
        }
    }
    
    /// <summary>
    /// Trigger dice flip animation (used by Dice Flip perk)
    /// </summary>
    public void TriggerDiceFlipAnimation(int newDiceAValue, int newDiceBValue)
    {
        // Trigger the visual dice flip animation through the dice manager
        if (diceManager != null)
        {
            // Determine which dice changed and flip accordingly
            int currentA = diceManager.GetDiceAValue();
            int currentB = diceManager.GetDiceBValue();
            
            if (currentA != newDiceAValue && currentB != newDiceBValue)
            {
                // Both dice flipped
                diceManager.FlipBothDice(newDiceAValue, newDiceBValue);
            }
            else if (currentA != newDiceAValue)
            {
                // Only dice A flipped
                diceManager.FlipDiceA(newDiceAValue);
            }
            else if (currentB != newDiceBValue)
            {
                // Only dice B flipped
                diceManager.FlipDiceB(newDiceBValue);
            }
        }
    }
    
    #endregion

}
