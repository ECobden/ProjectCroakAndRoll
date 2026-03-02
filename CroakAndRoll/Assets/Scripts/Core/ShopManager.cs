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
    [SerializeField] private int numberOfShopOffers = 2;

    [Header("Shop UI")]
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private Transform shopItemContainer;
    [SerializeField] private UnityEngine.UI.Button continueButton;

    [Header("References")]
    [SerializeField] private DB_GameManager gameManager;
    [SerializeField] private Player player;

    private bool isShopOpen = false;
    private DiceBag playerDiceBag;
    private List<DieData> currentShopOffers = new List<DieData>();
    private List<GameObject> instantiatedShopItems = new List<GameObject>();

    #region Lifecycle

    private void Start()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (player != null)
        {
            playerDiceBag = player.GetComponent<DiceBag>();
        }

        // Set up continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(CloseShop);
        }
    }

    #endregion

    #region Shop Control

    /// <summary>
    /// Open the shop interface at the end of a round.
    /// </summary>
    public void OpenShop(string roundResult, int seed)
    {
        isShopOpen = true;
        
        if (shopPanel != null)
            shopPanel.SetActive(true);

        if (roundResultDisplay != null)
            roundResultDisplay.text = roundResult;

        UpdateMoneyDisplay();
        RefreshShopUI(seed);

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

        // Notify GameManager to proceed to next round
        if (gameManager != null)
        {
            gameManager.OnShopClosed();
        }

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
            UpdateShopItemsAffordability();
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
        if (player == null)
            return false;

        if (player.SpendMoney(cost))
        {
            UpdateMoneyDisplay();
            UpdateShopItemsAffordability();
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
    public void RefreshShopUI(int seed)
    {
        // Clear previous shop items
        ClearShopItems();

        // Generate shop offers using weighted selection
        currentShopOffers = SelectShopOffers(seed, numberOfShopOffers);

        // Create UI elements for each offer
        foreach (DieData die in currentShopOffers)
        {
            if (shopItemPrefab != null && shopItemContainer != null)
            {
                GameObject itemObj = Instantiate(shopItemPrefab, shopItemContainer);
                instantiatedShopItems.Add(itemObj);

                // Get the ShopItemUI component and set it up
                ShopItemUI itemUI = itemObj.GetComponent<ShopItemUI>();
                if (itemUI != null)
                {
                    bool canAfford = player != null && player.GetCurrentMoney() >= die.cost;
                    itemUI.Setup(die, this, canAfford);
                }
            }
        }

        Debug.Log($"Shop UI refreshed - Showing {currentShopOffers.Count} offers");
    }

    /// <summary>
    /// Clear all instantiated shop item UI elements.
    /// </summary>
    private void ClearShopItems()
    {
        foreach (GameObject item in instantiatedShopItems)
        {
            if (item != null)
                Destroy(item);
        }
        instantiatedShopItems.Clear();
        currentShopOffers.Clear();
    }

    /// <summary>
    /// Update the player money display.
    /// </summary>
    private void UpdateMoneyDisplay()
    {
        if (player != null && playerMoneyDisplay != null)
        {
            playerMoneyDisplay.text = $"${player.GetCurrentMoney()}";
        }
    }

    /// <summary>
    /// Update affordability state of all shop items without regenerating offers.
    /// </summary>
    private void UpdateShopItemsAffordability()
    {
        if (player == null) return;

        int currentMoney = player.GetCurrentMoney();
        foreach (GameObject itemObj in instantiatedShopItems)
        {
            if (itemObj != null)
            {
                ShopItemUI itemUI = itemObj.GetComponent<ShopItemUI>();
                if (itemUI != null)
                {
                    itemUI.UpdateAffordability(currentMoney);
                }
            }
        }
    }

    #endregion

    #region Shop Selection Logic

    /// <summary>
    /// Select shop offers using weighted random selection based on rarity.
    /// Lower rarity = more common = higher selection weight.
    /// </summary>
    private List<DieData> SelectShopOffers(int seed, int count)
    {
        List<DieData> offers = new List<DieData>();

        if (availableDice.Count == 0)
        {
            Debug.LogWarning("No dice available in shop inventory!");
            return offers;
        }

        // Use seed for deterministic randomness
        System.Random rng = new System.Random(seed);

        // Calculate total weight (inverse of rarity)
        float totalWeight = 0f;
        foreach (DieData die in availableDice)
        {
            float weight = 1f / Mathf.Max(die.rarity, 1); // Prevent division by zero
            totalWeight += weight;
        }

        // Select the specified number of offers
        for (int i = 0; i < count && availableDice.Count > 0; i++)
        {
            float randomValue = (float)rng.NextDouble() * totalWeight;
            float cumulative = 0f;

            foreach (DieData die in availableDice)
            {
                float weight = 1f / Mathf.Max(die.rarity, 1);
                cumulative += weight;

                if (randomValue <= cumulative)
                {
                    offers.Add(die);
                    break;
                }
            }
        }

        return offers;
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
