using UnityEngine;

/// <summary>
/// Perk: Receive $4 for rolling a 4 on individual dice
/// </summary>
[System.Serializable]
public class LuckyFourPerk : Perk
{
    public LuckyFourPerk()
    {
        perkName = "Lucky Four";
        perkDescription = "Gain $4 each time you roll a 4";
        cost = 150;
    }
    
    public override void OnPerkAdded(Player player)
    {
        Debug.Log($"Perk Added: {perkName}");
    }
    
    public override void OnDiceRolled(Player player, int diceValue, bool isDiceA)
    {
        if (diceValue == 4)
        {
            player.AddMoney(4);
            Debug.Log($"{perkName} triggered! Gained $4 for rolling a 4");
        }
    }
    
    public override Perk Clone()
    {
        return new LuckyFourPerk();
    }
}
