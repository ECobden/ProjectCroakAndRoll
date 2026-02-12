using UnityEngine;

/// <summary>
/// Perk: Flip dice to opposite sides to avoid busting (like house cheat)
/// </summary>
[System.Serializable]
public class DiceFlipPerk : Perk
{
    private bool usedThisRound = false;
    
    public DiceFlipPerk()
    {
        perkName = "Dice Flip";
        perkDescription = "Flip dice to opposite sides once per round to avoid bust";
        cost = 300;
    }
    
    public override void OnPerkAdded(Player player)
    {
        Debug.Log($"Perk Added: {perkName}");
    }
    
    public override void OnTurnStart(Player player)
    {
        // Reset usage at start of each turn
        usedThisRound = false;
    }
    
    /// <summary>
    /// Get opposite face value of a die (1↔6, 2↔5, 3↔4)
    /// </summary>
    private int GetOppositeFace(int faceValue)
    {
        return 7 - faceValue;
    }
    
    public override (int diceA, int diceB) ModifyDiceValues(Player player, int diceA, int diceB, int currentTurnValue)
    {
        // Check if already used this round
        if (usedThisRound)
        {
            return (diceA, diceB);
        }
        
        // Check if this roll would cause a bust
        int totalRoll = diceA + diceB;
        int wouldBeTotal = currentTurnValue + totalRoll;
        
        if (wouldBeTotal <= 21)
        {
            // Would not bust, no need to flip
            return (diceA, diceB);
        }
        
        // Would bust - try flipping dice to avoid it
        int flippedA = GetOppositeFace(diceA);
        int flippedB = GetOppositeFace(diceB);
        
        // Try flipping both dice
        int newTotal = currentTurnValue + flippedA + flippedB;
        if (newTotal <= 21)
        {
            Debug.Log($"{perkName} activated! Table Slam! Flipping both dice: ({diceA},{diceB}) -> ({flippedA},{flippedB})");
            usedThisRound = true;
            player.TriggerDiceFlipAnimation(flippedA, flippedB);
            return (flippedA, flippedB);
        }
        
        // Try flipping only dice A
        newTotal = currentTurnValue + flippedA + diceB;
        if (newTotal <= 21)
        {
            Debug.Log($"{perkName} activated! Table Slam! Flipping dice A: {diceA} -> {flippedA}");
            usedThisRound = true;
            player.TriggerDiceFlipAnimation(flippedA, diceB);
            return (flippedA, diceB);
        }
        
        // Try flipping only dice B
        newTotal = currentTurnValue + diceA + flippedB;
        if (newTotal <= 21)
        {
            Debug.Log($"{perkName} activated! Table Slam! Flipping dice B: {diceB} -> {flippedB}");
            usedThisRound = true;
            player.TriggerDiceFlipAnimation(diceA, flippedB);
            return (diceA, flippedB);
        }
        
        // No flip combination saves us from busting
        Debug.Log($"{perkName} failed - no combination prevents bust");
        return (diceA, diceB);
    }
    
    public override Perk Clone()
    {
        return new DiceFlipPerk();
    }
}
