using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base class for both Player and House (Opponent).
/// Encapsulates common game logic including scoring areas, turn tracking, and stand state.
/// </summary>
public abstract class Participant : MonoBehaviour
{
    #region Serialized Fields

    [Header("Money System")]
    [SerializeField] protected int startingMoney = 1000;
    protected int currentMoney;

    [Header("Dice Collection")]
    [SerializeField] protected DiceBag diceBag;

    [Header("Manager References")]
    [SerializeField] protected DB_GameManager gameManager;
    [SerializeField] protected DB_DiceManager diceManager;
    [SerializeField] protected DB_UIManager uiManager;

    #endregion

    #region Protected Fields

    // Scoring
    protected int roundTotal = 0;
    protected List<(int diceA, int diceB)> rollHistory = new List<(int, int)>();

    // Turn state
    protected int rollCount = 0;
    protected bool hasStood = false;
    protected bool canAct = false;
    protected List<DieData> roundAvailableDice = new List<DieData>();
    protected List<DieData> lastConsumedRollDice = new List<DieData>();

    #endregion

    #region Lifecycle

    protected virtual void Awake()
    {
        ValidateReferences();
    }

    protected virtual void Start()
    {
        currentMoney = startingMoney;
    }

    #endregion

    #region Validation

    protected virtual void ValidateReferences()
    {
        if (gameManager == null) Debug.LogError($"GameManager not assigned to {gameObject.name}!");
        if (diceManager == null) Debug.LogError($"DiceManager not assigned to {gameObject.name}!");
        if (uiManager == null) Debug.LogError($"UIManager not assigned to {gameObject.name}!");
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Called when a new round starts. Implementation depends on whether this is player or AI.
    /// </summary>
    public abstract void OnRoundStart();

    /// <summary>
    /// Roll the dice. This should be called by the participant or game manager.
    /// </summary>
    public abstract void RollDice();

    /// <summary>
    /// Stand at the current score. Player triggers this via UI, AI triggers via logic.
    /// </summary>
    public abstract void Stand();

    #endregion

    #region Scoring System

    /// <summary>
    /// Records a roll in the scoring area (5x2 grid).
    /// </summary>
    public virtual void RecordRoll(int diceA, int diceB)
    {
        rollHistory.Add((diceA, diceB));
        rollCount++;
        roundTotal = diceA + diceB; // Note: In your game, this might accumulate differently
        Debug.Log($"{gameObject.name} rolled {diceA} + {diceB} = {roundTotal} (Roll #{rollCount})");
    }

    /// <summary>
    /// Get the current round total for this participant.
    /// </summary>
    public int GetRoundTotal()
    {
        return roundTotal;
    }

    /// <summary>
    /// Get the roll count for this round.
    /// </summary>
    public int GetRollCount()
    {
        return rollCount;
    }

    /// <summary>
    /// Get all rolls in the current round.
    /// </summary>
    public List<(int diceA, int diceB)> GetRollHistory()
    {
        return new List<(int, int)>(rollHistory);
    }

    /// <summary>
    /// Reset scoring for a new round.
    /// </summary>
    public virtual void ResetRound()
    {
        roundTotal = 0;
        rollCount = 0;
        rollHistory.Clear();
        hasStood = false;
        roundAvailableDice.Clear();
        lastConsumedRollDice.Clear();
        if (diceBag != null)
            diceBag.SetDisplayDiceOverride(null);
        Debug.Log($"{gameObject.name} reset for new round");
    }

    #endregion

    #region Turn State

    /// <summary>
    /// Returns whether this participant has chosen to stand.
    /// </summary>
    public bool HasStood()
    {
        return hasStood;
    }

    /// <summary>
    /// Set whether this participant has stood.
    /// </summary>
    protected void SetHasStood(bool stood)
    {
        hasStood = stood;
    }

    /// <summary>
    /// Check if participant can currently act (roll or stand).
    /// </summary>
    public bool CanAct()
    {
        return canAct;
    }

    /// <summary>
    /// Allow or disallow actions for this participant.
    /// </summary>
    protected void SetCanAct(bool can)
    {
        canAct = can;
    }

    /// <summary>
    /// Set whether this participant's turn is active.
    /// </summary>
    public void SetTurnActive(bool isActive)
    {
        canAct = isActive;
    }

    #endregion

    #region Money System

    /// <summary>
    /// Get current money for this participant.
    /// </summary>
    public int GetCurrentMoney()
    {
        return currentMoney;
    }

    /// <summary>
    /// Add money to this participant's balance.
    /// </summary>
    public virtual void AddMoney(int amount)
    {
        currentMoney += amount;
        Debug.Log($"{gameObject.name} received {amount}. Total: {currentMoney}");
    }

    /// <summary>
    /// Subtract money from this participant's balance.
    /// </summary>
    public virtual bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            Debug.Log($"{gameObject.name} spent {amount}. Remaining: {currentMoney}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Reset money to starting amount.
    /// </summary>
    public virtual void ResetMoney()
    {
        currentMoney = startingMoney;
    }

    #endregion

    #region Dice Bag Management

    /// <summary>
    /// Get the participant's dice bag.
    /// </summary>
    public DiceBag GetDiceBag()
    {
        return diceBag;
    }

    /// <summary>
    /// Add a die to the participant's collection.
    /// </summary>
    public void AddDieToBag(DieData die)
    {
        if (diceBag != null)
        {
            diceBag.AddDie(die);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} has no dice bag assigned!");
        }
    }

    /// <summary>
    /// Remove a die from the participant's collection.
    /// </summary>
    public bool RemoveDieFromBag(DieData die)
    {
        if (diceBag != null)
        {
            return diceBag.RemoveDie(die);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} has no dice bag assigned!");
            return false;
        }
    }

    /// <summary>
    /// Get the total number of dice in the participant's bag.
    /// </summary>
    public int GetDiceCount()
    {
        return diceBag != null ? diceBag.GetDiceCount() : 0;
    }

    /// <summary>
    /// Initializes temporary round-available dice from owned bag inventory.
    /// </summary>
    public virtual void InitializeRoundDiceAvailability()
    {
        if (diceBag == null)
        {
            roundAvailableDice.Clear();
            lastConsumedRollDice.Clear();
            Debug.LogWarning($"{gameObject.name} has no dice bag assigned!");
            return;
        }

        roundAvailableDice = diceBag.GetAllDice();
        lastConsumedRollDice.Clear();
        diceBag.SetDisplayDiceOverride(roundAvailableDice);
        Debug.Log($"{gameObject.name} round dice pool initialized with {roundAvailableDice.Count} dice");
    }

    /// <summary>
    /// Consume up to requested dice from this round's available pool.
    /// </summary>
    public int ConsumeRoundDiceForRoll(int requestedCount, out List<DieData> consumedDice)
    {
        consumedDice = new List<DieData>();

        if (requestedCount <= 0 || roundAvailableDice.Count == 0)
        {
            lastConsumedRollDice = consumedDice;
            return 0;
        }

        int toDraw = Mathf.Min(requestedCount, roundAvailableDice.Count);

        for (int i = 0; i < toDraw; i++)
        {
            int randomIndex = Random.Range(0, roundAvailableDice.Count);
            consumedDice.Add(roundAvailableDice[randomIndex]);
            roundAvailableDice.RemoveAt(randomIndex);
        }

        lastConsumedRollDice = new List<DieData>(consumedDice);
        if (diceBag != null)
            diceBag.SetDisplayDiceOverride(roundAvailableDice);
        return consumedDice.Count;
    }

    /// <summary>
    /// Gets remaining dice available to roll this round.
    /// </summary>
    public int GetRoundAvailableDiceCount()
    {
        return roundAvailableDice.Count;
    }

    /// <summary>
    /// Gets a copy of remaining round-available dice.
    /// </summary>
    public List<DieData> GetRoundAvailableDice()
    {
        return new List<DieData>(roundAvailableDice);
    }

    /// <summary>
    /// Gets the dice consumed on the last roll request.
    /// </summary>
    public List<DieData> GetLastConsumedRollDice()
    {
        return new List<DieData>(lastConsumedRollDice);
    }

    #endregion
}
