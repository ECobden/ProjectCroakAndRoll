using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for a single shop item display.
/// Shows die information and handles purchase interaction.
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Visual Feedback")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = Color.gray;
    [SerializeField] private Image backgroundImage;

    private DieData dieData;
    private ShopManager shopManager;
    private bool canAfford;

    /// <summary>
    /// Set up the shop item with die data.
    /// </summary>
    public void Setup(DieData die, ShopManager shop, bool affordable)
    {
        dieData = die;
        shopManager = shop;
        canAfford = affordable;

        // Set up UI text
        if (nameText != null)
            nameText.text = die.dieName;

        if (costText != null)
            costText.text = $"${die.cost}";

        if (descriptionText != null)
            descriptionText.text = die.description;

        if (rarityText != null)
            rarityText.text = $"Rarity: {GetRarityName(die.rarity)}";

        // Set up purchase button
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
            UpdateButtonState();
        }
    }

    /// <summary>
    /// Update affordability state based on current player money.
    /// </summary>
    public void UpdateAffordability(int playerMoney)
    {
        canAfford = playerMoney >= dieData.cost;
        UpdateButtonState();
    }

    /// <summary>
    /// Update the visual state of the purchase button.
    /// </summary>
    private void UpdateButtonState()
    {
        if (purchaseButton != null)
        {
            purchaseButton.interactable = canAfford;
        }

        if (buttonText != null)
        {
            buttonText.text = canAfford ? "Purchase" : "Can't Afford";
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = canAfford ? affordableColor : unaffordableColor;
        }
    }

    /// <summary>
    /// Handle purchase button click.
    /// </summary>
    private void OnPurchaseClicked()
    {
        if (shopManager != null && dieData != null && canAfford)
        {
            bool success = shopManager.PurchaseDie(dieData);
            if (success)
            {
                // Visual feedback could be added here (e.g., animation, sound)
                Debug.Log($"Successfully purchased {dieData.dieName}");
            }
        }
    }

    /// <summary>
    /// Convert rarity number to a readable name.
    /// </summary>
    private string GetRarityName(int rarity)
    {
        switch (rarity)
        {
            case 1: return "Common";
            case 2: return "Uncommon";
            case 3: return "Rare";
            case 4: return "Epic";
            case 5: return "Legendary";
            default: return $"Tier {rarity}";
        }
    }
}
