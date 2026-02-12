using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages all available perks in the game
/// </summary>
public class PerkManager : MonoBehaviour
{
    private static PerkManager instance;
    public static PerkManager Instance => instance;
    
    [Header("Available Perks Pool")]
    private List<Perk> allPerks = new List<Perk>();
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializePerks();
    }
    
    private void InitializePerks()
    {
        // Add all available perks to the pool
        allPerks.Add(new LuckyFourPerk());
        allPerks.Add(new LuckySixPerk());
        allPerks.Add(new FiveAndUnderPerk());
        allPerks.Add(new DiceFlipPerk());
        // Add more perks here as they're created
    }
    
    /// <summary>
    /// Get a random selection of perks for the shop
    /// </summary>
    public List<Perk> GetRandomPerks(int count, List<Perk> ownedPerks = null)
    {
        List<Perk> availablePerks = allPerks.ToList();
        
        // Remove already owned perks from selection
        if (ownedPerks != null)
        {
            availablePerks = availablePerks.Where(perk => 
                !ownedPerks.Any(owned => owned.GetType() == perk.GetType())
            ).ToList();
        }
        
        // Shuffle and take the requested count
        List<Perk> selectedPerks = new List<Perk>();
        int perksToSelect = Mathf.Min(count, availablePerks.Count);
        
        for (int i = 0; i < perksToSelect; i++)
        {
            int randomIndex = Random.Range(0, availablePerks.Count);
            selectedPerks.Add(availablePerks[randomIndex].Clone());
            availablePerks.RemoveAt(randomIndex);
        }
        
        return selectedPerks;
    }
    
    /// <summary>
    /// Get all available perks
    /// </summary>
    public List<Perk> GetAllPerks()
    {
        return allPerks.Select(p => p.Clone()).ToList();
    }
}
