using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// House (Opponent) entity - inherits common game logic from Participant base class.
/// Contains AI logic to make decisions whether to roll or stand.
/// </summary>
public class House : Participant
{
    [Header("Roll Settings")]
    [SerializeField] private float autoRollDelay = 1f;

    [Header("Turn State")]
    private int turnValue = 0;
    private int lastRollValue = 0;
    private int lastDiceA = 0;
    private int lastDiceB = 0;
    private int targetValue = 0;
    
    [Header("AI Settings")]
    [SerializeField] [Range(0f, 1f)] private float cautiousness = 0.7f; // How risk-averse (0=reckless, 1=very cautious)
    [SerializeField] private int safeThreshold = 17; // Total at which house becomes more cautious

    [Header("Stand Value")]
    [SerializeField] private int defaultStandValue = 17; // Default value the house must stand on
    private int currentStandValue = 17; // Current round's stand value

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
    }

    public override void OnRoundStart()
    {
        turnValue = 0;
        lastRollValue = 0;
        lastDiceA = 0;
        lastDiceB = 0;
        currentStandValue = defaultStandValue;
        InitializeRoundDiceAvailability();
        
        // Reset roll progress at round start
        if (uiManager != null)
        {
            uiManager.ResetGoalRollProgress();
        }
        
        // Target value is not used in alternating mode
        targetValue = 0;
        
        Debug.Log($"[ROUND START] House ready - Stand value: {currentStandValue}");
        
        if (gameManager == null)
        {
            Debug.LogError("GameManager is null in House.OnRoundStart! Cannot proceed.");
            return;
        }
    }

    public void BeginTurn()
    {
        if (!HasStood())
        {
            StartCoroutine(AutoRollAfterDelay());
        }
    }

    private IEnumerator AutoRollAfterDelay()
    {
        Debug.Log($"House will roll after {autoRollDelay} seconds");
        yield return new WaitForSeconds(autoRollDelay);
        Debug.Log("House is now rolling dice");
        RollDice();
    }

    public override void RollDice()
    {
        if (gameManager == null)
        {
            Debug.LogError("GameManager is null in House.RollDice!");
            return;
        }
        
        // Silently skip if dice are already rolling - this is normal during overlapping coroutines
        if (diceManager != null && diceManager.IsDiceRolling())
            return;
        
        // use AI to decide whether to roll or stand
        if (gameManager.GetCurrentState() == DB_GameManager.GameState.PlayRound)
        {
            if (ShouldHouseStand())
            {
                Debug.Log($"[HOUSE AI] Deciding to STAND with {GetTurnValue()}");
                Stand();
                return;
            }
            
            Debug.Log($"[HOUSE AI] Deciding to ROLL (current: {GetTurnValue()}, target: {GetPlayerScore()})");
        }

        int drawnCount = ConsumeRoundDiceForRoll(2, out List<DieData> selectedDice);
        if (drawnCount == 0)
        {
            Debug.Log("[HOUSE] No dice available in round pool. Auto-standing.");
            Stand();
            return;
        }
        
        Debug.Log("House calling diceManager.RollDiceAndGetResults");
        StartCoroutine(diceManager.RollDiceAndGetResults(OnDiceRolled, false, selectedDice)); // false = house turn
    }

    private void OnDiceRolled(int diceAValue, int diceBValue)
    {
        lastDiceA = diceAValue;
        lastDiceB = diceBValue;
        lastRollValue = diceAValue + diceBValue;
        
        // Handle roll through game manager for alternating mode
        if (gameManager != null)
        {

            RecordRoll(diceAValue, diceBValue);
            gameManager.OnParticipantRolled(false, diceAValue, diceBValue);
        }
    }

    public int GetTurnValue()
    {
        return turnValue;
    }

    public void ResetTurnValue()
    {
        turnValue = 0;
        lastRollValue = 0;
        lastDiceA = 0;
        lastDiceB = 0;
    }

    /// <summary>
    /// Set the stand value for this round. This is the threshold at which the house must stand.
    /// </summary>
    public void SetStandValue(int newStandValue)
    {
        currentStandValue = newStandValue;
        Debug.Log($"[HOUSE] Stand value set to {currentStandValue}");
    }

    /// <summary>
    /// Get the current stand value.
    /// </summary>
    public int GetStandValue()
    {
        return currentStandValue;
    }
    
    #region AI Decision Making
    
    /// <summary>
    /// AI logic to decide if house should stand based on risk assessment
    /// </summary>
    private bool ShouldHouseStand()
    {
        if (gameManager == null) return false;

        if (GetRoundAvailableDiceCount() <= 0)
        {
            return true;
        }
        
        int houseTotal = gameManager.GetHouseRoundTotal();
        int playerTotal = GetPlayerScore();
        List<DieData> availableDice = GetRoundAvailableDice();
        
        // Always roll if we have nothing
        if (houseTotal == 0) return false;

        // Mandatory stand at stand value threshold
        if (houseTotal >= currentStandValue)
        {
            Debug.Log($"[HOUSE AI] Reached stand value threshold ({houseTotal} >= {currentStandValue}) - MUST STAND");
            return true;
        }
        
        // Already won - stand
        if (houseTotal >= playerTotal && houseTotal <= 21)
        {
            Debug.Log($"[HOUSE AI] Already beating player ({houseTotal} >= {playerTotal}) - STAND");
            return true;
        }
        
        // Can't possibly win without rolling
        if (houseTotal < playerTotal)
        {
            // Calculate bust probability
            float bustProbability = CalculateBustProbability(houseTotal, availableDice);
            
            // Calculate win probability (need to beat player without busting)
            float winProbability = CalculateWinProbability(houseTotal, playerTotal, availableDice);
            
            Debug.Log($"[HOUSE AI] Analysis - Bust: {bustProbability:P0}, Win: {winProbability:P0}");
            
            // Decision based on risk tolerance
            // If bust probability is too high relative to cautiousness, stand
            if (bustProbability > (1f - cautiousness) && houseTotal >= safeThreshold)
            {
                Debug.Log($"[HOUSE AI] Too risky to continue (bust prob: {bustProbability:P0}) - STAND");
                return true;
            }
            
            // If we're close and the gap is small, be more cautious
            int gap = playerTotal - houseTotal;
            if (houseTotal >= safeThreshold && gap <= 3 && bustProbability > 0.5f)
            {
                Debug.Log($"[HOUSE AI] Close to player, high risk, standing - STAND");
                return true;
            }
            
            // Otherwise, take the risk and roll
            return false;
        }
        
        // Default: stand if at safe threshold or above
        return houseTotal >= safeThreshold;
    }
    
    /// <summary>
    /// Calculate probability of busting on next roll
    /// </summary>
    private float CalculateBustProbability(int currentTotal, List<DieData> availableDice)
    {
        if (currentTotal >= 21) return 1f;
        if (availableDice == null || availableDice.Count == 0) return 0f;
        
        float bustProbability = 0f;
        Dictionary<int, float> rollProbabilities = BuildRollTotalProbabilities(availableDice);
        
        foreach (var kvp in rollProbabilities)
        {
            if (currentTotal + kvp.Key > 21)
            {
                bustProbability += kvp.Value;
            }
        }

        return Mathf.Clamp01(bustProbability);
    }
    
    /// <summary>
    /// Calculate probability of winning (beating player without busting)
    /// </summary>
    private float CalculateWinProbability(int currentTotal, int playerTotal, List<DieData> availableDice)
    {
        if (currentTotal >= playerTotal && currentTotal <= 21) return 1f; // Already winning
        if (currentTotal > 21) return 0f; // Already bust
        if (availableDice == null || availableDice.Count == 0) return 0f;
        
        float winProbability = 0f;
        Dictionary<int, float> rollProbabilities = BuildRollTotalProbabilities(availableDice);

        foreach (var kvp in rollProbabilities)
        {
            int newTotal = currentTotal + kvp.Key;
            if (newTotal >= playerTotal && newTotal <= 21)
            {
                winProbability += kvp.Value;
            }
        }

        return Mathf.Clamp01(winProbability);
    }

    /// <summary>
    /// Build probability distribution for next roll total using remaining round-available dice.
    /// Draws up to 2 dice without replacement.
    /// </summary>
    private Dictionary<int, float> BuildRollTotalProbabilities(List<DieData> availableDice)
    {
        Dictionary<int, float> rollTotalToProbability = new Dictionary<int, float>();

        if (availableDice == null || availableDice.Count == 0)
        {
            return rollTotalToProbability;
        }

        if (availableDice.Count == 1)
        {
            int[] faces = GetDieFaces(availableDice[0]);
            float faceProbability = 1f / faces.Length;
            for (int faceIndex = 0; faceIndex < faces.Length; faceIndex++)
            {
                int rollTotal = faces[faceIndex];
                if (!rollTotalToProbability.ContainsKey(rollTotal))
                    rollTotalToProbability[rollTotal] = 0f;

                rollTotalToProbability[rollTotal] += faceProbability;
            }

            return rollTotalToProbability;
        }

        int diceCount = availableDice.Count;
        float pairDrawProbability = 2f / (diceCount * (diceCount - 1));

        for (int i = 0; i < diceCount - 1; i++)
        {
            int[] facesA = GetDieFaces(availableDice[i]);

            for (int j = i + 1; j < diceCount; j++)
            {
                int[] facesB = GetDieFaces(availableDice[j]);
                float faceOutcomeProbability = pairDrawProbability / (facesA.Length * facesB.Length);

                for (int faceA = 0; faceA < facesA.Length; faceA++)
                {
                    for (int faceB = 0; faceB < facesB.Length; faceB++)
                    {
                        int rollTotal = facesA[faceA] + facesB[faceB];
                        if (!rollTotalToProbability.ContainsKey(rollTotal))
                            rollTotalToProbability[rollTotal] = 0f;

                        rollTotalToProbability[rollTotal] += faceOutcomeProbability;
                    }
                }
            }
        }

        float totalProbability = rollTotalToProbability.Values.Sum();
        if (totalProbability > 0f)
        {
            List<int> keys = new List<int>(rollTotalToProbability.Keys);
            foreach (int key in keys)
            {
                rollTotalToProbability[key] /= totalProbability;
            }
        }

        return rollTotalToProbability;
    }

    private int[] GetDieFaces(DieData die)
    {
        if (die != null && die.faceValues != null && die.faceValues.Length > 0)
        {
            return die.faceValues;
        }

        return new int[] { 1, 2, 3, 4, 5, 6 };
    }
    
    /// <summary>
    /// Get the player's current score
    /// </summary>
    private int GetPlayerScore()
    {
        if (gameManager == null) return 0;
        
        // Try to get from alternating round manager first
        int playerScore = gameManager.GetPlayerRoundTotal();
        if (playerScore > 0) return playerScore;
        
        // Fallback to legacy target value
        return targetValue;
    }
    
    #endregion
    
    public override void Stand()
    {
        Debug.Log($"[HOUSE STAND] House stands with {GetTurnValue()}");
        SetHasStood(true);
        gameManager.OnParticipantStood(false);
    }
}
