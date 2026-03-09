using UnityEngine;

/// <summary>
/// Context object passed to dice ability lifecycle hooks.
/// Contains all necessary game state and participant references for ability execution.
/// </summary>
[System.Serializable]
public struct AbilityContext
{
    /// <summary>
    /// The participant who owns the die with this ability
    /// </summary>
    public Participant instigator;
    
    /// <summary>
    /// The opposing participant
    /// </summary>
    public Participant opponent;
    
    /// <summary>
    /// The value of the die face that was rolled (1-6 typically)
    /// </summary>
    public int diceValue;
    
    /// <summary>
    /// Reference to the game manager for state queries and game flow control
    /// </summary>
    public DB_GameManager gameManager;
    
    /// <summary>
    /// The die data that owns this ability (optional, for advanced queries)
    /// </summary>
    public DieData dieData;
    
    /// <summary>
    /// The index of the die in the roll (0 = first die, 1 = second die)
    /// Used to identify which physical dice controller this ability affects
    /// </summary>
    public int dieIndex;
    
    /// <summary>
    /// Current turn number (optional, for turn-based conditions)
    /// </summary>
    public int turnNumber;
    
    /// <summary>
    /// Round number (optional, for round-based conditions)
    /// </summary>
    public int roundNumber;

    /// <summary>
    /// Constructor for creating an ability context
    /// </summary>
    public AbilityContext(Participant instigator, Participant opponent, int diceValue, DB_GameManager gameManager, DieData dieData = null, int dieIndex = -1)
    {
        this.instigator = instigator;
        this.opponent = opponent;
        this.diceValue = diceValue;
        this.gameManager = gameManager;
        this.dieData = dieData;
        this.dieIndex = dieIndex;
        this.turnNumber = 0;
        this.roundNumber = 0;
    }

    /// <summary>
    /// Get the current round total score of the instigator
    /// </summary>
    public int GetInstigatorRoundTotal()
    {
        return instigator != null ? instigator.GetRoundTotal() : 0;
    }

    /// <summary>
    /// Get the current round total score of the opponent
    /// </summary>
    public int GetOpponentRoundTotal()
    {
        return opponent != null ? opponent.GetRoundTotal() : 0;
    }

    /// <summary>
    /// Get the current money of the instigator
    /// </summary>
    public int GetInstigatorMoney()
    {
        return instigator != null ? instigator.GetCurrentMoney() : 0;
    }

    /// <summary>
    /// Get the current money of the opponent
    /// </summary>
    public int GetOpponentMoney()
    {
        return opponent != null ? opponent.GetCurrentMoney() : 0;
    }

    /// <summary>
    /// Check if it's currently the instigator's turn
    /// </summary>
    public bool IsInstigatorTurn()
    {
        if (gameManager == null || instigator == null) return false;
        
        // Check if instigator is the player
        Participant player = gameManager.GetPlayerParticipant();
        bool instigatorIsPlayer = (instigator == player);
        
        // Check if it's currently player's turn
        bool isPlayerTurn = gameManager.IsPlayerTurn();
        
        return instigatorIsPlayer == isPlayerTurn;
    }
}
