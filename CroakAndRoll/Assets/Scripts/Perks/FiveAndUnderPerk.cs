using UnityEngine;

/// <summary>
/// Perk: Win the round if you roll 5 times and stay under 21
/// </summary>
[System.Serializable]
public class FiveAndUnderPerk : Perk
{
    private const int REQUIRED_ROLLS = 5;
    
    public FiveAndUnderPerk()
    {
        perkName = "5 and Under";
        perkDescription = "Win if you roll 5 times and stay under 21";
        cost = 250;
    }
    
    public override void OnPerkAdded(Player player)
    {
        Debug.Log($"Perk Added: {perkName}");
    }
    
    /// <summary>
    /// Check after each roll if the player has achieved the 5 rolls under 21 condition
    /// </summary>
    public override void OnDiceRolled(Player player, int diceValue, bool isDiceA)
    {
        // Only check after the second die of a roll (when isDiceA is false, meaning dice B just rolled)
        if (isDiceA) return;
        
        int rollCount = player.GetRollCount();
        int turnValue = player.GetTurnValue();
        
        // Check if player has rolled 5 times and is still under 21
        if (rollCount >= REQUIRED_ROLLS && turnValue < 21)
        {
            Debug.Log($"{perkName} triggered! Rolled {REQUIRED_ROLLS} times with total of {turnValue}. Instant win!");
            player.TriggerFiveAndUnderWin();
        }
    }
    
    public override Perk Clone()
    {
        return new FiveAndUnderPerk();
    }
}
