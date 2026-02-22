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
            GameObject diceInstanceA = Instantiate(dicePrefab, spawnPosition, Quaternion.identity, diceParent);
            diceControllerA = diceInstanceA.GetComponent<DB_DiceController>();
            Debug.Log("Spawned new dice A");
        }

        if (diceControllerB == null)
        {
            GameObject diceInstanceB = Instantiate(dicePrefab, spawnPosition, Quaternion.identity, diceParent);
            diceControllerB = diceInstanceB.GetComponent<DB_DiceController>();
            Debug.Log("Spawned new dice B");
        }
    }

    private void InitializeSharedDice(Vector3 launchPosition)
    {
        if (diceControllerA != null)
        {
            diceControllerA.Initialize(launchPosition);
            if (diceTargetArea != null)
                diceControllerA.SetTargetArea(diceTargetArea);
        }

        if (diceControllerB != null)
        {
            diceControllerB.Initialize(launchPosition);
            if (diceTargetArea != null)
                diceControllerB.SetTargetArea(diceTargetArea);
        }
    }

    #endregion

    #region Position Management

    public void RefreshDiceIdlePositions()
    {
        // Legacy method - no longer moves dice back to idle
        // Dice are now instantiated fresh for each roll
        // This method kept for compatibility but does nothing
        Debug.Log("RefreshDiceIdlePositions called (no-op in new system)");
    }
    
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

    public IEnumerator RollDiceAndGetResults(System.Action<int, int> onComplete, bool isPlayerTurn)
    {
        if (isDiceRolling)
        {
            Debug.LogWarning("Dice are already rolling!");
            yield break;
        }

        isDiceRolling = true;
        
        // Get random launch position (same for both dice)
        Vector3 launchPos = GetRandomLaunchPosition();
        
        // Spawn fresh dice at launch position
        SpawnSharedDice(launchPos);
        InitializeSharedDice(launchPos);

        // Tell dice to roll from the launch position
        if (diceControllerA != null)
            diceControllerA.RollFromLaunchPosition(launchPos);

        if (diceControllerB != null)
            diceControllerB.RollFromLaunchPosition(launchPos);

        // Wait for both dice to finish rolling
        while ((diceControllerA != null && diceControllerA.IsRolling()) ||
               (diceControllerB != null && diceControllerB.IsRolling()))
        {
            yield return null;
        }

        // Get dice values
        int diceAValue = diceControllerA != null ? diceControllerA.GetLastRollValue() : 0;
        int diceBValue = diceControllerB != null ? diceControllerB.GetLastRollValue() : 0;

        // Show floating score UI immediately after dice stop rolling
        if (uiManager != null)
        {
            // Get current total before adding new dice
            ScoredDicePositioner currentPositioner = isPlayerTurn ? playerScoringPositioner : houseScoringPositioner;
            int currentTotal = currentPositioner != null ? currentPositioner.GetTotalScore() : 0;
            int projectedTotal = currentTotal + diceAValue + diceBValue;
            
            // Trigger floating score animation
            uiManager.UpdateScoreText(projectedTotal, isPlayerTurn);
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
        
        isDiceRolling = false;

        // Return results via callback - only after dice are in position
        onComplete?.Invoke(diceAValue, diceBValue);
    }

    // Legacy method for backward compatibility - can be removed if not used elsewhere
    public void RollDice(System.Action<int, int> onComplete, bool isPlayerTurn)
    {
        if (isDiceRolling) return;
        StartCoroutine(RollDiceAndGetResults(onComplete, isPlayerTurn));
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
