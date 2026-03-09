using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject that holds the shop's available dice inventory.
/// Allows for easy configuration and reuse across different shop instances.
/// </summary>
[CreateAssetMenu(fileName = "ShopInventory", menuName = "Croak and Roll/Shop/Shop Inventory Data")]
public class ShopInventoryData : ScriptableObject
{
    [Header("Shop Inventory")]
    [Tooltip("List of all dice available for purchase in this shop")]
    [SerializeField] private List<DieData> availableDice = new List<DieData>();

    /// <summary>
    /// Get all dice available in this shop inventory.
    /// </summary>
    public List<DieData> GetAvailableDice()
    {
        return availableDice;
    }

    /// <summary>
    /// Get the number of dice in the inventory.
    /// </summary>
    public int GetDiceCount()
    {
        return availableDice != null ? availableDice.Count : 0;
    }

    /// <summary>
    /// Check if a specific die is in this inventory.
    /// </summary>
    public bool ContainsDie(DieData die)
    {
        return availableDice != null && availableDice.Contains(die);
    }
}
