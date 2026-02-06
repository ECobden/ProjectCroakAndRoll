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

    [Header("Shared Dice")]
    [SerializeField] private GameObject dicePrefab;
    [SerializeField] private Transform diceParent;
    [SerializeField] private DB_DiceTargetArea diceTargetArea;
    [SerializeField] private Transform diceIdlePositionA;
    [SerializeField] private Transform diceIdlePositionB;
    [SerializeField] private Transform playerLaunchPositionA;
    [SerializeField] private Transform playerLaunchPositionB;
    [SerializeField] private Transform houseLaunchPositionA;
    [SerializeField] private Transform houseLaunchPositionB;

    [Header("UI References")]
    [SerializeField] private GameObject buttonPanel;
    [SerializeField] private GameObject betSelectionPanel;
    [SerializeField] private Button smallBetButton;
    [SerializeField] private Button largeBetButton;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI goalText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    [Header("Game Settings")]
    [SerializeField] private int smallBetAmount = 50;
    [SerializeField] private int largeBetAmount = 200;
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private float newRoundDelay = 1.5f;
    
    [Header("Score Animation")]
    [SerializeField] private float scorePunchScale = 1.3f;
    [SerializeField] private float scorePunchDuration = 0.3f;
    [SerializeField] private float scoreTransferDelay = 0.5f;
    [SerializeField] private float scoreTransferSpeed = 0.05f;
    
    #endregion

    #region Private Fields
    
    private DB_DiceController diceControllerA;
    private DB_DiceController diceControllerB;
    private bool isDiceRolling = false;
    
    private TurnState currentTurn = TurnState.PlayerTurn;
    private bool isProcessingTurn = false;
    
    private Coroutine scoreTransferCoroutine;
    private int currentTurnTotal = 0;
    
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
        SpawnSharedDice();
        InitializeSharedDice();
        RefreshDiceIdlePositions();
    }

    private void InitializeUI()
    {
        SetupButtonListeners();
        HideAllPanels();
        
        // Clear score text on start
        if (scoreText != null)
            scoreText.text = "";
    }

    private void SetupButtonListeners()
    {
        if (smallBetButton != null)
            smallBetButton.onClick.AddListener(OnSmallBetSelected);

        if (largeBetButton != null)
            largeBetButton.onClick.AddListener(OnLargeBetSelected);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    private void HideAllPanels()
    {
        if (betSelectionPanel != null)
            betSelectionPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
    
    #endregion

    #region Turn Management

    public void StartPlayerTurn()
    {
        if (isProcessingTurn) return;

        currentTurn = TurnState.PlayerTurn;
        currentTurnTotal = 0;
        
        UpdateGoalText("Roll Closest to 21");
    }

    public void EndPlayerTurn()
    {
        if (currentTurn != TurnState.PlayerTurn || isProcessingTurn) return;

        Debug.Log("Player's turn ended");
        StartHouseTurn();
    }

    public void StartHouseTurn()
    {
        if (isProcessingTurn) return;

        currentTurn = TurnState.HouseTurn;
        currentTurnTotal = 0;
        Debug.Log("House's turn");

        if (player != null)
        {
            int playerScore = player.GetTurnValue();
            UpdateGoalText($"House must roll {playerScore} to win");
        }

        if (house != null)
        {
            house.OnTurnStart();
        }
    }

    public void EndHouseTurn()
    {
        if (currentTurn != TurnState.HouseTurn || isProcessingTurn) return;

        Debug.Log("House's turn ended");
        StartPlayerTurn();
    }
    
    #endregion

    #region Game Outcomes

    public void PlayerBust()
    {
        Debug.Log("GAME OVER - Player busted!");
        
        // Clear score text
        if (scoreText != null)
            scoreText.text = "";
        currentTurnTotal = 0;
        
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
        
        // Clear score text
        if (scoreText != null)
            scoreText.text = "";
        currentTurnTotal = 0;
        
        CheckGameOver();
    }

    public void HouseWins()
    {
        Debug.Log("HOUSE WINS - House beat player's score!");
        
        // Clear score text
        if (scoreText != null)
            scoreText.text = "";
        currentTurnTotal = 0;
        
        CheckGameOver();
    }

    public void PlayerOutOfMoney()
    {
        Debug.Log("GAME OVER - Player is out of money!");
        currentTurn = TurnState.GameOver;
        ShowGameOverPanel();
    }

    private void CheckGameOver()
    {
        if (player != null && player.GetCurrentMoney() < smallBetAmount)
        {
            Debug.Log("GAME OVER - Player cannot afford even the smallest bet!");
            currentTurn = TurnState.GameOver;
            ShowGameOverPanel();
            return;
        }

        if (house != null && house.GetCurrentMoney() <= 0)
        {
            Debug.Log("GAME OVER - Player wins! House is out of money!");
            currentTurn = TurnState.GameOver;
            ShowGameOverPanel();
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

        HideBetSelectionPanel();
        Debug.Log($"Player selected small bet: {smallBetAmount}");
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

        HideBetSelectionPanel();
        Debug.Log($"Player selected large bet: {largeBetAmount}");
        player.OnTurnStart(largeBetAmount);
    }

    public void OnStartNewRound()
    {
        Debug.Log("Starting new round...");

        roundManager.CountUpRound();

        //Show bet selection for new round
        ShowBetSelectionPanel();
               
        if (house != null)
        {
            house.ResetTurnValue();
        }
        
        // Clear score text for new round
        if (scoreText != null)
            scoreText.text = "";
        currentTurnTotal = 0;
        
        //StartPlayerTurn();
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
        RefreshDiceIdlePositions();
    }

    private void ResetGameState()
    {
        currentTurn = TurnState.PlayerTurn;
        isProcessingTurn = false;
        
        HideBetSelectionPanel();
        HideGameOverPanel();
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

    #region UI Management

    private void ShowButtonPanel()
    {
        if (buttonPanel != null)
            buttonPanel.SetActive(true);
    }

    private void HideButtonPanel()
    {
        if (buttonPanel != null)
            buttonPanel.SetActive(false);
    }

    private void ShowBetSelectionPanel()
    {
        if (betSelectionPanel != null)
            betSelectionPanel.SetActive(true);
    }

    private void HideBetSelectionPanel()
    {
        if (betSelectionPanel != null)
            betSelectionPanel.SetActive(false);
    }

    private void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    private void HideGameOverPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void UpdateGoalText(string text)
    {
        if (goalText != null)
            goalText.text = text;
    }

    public void UpdateScoreText(int turnTotal)
    {
        // Calculate the roll value (difference from previous total)
        int rollValue = turnTotal - currentTurnTotal;
        currentTurnTotal = turnTotal;
        
        if (scoreText != null)
        {
            scoreText.text = rollValue.ToString();
            
            // Punch animation
            scoreText.transform.DOKill();
            scoreText.transform.localScale = Vector3.one;
            scoreText.transform.DOPunchScale(Vector3.one * (scorePunchScale - 1f), scorePunchDuration, 5, 0.5f);
        }
        
        // Stop any existing transfer animation
        if (scoreTransferCoroutine != null)
        {
            StopCoroutine(scoreTransferCoroutine);
        }
        
        // Start score transfer animation
        scoreTransferCoroutine = StartCoroutine(TransferScoreAnimation(rollValue, turnTotal));
    }
    
    private IEnumerator TransferScoreAnimation(int rollValue, int turnTotal)
    {
        // Wait for punch animation to finish plus delay
        yield return new WaitForSeconds(scorePunchDuration + scoreTransferDelay);
        
        // Get starting value for goal text animation (previous total)
        int startingTotal = turnTotal - rollValue;
        string playerName = IsPlayerTurn() ? "Player" : "House";
        bool isFirstRoll = (startingTotal == 0);
        
        // Simultaneously count down score text (roll value) and count up goal text (turn total)
        for (int i = 0; i <= rollValue; i++)
        {
            int remainingRoll = rollValue - i;
            int currentTotal = startingTotal + i;
            
            // Update score text (counting down the roll value)
            if (scoreText != null)
            {
                scoreText.text = remainingRoll > 0 ? remainingRoll.ToString() : "";
            }
            
            // Update goal text (counting up the turn total)
            // Only update to X/21 format if not first roll, or on the last iteration of first roll
            if (goalText != null && (!isFirstRoll || i == rollValue))
            {
                goalText.text = $"{playerName}: {currentTotal} / 21";
            }
            
            yield return new WaitForSeconds(scoreTransferSpeed);
        }
        
        scoreTransferCoroutine = null;
    }
    
    #endregion

    #region Dice Management

    private void SpawnSharedDice()
    {
        if (dicePrefab == null) return;

        if (diceControllerA == null)
        {
            GameObject diceInstanceA = Instantiate(dicePrefab, GetIdlePosition(diceIdlePositionA), Quaternion.identity, diceParent);
            diceControllerA = diceInstanceA.GetComponent<DB_DiceController>();
        }

        if (diceControllerB == null)
        {
            GameObject diceInstanceB = Instantiate(dicePrefab, GetIdlePosition(diceIdlePositionB), Quaternion.identity, diceParent);
            diceControllerB = diceInstanceB.GetComponent<DB_DiceController>();
        }
    }

    private void InitializeSharedDice()
    {
        if (diceControllerA != null)
        {
            diceControllerA.Initialize(GetIdlePosition(diceIdlePositionA));
            if (diceTargetArea != null)
                diceControllerA.SetTargetArea(diceTargetArea);
        }

        if (diceControllerB != null)
        {
            diceControllerB.Initialize(GetIdlePosition(diceIdlePositionB));
            if (diceTargetArea != null)
                diceControllerB.SetTargetArea(diceTargetArea);
        }
    }

    public void RefreshDiceIdlePositions()
    {
        if (diceControllerA != null)
            diceControllerA.SetIdlePosition(GetIdlePosition(diceIdlePositionA));

        if (diceControllerB != null)
            diceControllerB.SetIdlePosition(GetIdlePosition(diceIdlePositionB));
    }

    private Vector3 GetIdlePosition(Transform target)
    {
        return target != null ? target.position : Vector3.zero;
    }

    public void RollSharedDice(System.Action<int, int> onComplete, bool isPlayerTurn)
    {
        if (isDiceRolling) return;
        StartCoroutine(RollDiceCoroutine(onComplete, isPlayerTurn));
    }

    private IEnumerator RollDiceCoroutine(System.Action<int, int> onComplete, bool isPlayerTurn)
    {
        isDiceRolling = true;

        // Get appropriate launch positions based on turn
        Vector3 launchPosA = isPlayerTurn ? GetIdlePosition(playerLaunchPositionA) : GetIdlePosition(houseLaunchPositionA);
        Vector3 launchPosB = isPlayerTurn ? GetIdlePosition(playerLaunchPositionB) : GetIdlePosition(houseLaunchPositionB);

        // Tell dice to roll from launch positions
        if (diceControllerA != null)
            diceControllerA.RollFromLaunchPosition(launchPosA);

        if (diceControllerB != null)
            diceControllerB.RollFromLaunchPosition(launchPosB);

        // Wait for both dice to finish rolling
        while ((diceControllerA != null && diceControllerA.IsRolling()) ||
               (diceControllerB != null && diceControllerB.IsRolling()))
        {
            yield return null;
        }

        // Get dice values
        int diceAValue = diceControllerA != null ? diceControllerA.GetLastRollValue() : 0;
        int diceBValue = diceControllerB != null ? diceControllerB.GetLastRollValue() : 0;

        isDiceRolling = false;

        // Callback with results
        onComplete?.Invoke(diceAValue, diceBValue);
    }
    
    #endregion

    #region Public API

    public TurnState GetCurrentTurn() => currentTurn;
    
    public bool IsPlayerTurn() => currentTurn == TurnState.PlayerTurn;
    
    public bool IsHouseTurn() => currentTurn == TurnState.HouseTurn;

    public bool IsDiceRolling() => isDiceRolling;
    
    #endregion
}
