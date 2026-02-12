using UnityEngine;

/// <summary>
/// Perk: When rolling a 6, prevent busting by adding only as much value as possible
/// </summary>
[System.Serializable]
public class LuckySixPerk : Perk
{
    public LuckySixPerk()
    {
        perkName = "Lucky Six";
        perkDescription = "Rolling a 6 prevents bust - adds only what fits";
        cost = 200;
    }
    
    public override void OnPerkAdded(Player player)
    {
        Debug.Log($"Perk Added: {perkName}");
    }
    
    public override (int diceA, int diceB) ModifyDiceValues(Player player, int diceA, int diceB, int currentTurnValue)
    {
        // Check if at least one die is a 6
        bool hasSix = (diceA == 6 || diceB == 6);
        
        if (!hasSix)
        {
            // No 6 rolled, no modification
            return (diceA, diceB);
        }
        
        // Check if the roll would cause a bust
        int totalRoll = diceA + diceB;
        int wouldBeTotal = currentTurnValue + totalRoll;
        
        if (wouldBeTotal <= 21)
        {
            // Would not bust, no modification needed
            return (diceA, diceB);
        }
        
        // Would bust with a 6 - calculate how much we can safely add
        int maxCanAdd = 21 - currentTurnValue;
        
        if (maxCanAdd <= 0)
        {
            // Already at or over 21, add nothing
            Debug.Log($"{perkName} activated! Rolled {diceA}+{diceB} but already at 21. Adding 0.");
            return (0, 0);
        }
        
        // Add as much as possible without busting
        // Distribute the value between the two dice proportionally
        if (totalRoll > 0)
        {
            float ratio = (float)maxCanAdd / totalRoll;
            int newDiceA = Mathf.FloorToInt(diceA * ratio);
            int newDiceB = maxCanAdd - newDiceA; // Ensure we add exactly maxCanAdd
            
            Debug.Log($"{perkName} activated! Rolled {diceA}+{diceB}={totalRoll}, but would bust. Adding {newDiceA}+{newDiceB}={maxCanAdd} instead.");
            return (newDiceA, newDiceB);
        }
        
        return (diceA, diceB);
    }
    
    public override Perk Clone()
    {
        return new LuckySixPerk();
    }
}
