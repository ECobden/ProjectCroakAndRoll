using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the collection of dice owned by a participant.
/// Handles drawing random dice for rolls and managing the inventory.
/// </summary>
public class DiceBag : MonoBehaviour
{
    [Header("Starting Dice")]
    [SerializeField] private List<DieData> startingDice = new List<DieData>();

    [Header("Settings")]
    [SerializeField] private int dicePerRoll = 2; // How many dice to draw per roll

    private List<DieData> diceCollection = new List<DieData>();
    private System.Random random = new System.Random();

    #region Lifecycle

    private void Start()
    {
        InitializeBag();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize the dice bag with starting dice.
    /// </summary>
    public void InitializeBag()
    {
        diceCollection.Clear();
        
        if (startingDice != null)
        {
            diceCollection.AddRange(startingDice);
            Debug.Log($"Dice bag initialized with {diceCollection.Count} dice");
        }
        else
        {
            Debug.LogWarning("No starting dice assigned to DiceBag!");
        }
    }

    #endregion

    #region Drawing Dice

    /// <summary>
    /// Draw a random dice from the bag for rolling.
    /// </summary>
    public DieData DrawRandomDie()
    {
        if (diceCollection.Count == 0)
        {
            Debug.LogWarning("No dice in bag!");
            return null;
        }

        int index = random.Next(diceCollection.Count);
        return diceCollection[index];
    }

    /// <summary>
    /// Draw multiple random dice from the bag.
    /// </summary>
    public List<DieData> DrawRandomDice(int count)
    {
        List<DieData> drawn = new List<DieData>();

        if (diceCollection.Count == 0)
        {
            Debug.LogWarning("No dice in bag!");
            return drawn;
        }

        for (int i = 0; i < count && i < diceCollection.Count; i++)
        {
            int index = random.Next(diceCollection.Count);
            drawn.Add(diceCollection[index]);
        }

        Debug.Log($"Drew {drawn.Count} dice from bag. Bag now contains {diceCollection.Count} dice");
        return drawn;
    }

    /// <summary>
    /// Draw the default number of dice for a roll.
    /// </summary>
    public List<DieData> DrawRollDice()
    {
        return DrawRandomDice(dicePerRoll);
    }

    #endregion

    #region Inventory Management

    /// <summary>
    /// Add a die to the collection.
    /// </summary>
    public void AddDie(DieData die)
    {
        if (die != null)
        {
            diceCollection.Add(die);
            Debug.Log($"Added {die.dieName} to dice bag. Total dice: {diceCollection.Count}");
        }
    }

    /// <summary>
    /// Remove a specific die from the collection.
    /// </summary>
    public bool RemoveDie(DieData die)
    {
        if (diceCollection.Remove(die))
        {
            Debug.Log($"Removed {die.dieName} from dice bag. Total dice: {diceCollection.Count}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Get all dice in the bag.
    /// </summary>
    public List<DieData> GetAllDice()
    {
        return new List<DieData>(diceCollection);
    }

    /// <summary>
    /// Get the total count of dice in the bag.
    /// </summary>
    public int GetDiceCount()
    {
        return diceCollection.Count;
    }

    /// <summary>
    /// Count how many of a specific die type are in the bag.
    /// </summary>
    public int CountDieType(DieData die)
    {
        return diceCollection.Count(d => d == die);
    }

    /// <summary>
    /// Clear all dice from the bag.
    /// </summary>
    public void ClearBag()
    {
        diceCollection.Clear();
        Debug.Log("Dice bag cleared");
    }

    #endregion

    #region Utility

    /// <summary>
    /// Get a summary of what's in the bag.
    /// </summary>
    public string GetBagSummary()
    {
        if (diceCollection.Count == 0)
            return "Dice Bag is empty";

        // Group by die type
        var grouped = diceCollection
            .GroupBy(d => d.dieName)
            .Select(g => $"{g.Key} x{g.Count()}");

        return "Dice: " + string.Join(", ", grouped);
    }

    #endregion
}
