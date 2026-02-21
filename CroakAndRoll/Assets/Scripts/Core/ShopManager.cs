using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Manages the shop interface and progression system.
/// Triggered by GameManager at the end of each round to allow player to purchase dice and upgrades.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Shop Settings")]
    [SerializeField] private List<DieData> availableDice = new List<DieData>();
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI playerMoneyDisplay;
    [SerializeField] private TextMeshProUGUI roundResultDisplay;

    [Header("References")]
    [SerializeField] private DB_GameManager gameManager;
    [SerializeField] private Player player;

    private bool isShopOpen = false;
    private DiceBag playerDiceBag;

    #region Lifecycle

    private void Start()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (player != null)
        {
            playerDiceBag = player.GetComponent<DiceBag>();
        }
    }

    #endregion

    #region Shop Control

    /// <summary>
    /// Open the shop interface at the end of a round.
    /// </summary>
    public void OpenShop(string roundResult)
    {
        isShopOpen = true;
        
        if (shopPanel != null)
            shopPanel.SetActive(true);

        if (roundResultDisplay != null)
            roundResultDisplay.text = roundResult;

        UpdateMoneyDisplay();
        RefreshShopUI();

        Debug.Log("Shop opened for player");
    }

    /// <summary>
    /// Close the shop and resume game.
    /// </summary>
    public void CloseShop()
    {
        isShopOpen = false;

        if (shopPanel != null)
            shopPanel.SetActive(false);

        // TODO: Notify GameManager to proceed to next round
        // Uncomment when DB_GameManager has the OnShopClosed method:
        // if (gameManager != null)
        // {
        //     gameManager.OnShopClosed();
        // }

        Debug.Log("Shop closed - proceeding to next round");
    }

    /// <summary>
    /// Check if shop is currently open.
    /// </summary>
    public bool IsShopOpen()
    {
        return isShopOpen;
    }

    #endregion

    #region Purchasing

    /// <summary>
    /// Purchase a die from the shop.
    /// </summary>
    public bool PurchaseDie(DieData die)
    {
        if (player == null || playerDiceBag == null)
            return false;

        if (player.SpendMoney(die.cost))
        {
            playerDiceBag.AddDie(die);
            UpdateMoneyDisplay();
            RefreshShopUI();
            Debug.Log($"Player purchased {die.dieName} for {die.cost}");
            return true;
        }
        else
        {
            Debug.LogWarning($"Player cannot afford {die.dieName}");
            return false;
        }
    }

    /// <summary>
    /// Purchase an upgrade (e.g., extra inventory slot).
    /// </summary>
    public bool PurchaseUpgrade(string upgradeName, int cost)
    {
        if (player == null || player.SpendMoney(cost))
        {
            UpdateMoneyDisplay();
            RefreshShopUI();
            Debug.Log($"Player purchased upgrade: {upgradeName}");
            return true;
        }
        return false;
    }

    #endregion

    #region UI Updates

    /// <summary>
    /// Refresh the shop UI to show available items and prices.
    /// </summary>
    public void RefreshShopUI()
    {
        // TODO: Update UI elements to show all available dice
        // List prices, compare with player money, show affordability
        Debug.Log($"Shop UI refreshed - {availableDice.Count} dice available");
    }

    /// <summary>
    /// Update the player money display.
    /// </summary>
    private void UpdateMoneyDisplay()
    {
        if (player != null && playerMoneyDisplay != null)
        {
            playerMoneyDisplay.text = $"Money: {player.GetCurrentMoney()}";
        }
    }

    #endregion

    #region Shop Inventory

    /// <summary>
    /// Get all dice available in the shop.
    /// </summary>
    public List<DieData> GetAvailableDice()
    {
        return new List<DieData>(availableDice);
    }

    /// <summary>
    /// Add a new die to the shop inventory.
    /// </summary>
    public void AddDieToShop(DieData die)
    {
        if (die != null && !availableDice.Contains(die))
        {
            availableDice.Add(die);
            Debug.Log($"{die.dieName} added to shop");
        }
    }

    /// <summary>
    /// Remove a die from the shop inventory (sold out).
    /// </summary>
    public void RemoveDieFromShop(DieData die)
    {
        if (availableDice.Remove(die))
        {
            Debug.Log($"{die.dieName} removed from shop");
        }
    }

    #endregion

    #region Player Progression

    /// <summary>
    /// Get the player's current dice bag.
    /// </summary>
    public DiceBag GetPlayerDiceBag()
    {
        return playerDiceBag;
    }

    /// <summary>
    /// Reset the progression system for a new game.
    /// </summary>
    public void ResetProgression()
    {
        if (playerDiceBag != null)
        {
            playerDiceBag.ClearBag();
            playerDiceBag.InitializeBag();
        }

        if (player != null)
        {
            player.ResetMoney();
        }

        Debug.Log("Progression reset");
    }

    #endregion
}
