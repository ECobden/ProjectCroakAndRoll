using UnityEngine;

/// <summary>
/// Base class for all player perks
/// </summary>
[System.Serializable]
public abstract class Perk
{
    public string perkName;
    public string perkDescription;
    public int cost;
    public Sprite icon;
    
    /// <summary>
    /// Called when the perk is purchased and added to the player
    /// </summary>
    public abstract void OnPerkAdded(Player player);
    
    /// <summary>
    /// Called when a die is rolled during player's turn
    /// </summary>
    public virtual void OnDiceRolled(Player player, int diceValue, bool isDiceA) { }
    
    /// <summary>
    /// Modify dice values before they are added to turn total
    /// Return the modified dice values
    /// </summary>
    public virtual (int diceA, int diceB) ModifyDiceValues(Player player, int diceA, int diceB, int currentTurnValue)
    {
        return (diceA, diceB);
    }
    
    /// <summary>
    /// Called at the start of player's turn
    /// </summary>
    public virtual void OnTurnStart(Player player) { }
    
    /// <summary>
    /// Called when player stands
    /// </summary>
    public virtual void OnStand(Player player) { }
    
    /// <summary>
    /// Called when player busts
    /// </summary>
    public virtual void OnBust(Player player) { }
    
    /// <summary>
    /// Called when player wins a round
    /// </summary>
    public virtual void OnWinRound(Player player) { }
    
    /// <summary>
    /// Create a copy of this perk
    /// </summary>
    public abstract Perk Clone();
}
