using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Displays a single perk option in the shop
/// </summary>
public class UI_PerkShopItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI perkNameText;
    [SerializeField] private TextMeshProUGUI perkDescriptionText;
    [SerializeField] private TextMeshProUGUI perkCostText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Image perkIcon;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject cannotAffordOverlay;
    
    private Perk perk;
    private Action<Perk> onPurchase;
    
    /// <summary>
    /// Setup the perk item display
    /// </summary>
    public void Setup(Perk perk, int playerMoney, Action<Perk> onPurchaseCallback)
    {
        this.perk = perk;
        this.onPurchase = onPurchaseCallback;
        
        // Set text
        if (perkNameText != null)
            perkNameText.text = perk.perkName;
            
        if (perkDescriptionText != null)
            perkDescriptionText.text = perk.perkDescription;
            
        if (perkCostText != null)
            perkCostText.text = $"${perk.cost}";
            
        if (perkIcon != null && perk.icon != null)
            perkIcon.sprite = perk.icon;
        
        // Setup button
        bool canAfford = playerMoney >= perk.cost;
        
        if (purchaseButton != null)
        {
            purchaseButton.interactable = canAfford;
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }
        
        // Show/hide cannot afford overlay
        if (cannotAffordOverlay != null)
            cannotAffordOverlay.SetActive(!canAfford);
    }
    
    private void OnPurchaseClicked()
    {
        onPurchase?.Invoke(perk);
    }
    
    /// <summary>
    /// Disable this perk item (after purchase)
    /// </summary>
    public void Disable()
    {
        if (purchaseButton != null)
            purchaseButton.interactable = false;
            
        gameObject.SetActive(false);
    }
}
