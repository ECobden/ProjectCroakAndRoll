using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Controls the perk shop UI and purchasing
/// </summary>
public class UI_PerkShopController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UI_PerkShopItem[] perkSlots;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    private Player player;
    private Action onClose;
    private List<Perk> currentPerks;
    
    /// <summary>
    /// Open the shop with random perks
    /// </summary>
    public void OpenShop(Player player, Action onCloseCallback)
    {
        this.player = player;
        this.onClose = onCloseCallback;
        
        // Get random perks from the pool
        if (PerkManager.Instance != null)
        {
            currentPerks = PerkManager.Instance.GetRandomPerks(3, player.GetActivePerks());
        }
        else
        {
            Debug.LogError("PerkManager not found!");
            currentPerks = new List<Perk>();
        }
        
        // Setup perk slots
        SetupPerkSlots();
        
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseShop);
        }
        
        // Fade in
        gameObject.SetActive(true);
        StartCoroutine(FadeIn());
    }
    
    private void SetupPerkSlots()
    {
        if (perkSlots == null || player == null) return;
        
        int playerMoney = player.GetCurrentMoney();
        
        for (int i = 0; i < perkSlots.Length; i++)
        {
            if (perkSlots[i] == null) continue;
            
            if (i < currentPerks.Count)
            {
                perkSlots[i].gameObject.SetActive(true);
                perkSlots[i].Setup(currentPerks[i], playerMoney, OnPerkPurchased);
            }
            else
            {
                perkSlots[i].gameObject.SetActive(false);
            }
        }
    }
    
    private void OnPerkPurchased(Perk perk)
    {
        if (player == null || perk == null) return;
        
        int playerMoney = player.GetCurrentMoney();
        
        // Check if player can afford
        if (playerMoney < perk.cost)
        {
            Debug.Log("Cannot afford this perk!");
            return;
        }
        
        // Deduct cost
        player.AddMoney(-perk.cost);
        
        // Add perk to player
        player.AddPerk(perk);
        
        // Refresh the shop display
        SetupPerkSlots();
        
        Debug.Log($"Purchased perk: {perk.perkName} for ${perk.cost}");
    }
    
    private void CloseShop()
    {
        StartCoroutine(FadeOutAndClose());
    }
    
    /// <summary>
    /// Hide the shop
    /// </summary>
    public void Hide()
    {
        StartCoroutine(FadeOutAndClose());
    }
    
    #region Fade Animations
    
    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
        
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
    }
    
    private IEnumerator FadeOutAndClose()
    {
        if (canvasGroup == null)
        {
            // No canvas group, just close immediately
            gameObject.SetActive(false);
            onClose?.Invoke();
            yield break;
        }
        
        canvasGroup.interactable = false;
        
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        onClose?.Invoke();
    }
    
    #endregion
}
