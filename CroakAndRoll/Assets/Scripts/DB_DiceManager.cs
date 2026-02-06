using UnityEngine;
using System.Collections;

public class DB_DiceManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("Dice Setup")]
    [SerializeField] private GameObject dicePrefab;
    [SerializeField] private Transform diceParent;
    [SerializeField] private DB_DiceTargetArea diceTargetArea;

    [Header("Position References")]
    [SerializeField] private Transform diceIdlePositionA;
    [SerializeField] private Transform diceIdlePositionB;
    [SerializeField] private Transform playerLaunchPositionA;
    [SerializeField] private Transform playerLaunchPositionB;
    [SerializeField] private Transform houseLaunchPositionA;
    [SerializeField] private Transform houseLaunchPositionB;

    #endregion

    #region Private Fields

    private DB_DiceController diceControllerA;
    private DB_DiceController diceControllerB;
    private bool isDiceRolling = false;

    #endregion

    #region Initialization

    public void Initialize()
    {
        SpawnSharedDice();
        InitializeSharedDice();
        RefreshDiceIdlePositions();
    }

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

    #endregion

    #region Position Management

    public void RefreshDiceIdlePositions()
    {
        if (diceControllerA != null)
            diceControllerA.ReturnToIdlePosition();

        if (diceControllerB != null)
            diceControllerB.ReturnToIdlePosition();
    }

    private Vector3 GetIdlePosition(Transform target)
    {
        return target != null ? target.position : Vector3.zero;
    }

    #endregion

    #region Dice Rolling

    public void RollDice(System.Action<int, int> onComplete, bool isPlayerTurn)
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

    public bool IsDiceRolling() => isDiceRolling;

    #endregion
}
