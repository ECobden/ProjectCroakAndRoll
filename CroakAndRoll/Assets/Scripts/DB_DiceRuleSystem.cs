using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles special dice rules: matching (destroy) and ±1 swapping.
/// Separated from GameManager to simplify and modularize rule logic.
/// 
/// Rule 1 - Matching Dice: If your roll matches opponent's dice value(s), you can destroy one
/// Rule 2 - ±1 Swapping: If your roll is ±1 from opponent's last roll, you can swap values
/// </summary>
public class DB_DiceRuleSystem : MonoBehaviour
{
    #region Serialized Fields
    
    [SerializeField] private DB_UIManager uiManager;
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Check what rule actions are available for current roll
    /// </summary>
    /// <param name="diceA">First die value</param>
    /// <param name="diceB">Second die value</param>
    /// <param name="opponentPositioner">Opponent's dice positioner</param>
    /// <returns>Tuple of (matching dice values, swappable dice values)</returns>
    public (List<int> matchingDice, List<int> swappableDice) CheckAvailableRules(
        int diceA, 
        int diceB, 
        ScoredDicePositioner opponentPositioner)
    {
        List<int> matchingDice = new List<int>();
        List<int> swappableDice = new List<int>();
        
        if (opponentPositioner == null) 
            return (matchingDice, swappableDice);
        
        // Get opponent's dice and last row
        List<int> opponentDiceValues = opponentPositioner.GetAllDiceValues();
        var opponentLastRow = opponentPositioner.GetLastRow();
        
        // Rule 1: Matching dice (can destroy)
        if (opponentDiceValues.Contains(diceA))
            matchingDice.Add(diceA);
        if (opponentDiceValues.Contains(diceB) && !matchingDice.Contains(diceB))
            matchingDice.Add(diceB);
        
        // Rule 2: ±1 dice (can swap)
        if (opponentLastRow != null)
        {
            int lastDiceA = opponentLastRow.diceA != null ? opponentLastRow.diceA.GetLastRollValue() : -1;
            int lastDiceB = opponentLastRow.diceB != null ? opponentLastRow.diceB.GetLastRollValue() : -1;
            
            CheckSwappable(diceA, diceB, lastDiceA, swappableDice);
            CheckSwappable(diceA, diceB, lastDiceB, swappableDice);
        }
        
        return (matchingDice, swappableDice);
    }
    
    /// <summary>
    /// Highlight available rule actions for player
    /// </summary>
    public void HighlightAvailableActions(
        List<int> matchingDice, 
        List<int> swappableDice,
        ScoredDicePositioner opponentPositioner,
        System.Action<DB_DiceController> onDieClicked)
    {
        if (opponentPositioner == null) return;
        
        // Highlight matching dice (red) - can be destroyed
        foreach (int value in matchingDice)
        {
            opponentPositioner.HighlightDiceWithValue(value, Color.red, onDieClicked);
        }
        
        // Highlight swappable dice (blue) - opponent's last row that can be swapped
        if (swappableDice.Count > 0)
        {
            var opponentLastRow = opponentPositioner.GetLastRow();
            if (opponentLastRow != null)
            {
                bool highlightA = ShouldHighlight(opponentLastRow.diceA, swappableDice);
                bool highlightB = ShouldHighlight(opponentLastRow.diceB, swappableDice);
                opponentPositioner.HighlightLastRowDice(highlightA, highlightB, Color.blue, onDieClicked);
            }
        }
    }
    
    /// <summary>
    /// Clear all rule highlights
    /// </summary>
    public void ClearHighlights(ScoredDicePositioner positioner)
    {
        if (positioner != null)
            positioner.ClearAllHighlights();
    }
    
    /// <summary>
    /// Execute destroy die action
    /// </summary>
    public void DestroyDie(DB_DiceController die, ScoredDicePositioner positioner, string messagePrefix = "")
    {
        if (die == null || positioner == null) return;
        
        int value = die.GetLastRollValue();
        positioner.RemoveDie(die);
        
        Debug.Log($"{messagePrefix}Destroyed die with value {value}");
        
        if (uiManager != null)
            uiManager.UpdateGoalText($"{messagePrefix}Destroyed {value}!");
    }
    
    /// <summary>
    /// Execute swap dice action - flips both dice to opposite values
    /// </summary>
    public void SwapDice(
        DB_DiceController playerDie,
        DB_DiceController opponentDie,
        List<DB_DiceController> diceBeingFlipped)
    {
        if (playerDie == null || opponentDie == null) return;
        
        int playerValue = playerDie.GetLastRollValue();
        int opponentValue = opponentDie.GetLastRollValue();
        
        Debug.Log($"Swapping {playerValue} <-> {opponentValue}");
        
        // Track dice being flipped for animation completion
        diceBeingFlipped.Clear();
        diceBeingFlipped.Add(playerDie);
        diceBeingFlipped.Add(opponentDie);
        
        // Flip both dice to opposite values
        playerDie.FlipToOppositeFace(opponentValue);
        opponentDie.FlipToOppositeFace(playerValue);
        
        if (uiManager != null)
            uiManager.UpdateGoalText($"Swapped {playerValue} for {opponentValue}!");
    }
    
    /// <summary>
    /// Check if a die can be destroyed (matching rule)
    /// </summary>
    public bool CanDestroyDie(int dieValue, List<int> matchingDice)
    {
        return matchingDice.Contains(dieValue);
    }
    
    /// <summary>
    /// Check if two dice can be swapped (±1 rule)
    /// </summary>
    public bool CanSwapDice(int playerDieValue, int opponentDieValue, List<int> swappableDice)
    {
        return Mathf.Abs(playerDieValue - opponentDieValue) == 1 && swappableDice.Contains(playerDieValue);
    }
    
    /// <summary>
    /// Find which player die can swap with clicked opponent die
    /// </summary>
    public DB_DiceController FindSwappablePlayerDie(
        DB_DiceController clickedOpponentDie,
        ScoredDicePositioner playerPositioner,
        List<int> swappableDice)
    {
        if (clickedOpponentDie == null || playerPositioner == null) return null;
        
        int opponentValue = clickedOpponentDie.GetLastRollValue();
        var playerLastRow = playerPositioner.GetLastRow();
        
        if (playerLastRow == null) return null;
        
        // Check dice A
        if (playerLastRow.diceA != null)
        {
            int playerDiceA = playerLastRow.diceA.GetLastRollValue();
            if (CanSwapDice(playerDiceA, opponentValue, swappableDice))
            {
                return playerLastRow.diceA;
            }
        }
        
        // Check dice B
        if (playerLastRow.diceB != null)
        {
            int playerDiceB = playerLastRow.diceB.GetLastRollValue();
            if (CanSwapDice(playerDiceB, opponentValue, swappableDice))
            {
                return playerLastRow.diceB;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// House AI decision: prioritize destroying high-value dice
    /// </summary>
    public IEnumerator ExecuteHouseAIDecision(
        List<int> matchingDice,
        List<int> swappableDice,
        ScoredDicePositioner playerPositioner)
    {
        yield return new WaitForSeconds(0.5f);
        
        // Prioritize destroying the highest matching die
        if (matchingDice.Count > 0)
        {
            int highestMatch = Mathf.Max(matchingDice.ToArray());
            var dieToDestroy = playerPositioner?.FindDieByValue(highestMatch);
            
            if (dieToDestroy != null)
            {
                DestroyDie(dieToDestroy, playerPositioner, "House ");
                yield return new WaitForSeconds(1f);
            }
        }
        // Could implement swap logic here in the future
        else if (swappableDice.Count > 0)
        {
            Debug.Log("House could swap but chooses not to (AI logic not implemented)");
        }
        
        yield return new WaitForSeconds(0.3f);
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Check if any of our dice are ±1 from target value
    /// </summary>
    private void CheckSwappable(int diceA, int diceB, int targetValue, List<int> swappableDice)
    {
        if (targetValue <= 0) return;
        
        if (Mathf.Abs(diceA - targetValue) == 1 && !swappableDice.Contains(diceA))
            swappableDice.Add(diceA);
        if (Mathf.Abs(diceB - targetValue) == 1 && !swappableDice.Contains(diceB))
            swappableDice.Add(diceB);
    }
    
    /// <summary>
    /// Check if opponent's die should be highlighted for swapping
    /// </summary>
    private bool ShouldHighlight(DB_DiceController die, List<int> swappableDice)
    {
        if (die == null) return false;
        
        int value = die.GetLastRollValue();
        
        // Check if any of our swappable dice are ±1 from this die
        foreach (int swapValue in swappableDice)
        {
            if (Mathf.Abs(swapValue - value) == 1)
                return true;
        }
        
        return false;
    }
    
    #endregion
}
