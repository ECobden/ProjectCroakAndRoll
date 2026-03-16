using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;

/// <summary>
/// Manages the shop interface and progression system.
/// Triggered by GameManager at the end of each round to allow player to purchase dice and upgrades.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Shop Settings")]
    [SerializeField] private ShopInventoryData shopInventory;
    [SerializeField] private int numberOfShopOffers = 2;
    [SerializeField] private int rerollCost = 50;

    [Header("Shop Item Spawning")]
    [SerializeField] private Transform[] shopItemSpawnPoints;
    [SerializeField] private GameObject rerollObject;

    [Header("References")]
    [SerializeField] private DB_GameManager gameManager;
    [SerializeField] private Player player;
    [SerializeField] private ShopMachineController shopMachineController;

    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera shopCamera;
    [SerializeField] private CinemachineCamera gameplayCamera;
    [SerializeField] private int shopCameraPriority = 10;
    [SerializeField] private int gameplayCameraPriority = 10;

    private bool isShopOpen = false;
    private DiceBag playerDiceBag;
    private List<DieData> currentShopOffers = new List<DieData>();
    private List<GameObject> instantiatedShopItems = new List<GameObject>();
    private Dictionary<DB_DiceController, DieData> shopDiceLookup = new Dictionary<DB_DiceController, DieData>();
    private DB_DiceController selectedShopDice;

    #region Lifecycle

    private void Start()
    {
        if (player != null)
        {
            playerDiceBag = player.GetComponent<DiceBag>();
        }

    }

    #endregion

    #region Shop Control

    /// <summary>
    /// Open the shop at the end of a round.
    /// </summary>
    public void OpenShop(int seed)
    {
        isShopOpen = true;

        // Hide gameplay buttons and show shop buttons
        if (DB_UIManager.Instance != null)
        {
            DB_UIManager.Instance.HideButtonPanel();
            DB_UIManager.Instance.ShowShopButtons(CloseShop, () => RerollShop());
        }

        // Show shop machine
        if (shopMachineController != null)
        {
            shopMachineController.ShowShop();
        }

        RefreshShopUI(seed);

        // Switch to shop camera
        SwitchToShopCamera();

        Debug.Log("Shop opened for player");
    }

    /// <summary>
    /// Close the shop and resume game.
    /// </summary>
    public void CloseShop()
    {
        isShopOpen = false;

        // Hide shop buttons
        if (DB_UIManager.Instance != null)
            DB_UIManager.Instance.HideShopButtons();

        // Hide shop machine
        if (shopMachineController != null)
        {
            shopMachineController.HideShop();
        }

        // Clear shop items from scene
        ClearShopItems();

        // Switch back to gameplay camera
        SwitchToGameplayCamera();

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

    #region Camera Control

    /// <summary>
    /// Switch to the shop camera by adjusting priorities.
    /// </summary>
    private void SwitchToShopCamera()
    {
        if (shopCamera != null)
        {
            shopCamera.Priority = shopCameraPriority;
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.Priority = 0;
        }
    }

    /// <summary>
    /// Switch to the gameplay camera by adjusting priorities.
    /// </summary>
    private void SwitchToGameplayCamera()
    {
        if (gameplayCamera != null)
        {
            gameplayCamera.Priority = gameplayCameraPriority;
        }

        if (shopCamera != null)
        {
            shopCamera.Priority = 0;
        }
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
    /// Purchase the currently selected shop die.
    /// Intended for wiring to the shop Buy button.
    /// </summary>
    public void BuyItem()
    {
        if (!isShopOpen)
        {
            Debug.LogWarning("Cannot buy item: shop is not open.");
            return;
        }

        if (selectedShopDice == null)
        {
            Debug.LogWarning("Cannot buy item: no shop die selected.");
            return;
        }

        if (!shopDiceLookup.TryGetValue(selectedShopDice, out DieData selectedDie) || selectedDie == null)
        {
            Debug.LogWarning("Cannot buy item: selected die is invalid.");
            return;
        }

        if (!PurchaseDie(selectedDie))
            return;

        if (selectedShopDice != null)
        {
            selectedShopDice.RemoveHighlight();
            GameObject purchasedObject = selectedShopDice.gameObject;
            instantiatedShopItems.Remove(purchasedObject);
            currentShopOffers.Remove(selectedDie);
            shopDiceLookup.Remove(selectedShopDice);
            selectedShopDice = null;

            if (purchasedObject != null)
                Destroy(purchasedObject);
        }

        if (DB_UIManager.Instance != null)
            DB_UIManager.Instance.HideShopBuyButton();
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
            Debug.Log($"Player purchased upgrade: {upgradeName}");
            return true;
        }
        return false;
    }

    #endregion

    #region UI Updates

    /// <summary>
    /// Refresh the shop to show available items.
    /// </summary>
    public void RefreshShopUI(int seed)
    {
        // Clear previous shop items
        ClearShopItems();

        // Generate shop offers using weighted selection
        currentShopOffers = SelectShopOffers(seed, numberOfShopOffers);

        if (shopItemSpawnPoints == null || shopItemSpawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned to ShopManager!");
            return;
        }

        // Delayed spawning to allow shop machine animations to play
        StartCoroutine(SpawnShopItemsDelayed());
    }

    /// <summary>
    /// Spawn shop items with a delay to allow animations to play.
    /// </summary>
    private System.Collections.IEnumerator SpawnShopItemsDelayed()
    {
        // 1 second delay to allow shop machine animations to complete
        Debug.Log("Delaying shop item spawn by 1 second to allow shop machine animations to play");
        yield return new UnityEngine.WaitForSeconds(1f);

        // Spawn dice prefabs at designated spawn points
        for (int i = 0; i < currentShopOffers.Count && i < shopItemSpawnPoints.Length; i++)
        {
            DieData die = currentShopOffers[i];
            Transform spawnPoint = shopItemSpawnPoints[i];

            if (die != null && die.diePrefab != null && spawnPoint != null)
            {
                GameObject diceObj = Instantiate(die.diePrefab, spawnPoint.position, spawnPoint.rotation);
                instantiatedShopItems.Add(diceObj);

                DB_DiceController diceController = diceObj.GetComponent<DB_DiceController>();
                if (diceController != null)
                {
                    diceController.SetDieData(die);
                    diceController.SetClickable(true, OnShopDiceClicked);
                    shopDiceLookup[diceController] = die;
                }
            }
        }

        Debug.Log($"Shop refreshed - Showing {currentShopOffers.Count} offers");
    }

    /// <summary>
    /// Reroll the shop offers for a cost.
    /// </summary>
    public bool RerollShop()
    {
        if (player == null)
            return false;

        if (!player.SpendMoney(rerollCost))
        {
            Debug.LogWarning($"Cannot afford shop reroll (costs {rerollCost})");
            return false;
        }

        // Generate new seed based on current time
        int newSeed = System.DateTime.Now.Millisecond + UnityEngine.Random.Range(0, 10000);
        
        // Animate reroll lever
        if (shopMachineController != null)
        {
            shopMachineController.RotateRerollLever();
        }

        RefreshShopUI(newSeed);
        
        Debug.Log($"Shop rerolled for {rerollCost}");
        return true;
    }

    /// <summary>
    /// Get the cost to reroll the shop.
    /// </summary>
    public int GetRerollCost()
    {
        return rerollCost;
    }

    /// <summary>
    /// Clear all instantiated shop dice from the scene.
    /// </summary>
    private void ClearShopItems()
    {
        if (selectedShopDice != null)
            selectedShopDice.RemoveHighlight();

        foreach (GameObject item in instantiatedShopItems)
        {
            if (item != null)
                Destroy(item);
        }

        selectedShopDice = null;
        instantiatedShopItems.Clear();
        currentShopOffers.Clear();
        shopDiceLookup.Clear();

        if (DB_UIManager.Instance != null)
            DB_UIManager.Instance.HideShopBuyButton();
    }

    private void OnShopDiceClicked(DB_DiceController clickedDice)
    {
        if (clickedDice == null)
            return;

        clickedDice.ShowDiceInfo();

        if (selectedShopDice != null && selectedShopDice != clickedDice)
            selectedShopDice.RemoveHighlight();

        selectedShopDice = clickedDice;
        selectedShopDice.Highlight(Color.blue);

        if (DB_UIManager.Instance != null)
            DB_UIManager.Instance.ShowShopBuyButton(BuyItem);
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

        if (shopInventory == null || shopInventory.GetDiceCount() == 0)
        {
            Debug.LogWarning("No dice available in shop inventory!");
            return offers;
        }

        List<DieData> availableDice = shopInventory.GetAvailableDice();

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
        if (shopInventory == null)
            return new List<DieData>();
        
        return new List<DieData>(shopInventory.GetAvailableDice());
    }

    /// <summary>
    /// Get the current shop inventory data.
    /// </summary>
    public ShopInventoryData GetShopInventory()
    {
        return shopInventory;
    }

    /// <summary>
    /// Set a new shop inventory.
    /// </summary>
    public void SetShopInventory(ShopInventoryData newInventory)
    {
        shopInventory = newInventory;
        Debug.Log($"Shop inventory updated");
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
