using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class DB_GameManager : MonoBehaviour
{
    #region Enums
    
    public enum TurnState
    {
        PlayerTurn,
        HouseTurn,
        GameOver
    }
    
    #endregion

    #region Serialized Fields

    [Header("Player References")]
    [SerializeField] private Player player;
    [SerializeField] private House house;

    [Header("Round Manager")]
    [SerializeField] private DB_RoundManager roundManager;

    [Header("Dice Manager")]
    [SerializeField] private DB_DiceManager diceManager;

    [Header("UI Manager")]
    [SerializeField] private DB_UIManager uiManager;

    [Header("Game Settings")]
    [SerializeField] private int smallBetAmount = 50;
    [SerializeField] private int largeBetAmount = 200;
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private float newRoundDelay = 1.5f;
    
    #endregion

    #region Private Fields
    
    private TurnState currentTurn = TurnState.PlayerTurn;
    private bool isProcessingTurn = false;
    private bool isBettingMode = true;
    
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeDice();
        InitializeUI();

        roundManager.InitializeRound();
        ShowBetSelectionPanel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(restartKey))
        {
            RestartGame();
        }
    }
    
    #endregion

    #region Initialization

    private void InitializeDice()
    {
        if (diceManager != null)
            diceManager.Initialize();
    }

    private void InitializeUI()
    {
        if (uiManager != null)
            uiManager.Initialize(RestartGame);
    }
    
    #endregion

    #region Turn Management

    public void StartPlayerTurn()
    {
        if (currentTurn == TurnState.PlayerTurn)
        {
            Debug.LogWarning("StartPlayerTurn called but already in PlayerTurn. Ignoring.");
            return;
        }

        currentTurn = TurnState.PlayerTurn;
        
        if (uiManager != null)
        {
            uiManager.UpdateGoalText("Roll Closest to 21");
            uiManager.SetTurnMarkerToPlayer();
        }
    }

    public void EndPlayerTurn()
    {
        if (currentTurn != TurnState.PlayerTurn) 
        {
            Debug.LogWarning($"EndPlayerTurn called but currentTurn is {currentTurn}, not PlayerTurn. Ignoring.");
            return;
        }
        
        if (isProcessingTurn)
        {
            Debug.LogWarning("EndPlayerTurn called but already processing turn transition. Ignoring.");
            return;
        }

        Debug.Log("Player's turn ended");
        isProcessingTurn = true;
        StartHouseTurn();
        isProcessingTurn = false;
    }

    public void StartHouseTurn()
    {
        if (currentTurn == TurnState.HouseTurn)
        {
            Debug.LogWarning("StartHouseTurn called but already in HouseTurn. Ignoring.");
            return;
        }

        currentTurn = TurnState.HouseTurn;
        Debug.Log("House's turn");

        if (player != null && uiManager != null)
        {
            int playerScore = player.GetTurnValue();
            uiManager.UpdateGoalText($"House must roll {playerScore} to win");
        }
        
        if (uiManager != null)
            uiManager.SetTurnMarkerToHouse();

        if (house != null)
        {
            house.OnTurnStart();
        }
        else
        {
            Debug.LogError("House is null! Cannot start house turn.");
        }
    }

    public void EndHouseTurn()
    {
        if (currentTurn != TurnState.HouseTurn)
        {
            Debug.LogWarning($"EndHouseTurn called but currentTurn is {currentTurn}, not HouseTurn. Ignoring.");
            return;
        }
        
        if (isProcessingTurn)
        {
            Debug.LogWarning("EndHouseTurn called but already processing turn transition. Ignoring.");
            return;
        }

        Debug.Log("House's turn ended");
        isProcessingTurn = true;
        StartPlayerTurn();
        isProcessingTurn = false;
    }
    
    #endregion

    #region Game Outcomes

    public void PlayerBust()
    {
        Debug.Log("GAME OVER - Player busted!");
        
        if (uiManager != null)
            uiManager.ClearScoreText();
        
        CheckGameOver();
    }

    public void HouseBust()
    {
        Debug.Log("PLAYER WINS - House busted!");
        
        if (player != null && house != null)
        {
            int betAmount = player.GetBetAmount();
            int totalPayout = house.PayWinnings(betAmount);
            player.AddMoney(totalPayout);
        }
        
        if (uiManager != null)
            uiManager.ClearScoreText();
        
        CheckGameOver();
    }

    public void HouseWins()
    {
        Debug.Log("HOUSE WINS - House beat player's score!");
        
        if (uiManager != null)
            uiManager.ClearScoreText();
        
        CheckGameOver();
    }

    public void PlayerOutOfMoney()
    {
        Debug.Log("GAME OVER - Player is out of money!");
        currentTurn = TurnState.GameOver;
        
        if (uiManager != null)
            uiManager.ShowGameOverPanel();
    }

    private void CheckGameOver()
    {
        if (player != null && player.GetCurrentMoney() < smallBetAmount)
        {
            Debug.Log("GAME OVER - Player cannot afford even the smallest bet!");
            currentTurn = TurnState.GameOver;
            
            if (uiManager != null)
                uiManager.ShowGameOverPanel();
            return;
        }

        if (house != null && house.GetCurrentMoney() <= 0)
        {
            Debug.Log("GAME OVER - Player wins! House is out of money!");
            currentTurn = TurnState.GameOver;
            
            if (uiManager != null)
                uiManager.ShowGameOverPanel();
            return;
        }

        // If we got here, game is not over - start a new round
        StartCoroutine(StartNewRoundAfterDelay());
    }
    
    #endregion

    #region Betting

    public void OnSmallBetSelected()
    {
        if (player == null) return;

        if (player.GetCurrentMoney() < smallBetAmount)
        {
            Debug.Log($"Cannot afford small bet of {smallBetAmount}!");
            PlayerOutOfMoney();
            return;
        }

        Debug.Log($"Player selected small bet: {smallBetAmount}");
        StartCoroutine(SwitchToGameplayMode());
        player.OnTurnStart(smallBetAmount);
    }

    public void OnLargeBetSelected()
    {
        if (player == null) return;

        if (player.GetCurrentMoney() < largeBetAmount)
        {
            Debug.Log($"Cannot afford large bet of {largeBetAmount}!");
            return;
        }

        Debug.Log($"Player selected large bet: {largeBetAmount}");
        StartCoroutine(SwitchToGameplayMode());
        player.OnTurnStart(largeBetAmount);
    }
    
    private IEnumerator SwitchToGameplayMode()
    {
        if (uiManager == null) yield break;
        
        isBettingMode = false;
        
        yield return StartCoroutine(uiManager.SwitchToGameplayButtons(
            () => { if (player != null) player.Stand(); },
            () => { if (player != null) player.RollDice(); }
        ));
    }

    public void OnStartNewRound()
    {
        Debug.Log("Starting new round...");

        // Reset turn state for new round
        currentTurn = TurnState.PlayerTurn;
        isBettingMode = true;

        roundManager.CountUpRound();

        ShowBetSelectionPanel();
               
        if (house != null)
        {
            house.ResetTurnValue();
        }
        
        if (uiManager != null)
            uiManager.ClearScoreText();
    }

    private IEnumerator StartNewRoundAfterDelay()
    {
        yield return new WaitForSeconds(newRoundDelay);
        OnStartNewRound();
    }
    
    #endregion

    #region Game Control

    public void RestartGame()
    {
        Debug.Log("Restarting game...");
        
        ResetGameState();
        ResetPlayers();
        
        if (diceManager != null)
            diceManager.RefreshDiceIdlePositions();
        
        // Reinitialize and show bet selection
        roundManager.InitializeRound();
        ShowBetSelectionPanel();
    }

    private void ResetGameState()
    {
        currentTurn = TurnState.PlayerTurn;
        isProcessingTurn = false;
        isBettingMode = true;
        
        if (uiManager != null)
        {
            uiManager.HideGameOverPanel();
            uiManager.DeactivateButtons();
        }
    }

    private void ResetPlayers()
    {
        if (player != null)
            player.ResetMoney();

        if (house != null)
        {
            house.ResetMoney();
            house.ResetTurnValue();
        }
    }
    
    #endregion

    #region UI Helper

    private void ShowBetSelectionPanel()
    {
        isBettingMode = true;
        
        if (uiManager != null)
        {
            uiManager.ShowBetSelection(smallBetAmount, largeBetAmount, OnSmallBetSelected, OnLargeBetSelected);
        }
    }
    
    #endregion

    #region Dice Management

    public void RollSharedDice(System.Action<int, int> onComplete, bool isPlayerTurn)
    {
        if (diceManager == null || diceManager.IsDiceRolling()) return;
        StartCoroutine(RollDiceCoroutine(onComplete, isPlayerTurn));
    }

    private IEnumerator RollDiceCoroutine(System.Action<int, int> onComplete, bool isPlayerTurn)
    {
        // Disable both buttons during rolling
        if (!isBettingMode && isPlayerTurn && uiManager != null)
        {
            uiManager.DisableGameplayButtons();
        }

        // Roll dice and wait for callback
        bool rollComplete = false;
        int resultA = 0;
        int resultB = 0;

        diceManager.RollDice((a, b) =>
        {
            resultA = a;
            resultB = b;
            rollComplete = true;
        }, isPlayerTurn);

        // Wait for roll to complete
        while (!rollComplete)
        {
            yield return null;
        }

        // Re-enable buttons after dice finish rolling (for player turn only)
        if (!isBettingMode && isPlayerTurn)
        {
            if (uiManager != null)
            {
                // Only enable Stand button if player has rolled this turn
                if (player != null && player.HasRolledThisTurn())
                    uiManager.EnableStandButton();
                uiManager.EnableRollButton();
            }
        }

        // Callback with results
        onComplete?.Invoke(resultA, resultB);
    }
    
    #endregion

    #region Public API

    public TurnState GetCurrentTurn() => currentTurn;
    
    public bool IsPlayerTurn() => currentTurn == TurnState.PlayerTurn;
    
    public bool IsHouseTurn() => currentTurn == TurnState.HouseTurn;

    public bool IsDiceRolling() => diceManager != null && diceManager.IsDiceRolling();
    
    public void DisableGameplayButtons()
    {
        if (isBettingMode) return;
        
        if (uiManager != null)
            uiManager.DisableGameplayButtons();
    }
    
    #endregion
}
