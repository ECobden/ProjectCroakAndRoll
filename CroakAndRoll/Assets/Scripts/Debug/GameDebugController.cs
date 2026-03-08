using UnityEngine;
using MoreMountains.Tools;
using System.Collections.Generic;

/// <summary>
/// Debug controller for testing game functionality during development.
/// Provides shortcuts for manipulating lives, opponents, rounds, and shop access.
/// </summary>
public class GameDebugController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DB_GameManager gameManager;
    [SerializeField] private Player player;
    [SerializeField] private House house;
    [SerializeField] private ShopManager shopManager;

    [Header("Debug Settings")]
    [SerializeField] private KeyCode debugMenuToggleKey = KeyCode.F1;
    [SerializeField] private List<OpponentProfileData> debugOpponentProfiles;

    [Header("Debug UI")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TMPro.TextMeshProUGUI shortcutsText;

    private int currentDebugOpponentIndex = 0;
    private bool isDebugPanelVisible = false;
    private bool isShopCurrentlyOpen = false;

    #region Unity Lifecycle

    private void Start()
    {
        // Hide debug panel initially
        if (debugPanel != null)
        {
            debugPanel.SetActive(false);
            isDebugPanelVisible = false;
        }

        // Populate shortcuts text
        PopulateShortcutsText();
    }

    private void Update()
    {
        // Toggle debug menu with F1 (or assigned key)
        if (Input.GetKeyDown(debugMenuToggleKey))
        {
            ToggleDebugMenu();
        }

        // Player Lives
        if (Input.GetKeyDown(KeyCode.F2))
            AddPlayerLife();
        
        if (Input.GetKeyDown(KeyCode.F3))
            RemovePlayerLife();

        // Opponent Lives
        if (Input.GetKeyDown(KeyCode.F4))
            AddOpponentLife();
        
        if (Input.GetKeyDown(KeyCode.F5))
            RemoveOpponentLife();

        // Round Control - Win/Lose
        if (Input.GetKeyDown(KeyCode.F6))
            ForcePlayerWin();
        
        if (Input.GetKeyDown(KeyCode.F7))
            ForceOpponentWin();

        // Shop
        if (Input.GetKeyDown(KeyCode.F8))
            ToggleShop();

        // Opponent Switching
        if (Input.GetKeyDown(KeyCode.F9))
            SwitchToNextOpponent();
        
        if (Input.GetKeyDown(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F9))
            SwitchToPreviousOpponent();

        // Money
        if (Input.GetKeyDown(KeyCode.F10))
            AddMoney(100);
        
        if (Input.GetKeyDown(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F10))
            AddMoney(1000);

        // Round Control - Bust
        if (Input.GetKeyDown(KeyCode.F11))
            ForcePlayerBust();
        
        if (Input.GetKeyDown(KeyCode.F12))
            ForceOpponentBust();
    }

    #endregion

    #region Debug Menu Methods

    private void ToggleDebugMenu()
    {
        if (debugPanel == null)
        {
            Debug.LogWarning("[DEBUG] Debug panel reference is not assigned!");
            return;
        }

        isDebugPanelVisible = !isDebugPanelVisible;
        debugPanel.SetActive(isDebugPanelVisible);
        
        Debug.Log($"[DEBUG] Debug menu {(isDebugPanelVisible ? "opened" : "closed")}");
    }

    private void PopulateShortcutsText()
    {
        if (shortcutsText == null)
            return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine("<b>=== DEBUG SHORTCUTS ===</b>\n");
        
        sb.AppendLine("<b>TOGGLE MENU</b>");
        sb.AppendLine($"  {debugMenuToggleKey} - Show/Hide this panel\n");
        
        sb.AppendLine("<b>PLAYER LIVES</b>");
        sb.AppendLine("  F2 - Add Player Life");
        sb.AppendLine("  F3 - Remove Player Life\n");
        
        sb.AppendLine("<b>OPPONENT LIVES</b>");
        sb.AppendLine("  F4 - Add Opponent Life");
        sb.AppendLine("  F5 - Remove Opponent Life\n");
        
        sb.AppendLine("<b>ROUND CONTROL</b>");
        sb.AppendLine("  F6 - Force Player Win");
        sb.AppendLine("  F7 - Force Opponent Win");
        sb.AppendLine("  F11 - Force Player Bust");
        sb.AppendLine("  F12 - Force Opponent Bust\n");
        
        sb.AppendLine("<b>SHOP</b>");
        sb.AppendLine("  F8 - Toggle Shop\n");
        
        sb.AppendLine("<b>OPPONENT SWITCHING</b>");
        sb.AppendLine("  F9 - Next Opponent");
        sb.AppendLine("  Shift+F9 - Previous Opponent\n");
        
        sb.AppendLine("<b>MONEY</b>");
        sb.AppendLine("  F10 - Add 100 Money");
        sb.AppendLine("  Shift+F10 - Add 1000 Money");
        
        shortcutsText.text = sb.ToString();
    }

    #endregion

    #region Lives Manipulation

    /// <summary>
    /// Add one life to the player
    /// </summary>
    public void AddPlayerLife()
    {
        if (player == null) return;
        
        player.AddLives(1);
        RefreshLivesUI();
        Debug.Log($"[DEBUG] Player lives increased to: {player.GetCurrentLives()}");
    }

    /// <summary>
    /// Remove one life from the player
    /// </summary>
    public void RemovePlayerLife()
    {
        if (player == null) return;
        
        int currentLives = player.GetCurrentLives();
        if (currentLives > 0)
        {
            player.LoseLife();
            RefreshLivesUI();
            Debug.Log($"[DEBUG] Player lives decreased to: {player.GetCurrentLives()}");
        }
    }

    /// <summary>
    /// Set player lives to a specific amount
    /// </summary>
    public void SetPlayerLives(int lives)
    {
        if (player == null) return;
        
        player.SetCurrentLives(lives);
        RefreshLivesUI();
        Debug.Log($"[DEBUG] Player lives set to: {lives}");
    }

    /// <summary>
    /// Add one life to the opponent
    /// </summary>
    public void AddOpponentLife()
    {
        if (house == null) return;
        
        house.AddLives(1);
        RefreshLivesUI();
        Debug.Log($"[DEBUG] Opponent lives increased to: {house.GetCurrentLives()}");
    }

    /// <summary>
    /// Remove one life from the opponent
    /// </summary>
    public void RemoveOpponentLife()
    {
        if (house == null) return;
        
        int currentLives = house.GetCurrentLives();
        if (currentLives > 0)
        {
            house.LoseLife();
            RefreshLivesUI();
            Debug.Log($"[DEBUG] Opponent lives decreased to: {house.GetCurrentLives()}");
        }
    }

    /// <summary>
    /// Set opponent lives to a specific amount
    /// </summary>
    public void SetOpponentLives(int lives)
    {
        if (house == null) return;
        
        house.SetCurrentLives(lives);
        RefreshLivesUI();
        Debug.Log($"[DEBUG] Opponent lives set to: {lives}");
    }

    private void RefreshLivesUI()
    {
        if (gameManager != null)
        {
            gameManager.UpdateLivesUI();
        }
    }

    #endregion

    #region Round Completion

    /// <summary>
    /// Force the current round to end with player winning
    /// </summary>
    public void ForcePlayerWin()
    {
        if (gameManager == null) return;
        
        Debug.Log("[DEBUG] Forcing player win");
        gameManager.PlayerWinsWith21();
    }

    /// <summary>
    /// Force the current round to end with opponent winning
    /// </summary>
    public void ForceOpponentWin()
    {
        if (gameManager == null) return;
        
        Debug.Log("[DEBUG] Forcing opponent win");
        gameManager.HouseWins();
    }

    /// <summary>
    /// Force player to bust (lose round)
    /// </summary>
    public void ForcePlayerBust()
    {
        if (gameManager == null) return;
        
        Debug.Log("[DEBUG] Forcing player bust");
        gameManager.PlayerBust();
    }

    /// <summary>
    /// Force opponent to bust (player wins)
    /// </summary>
    public void ForceOpponentBust()
    {
        if (gameManager == null) return;
        
        Debug.Log("[DEBUG] Forcing opponent bust");
        gameManager.HouseBust();
    }

    #endregion

    #region Opponent Manipulation

    /// <summary>
    /// Cycle to next opponent profile
    /// </summary>
    public void SwitchToNextOpponent()
    {
        if (debugOpponentProfiles == null || debugOpponentProfiles.Count == 0) return;
        
        currentDebugOpponentIndex = (currentDebugOpponentIndex + 1) % debugOpponentProfiles.Count;
        ApplyOpponentProfile(currentDebugOpponentIndex);
    }

    /// <summary>
    /// Cycle to previous opponent profile
    /// </summary>
    public void SwitchToPreviousOpponent()
    {
        if (debugOpponentProfiles == null || debugOpponentProfiles.Count == 0) return;
        
        currentDebugOpponentIndex--;
        if (currentDebugOpponentIndex < 0)
            currentDebugOpponentIndex = debugOpponentProfiles.Count - 1;
        
        ApplyOpponentProfile(currentDebugOpponentIndex);
    }

    /// <summary>
    /// Apply a specific opponent profile by index
    /// </summary>
    public void ApplyOpponentProfile(int index)
    {
        if (debugOpponentProfiles == null || index < 0 || index >= debugOpponentProfiles.Count)
            return;

        if (house == null) return;

        OpponentProfileData profile = debugOpponentProfiles[index];
        house.ApplyOpponentProfile(profile);
        RefreshLivesUI();
        
        Debug.Log($"[DEBUG] Switched to opponent: {profile.opponentName} (Lives: {profile.lives})");
    }

    #endregion

    #region Shop Manipulation

    /// <summary>
    /// Toggle the shop open/closed
    /// </summary>
    public void ToggleShop()
    {
        if (shopManager == null) return;
        
        if (isShopCurrentlyOpen)
        {
            Debug.Log("[DEBUG] Closing shop");
            shopManager.CloseShop();
            isShopCurrentlyOpen = false;
        }
        else
        {
            Debug.Log("[DEBUG] Opening shop");
            int seed = Random.Range(0, 10000);
            shopManager.OpenShop(seed);
            isShopCurrentlyOpen = true;
        }
    }

    /// <summary>
    /// Open the shop immediately
    /// </summary>
    public void OpenShopNow()
    {
        if (shopManager == null) return;
        
        Debug.Log("[DEBUG] Opening shop");
        int seed = Random.Range(0, 10000);
        shopManager.OpenShop(seed);
        isShopCurrentlyOpen = true;
    }

    /// <summary>
    /// Close the shop
    /// </summary>
    public void CloseShopNow()
    {
        if (shopManager == null) return;
        
        Debug.Log("[DEBUG] Closing shop");
        shopManager.CloseShop();
        isShopCurrentlyOpen = false;
    }

    #endregion

    #region Money Manipulation

    /// <summary>
    /// Add money to player
    /// </summary>
    public void AddMoney(int amount)
    {
        if (player == null) return;
        
        player.AddMoney(amount);
        Debug.Log($"[DEBUG] Added {amount} money to player");
    }

    /// <summary>
    /// Remove money from player
    /// </summary>
    public void RemoveMoney(int amount)
    {
        if (player == null) return;
        
        player.SpendMoney(amount);
        Debug.Log($"[DEBUG] Removed {amount} money from player");
    }

    /// <summary>
    /// Set player money to specific amount
    /// </summary>
    public void SetMoney(int amount)
    {
        if (player == null) return;
        
        int current = player.GetCurrentMoney();
        int difference = amount - current;
        
        if (difference > 0)
            player.AddMoney(difference);
        else if (difference < 0)
            player.SpendMoney(-difference);
        
        Debug.Log($"[DEBUG] Set player money to: {amount}");
    }

    #endregion
}
