using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DB_DiceManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("Dice Setup")]
    [SerializeField] private GameObject dicePrefab;
    [SerializeField] private Transform diceParent;
    [SerializeField] private DB_DiceTargetArea diceTargetArea;

    [Header("Position References")]
    [SerializeField] private List<Transform> launchPositions = new List<Transform>();
    
    [Header("Scoring Positions")]
    [SerializeField] private ScoredDicePositioner playerScoringPositioner; // Player scored dice manager
    [SerializeField] private ScoredDicePositioner houseScoringPositioner;  // House scored dice manager
    
    [Header("UI Reference")]
    [SerializeField] private DB_UIManager uiManager; // Reference to UI manager for score animations
    
    [Header("Timing Settings")]
    [SerializeField] private float delayBeforeMovingToScoring = 0.5f; // Delay after dice settle before moving to scoring position
    
    [Header("Dice Spawn Offset")]
    [SerializeField] private float diceSpawnXOffset = 15.5f; // Horizontal offset to prevent dice spawning inside each other

    #endregion

    #region Private Fields

    private DB_DiceController diceControllerA;
    private DB_DiceController diceControllerB;
    private bool isDiceRolling = false;

    #endregion

    #region Initialization

    private void SpawnSharedDice(Vector3 spawnPosition)
    {
        if (dicePrefab == null)
        {
            Debug.LogError("Dice prefab is not assigned!");
            return;
        }

        if (diceControllerA == null)
        {
            GameObject diceInstanceA = Instantiate(dicePrefab, spawnPosition + Vector3.left * diceSpawnXOffset, Quaternion.identity, diceParent);
            diceControllerA = diceInstanceA.GetComponent<DB_DiceController>();
            Debug.Log("Spawned new dice A");
        }

        if (diceControllerB == null)
        {
            GameObject diceInstanceB = Instantiate(dicePrefab, spawnPosition + Vector3.right * diceSpawnXOffset, Quaternion.identity, diceParent);
            diceControllerB = diceInstanceB.GetComponent<DB_DiceController>();
            Debug.Log("Spawned new dice B");
        }
    }

    private void InitializeSharedDice(Vector3 launchPosition)
    {
        if (diceControllerA != null)
        {
            Vector3 offsetPosA = launchPosition + Vector3.left * diceSpawnXOffset;
            diceControllerA.Initialize(offsetPosA);
            if (diceTargetArea != null)
                diceControllerA.SetTargetArea(diceTargetArea);
        }

        if (diceControllerB != null)
        {
            Vector3 offsetPosB = launchPosition + Vector3.right * diceSpawnXOffset;
            diceControllerB.Initialize(offsetPosB);
            if (diceTargetArea != null)
                diceControllerB.SetTargetArea(diceTargetArea);
        }
    }

    #endregion

    #region Position Management
    /// <summary>
    /// Get a random launch position from the list
    /// </summary>
    private Vector3 GetRandomLaunchPosition()
    {
        if (launchPositions == null || launchPositions.Count == 0)
        {
            Debug.LogWarning("No launch positions set!");
            return Vector3.zero;
        }
        
        // Select a random position from the list
        int randomIndex = Random.Range(0, launchPositions.Count);
        Transform selectedPosition = launchPositions[randomIndex];
        
        return selectedPosition != null ? selectedPosition.position : Vector3.zero;
    }

    #endregion

    #region Dice Rolling

    /// <summary>
    /// Roll dice and handle all results (scoring, positioning, callbacks)
    /// </summary>
    public IEnumerator RollDiceAndGetResults(System.Action<int, int> onComplete, bool isPlayerTurn, List<DieData> selectedDice = null)
    {
        yield return StartCoroutine(RollDiceCoroutine(onComplete, isPlayerTurn, selectedDice));
    }

    /// <summary>
    /// Orchestrates the rolling and result handling phases
    /// </summary>
    private IEnumerator RollDiceCoroutine(System.Action<int, int> onComplete, bool isPlayerTurn, List<DieData> selectedDice)
    {
        if (isDiceRolling)
        {
            Debug.LogWarning("Dice are already rolling!");
            yield break;
        }

        int diceToRoll = selectedDice == null ? 2 : Mathf.Clamp(selectedDice.Count, 0, 2);
        if (diceToRoll <= 0)
        {
            onComplete?.Invoke(0, 0);
            yield break;
        }

        isDiceRolling = true;
        
        // Phase 1: Roll the dice
        Vector3 launchPos = GetRandomLaunchPosition();
        yield return StartCoroutine(PerformDiceRoll(launchPos, selectedDice));
        
        // Get dice values after rolling
        int diceAValue = diceControllerA != null ? diceControllerA.GetLastRollValue() : 0;
        int diceBValue = diceControllerB != null ? diceControllerB.GetLastRollValue() : 0;
        
        Debug.Log($"[DiceManager] Dice values after roll: A={diceAValue}, B={diceBValue}, Total={diceAValue + diceBValue}");
        
        // Phase 2: Handle results and scoring
        yield return StartCoroutine(ProcessDiceResults(diceAValue, diceBValue, onComplete, isPlayerTurn));
        
        isDiceRolling = false;
    }

    /// <summary>
    /// Phase 1: Spawn, initialize, and roll the dice
    /// </summary>
    private IEnumerator PerformDiceRoll(Vector3 launchPos, List<DieData> selectedDice)
    {
        int diceToRoll = selectedDice == null ? 2 : Mathf.Clamp(selectedDice.Count, 0, 2);

        // Spawn fresh dice at launch position
        SpawnSharedDice(launchPos);
        InitializeSharedDice(launchPos);

        if (diceToRoll >= 1 && diceControllerA != null && selectedDice != null && selectedDice.Count >= 1)
        {
            diceControllerA.SetDieData(selectedDice[0]);
        }

        if (diceToRoll >= 2 && diceControllerB != null && selectedDice != null && selectedDice.Count >= 2)
        {
            diceControllerB.SetDieData(selectedDice[1]);
        }

        // Tell dice to roll from their offset positions
        if (diceToRoll >= 1 && diceControllerA != null)
            diceControllerA.RollFromLaunchPosition(launchPos + Vector3.left * diceSpawnXOffset);

        if (diceToRoll >= 2 && diceControllerB != null)
            diceControllerB.RollFromLaunchPosition(launchPos + Vector3.right * diceSpawnXOffset);

        if (diceToRoll < 2 && diceControllerB != null)
        {
            diceControllerB.DestroyWithEffect();
            diceControllerB = null;
        }

        // Wait one frame to ensure roll coroutines have started
        yield return null;

        // Wait for both dice to finish rolling
        while ((diceControllerA != null && diceControllerA.IsRolling()) ||
               (diceControllerB != null && diceControllerB.IsRolling()))
        {
            yield return null;
        }
        
        // Wait one more frame to ensure values are fully set after IsRolling becomes false
        yield return null;
    }

    /// <summary>
    /// Phase 2: Show UI, move to scoring position, and invoke callback
    /// </summary>
    private IEnumerator ProcessDiceResults(int diceAValue, int diceBValue, System.Action<int, int> onComplete, bool isPlayerTurn)
    {

        // Show floating score UI immediately after dice stop rolling
        if (uiManager != null)
        {
            // Get current total before adding new dice
            ScoredDicePositioner currentPositioner = isPlayerTurn ? playerScoringPositioner : houseScoringPositioner;
            int currentTotal = currentPositioner != null ? currentPositioner.GetTotalScore() : 0;
            int rollTotal = diceAValue + diceBValue;
            Debug.Log($"[DiceManager] Sending rollTotal={rollTotal}");

            int projectedTotal = currentTotal + rollTotal;
            
            
            // Trigger floating score animation
            uiManager.UpdateRollScoreText(rollTotal);
        }

        // Wait a short delay before moving to scoring position
        yield return new WaitForSeconds(delayBeforeMovingToScoring);
        
        // Store references to current dice
        DB_DiceController currentDiceA = diceControllerA;
        DB_DiceController currentDiceB = diceControllerB;
        
        // Clear the main references so next roll can spawn new dice
        diceControllerA = null;
        diceControllerB = null;
        
        // Move dice to scoring position and wait for movement to complete
        yield return StartCoroutine(MoveDiceToScoringPositionCoroutine(currentDiceA, currentDiceB, isPlayerTurn));

        // Return results via callback - only after dice are in position
        onComplete?.Invoke(diceAValue, diceBValue);
    }

    // Legacy method for backward compatibility - can be removed if not used elsewhere
    public void RollDice(System.Action<int, int> onComplete, bool isPlayerTurn)
    {
        if (isDiceRolling) return;
        StartCoroutine(RollDiceAndGetResults(onComplete, isPlayerTurn, null));
    }

    #endregion

    #region Scoring Position Management
    
    /// <summary>
    /// Move rolled dice to scoring positions and wait for movement to complete
    /// </summary>
    private IEnumerator MoveDiceToScoringPositionCoroutine(DB_DiceController diceA, DB_DiceController diceB, bool isPlayerTurn)
    {
        ScoredDicePositioner positioner = isPlayerTurn ? playerScoringPositioner : houseScoringPositioner;
        
        if (positioner == null)
        {
            Debug.LogWarning($"Scoring positioner not set for {(isPlayerTurn ? "player" : "house")}! Dice will not be positioned.");
            yield break;
        }
        
        // Let the positioner handle the positioning and wait for movement to complete
        yield return StartCoroutine(positioner.AddDiceRowCoroutine(diceA, diceB));
        
        Debug.Log($"Added {(isPlayerTurn ? "player" : "house")} dice to scoring area via positioner");
    }
    
    /// <summary>
    /// Clear all scored dice (called at round start)
    /// </summary>
    public void ClearScoredDice()
    {
        // Clear player scored dice
        if (playerScoringPositioner != null)
            playerScoringPositioner.ClearAllDice();
        
        // Clear house scored dice
        if (houseScoringPositioner != null)
            houseScoringPositioner.ClearAllDice();
        
        // Clear current rolling dice
        if (diceControllerA != null)
        {
            diceControllerA.DestroyWithEffect();
            diceControllerA = null;
        }
        if (diceControllerB != null)
        {
            diceControllerB.DestroyWithEffect();
            diceControllerB = null;
        }
        
        Debug.Log("Cleared all scored dice");
    }
    
    #endregion

    #region Public API

    public bool IsDiceRolling() => isDiceRolling;
    
    public ScoredDicePositioner GetPlayerScoringPositioner() => playerScoringPositioner;
    
    public ScoredDicePositioner GetHouseScoringPositioner() => houseScoringPositioner;
    
    /// <summary>
    /// Get the current value of dice A
    /// </summary>
    public int GetDiceAValue()
    {
        return diceControllerA != null ? diceControllerA.GetLastRollValue() : 0;
    }
    
    /// <summary>
    /// Get the current value of dice B
    /// </summary>
    public int GetDiceBValue()
    {
        return diceControllerB != null ? diceControllerB.GetLastRollValue() : 0;
    }
    
    /// <summary>
    /// Flip both dice to show opposite faces with animation
    /// </summary>
    public void FlipBothDice(int newDiceAValue, int newDiceBValue)
    {
        if (diceControllerA != null)
            diceControllerA.FlipToOppositeFace(newDiceAValue);
            
        if (diceControllerB != null)
            diceControllerB.FlipToOppositeFace(newDiceBValue);
    }
    
    /// <summary>
    /// Flip only dice A to show opposite face with animation
    /// </summary>
    public void FlipDiceA(int newValue)
    {
        if (diceControllerA != null)
            diceControllerA.FlipToOppositeFace(newValue);
    }
    
    /// <summary>
    /// Flip only dice B to show opposite face with animation
    /// </summary>
    public void FlipDiceB(int newValue)
    {
        if (diceControllerB != null)
            diceControllerB.FlipToOppositeFace(newValue);
    }

    #endregion
}
