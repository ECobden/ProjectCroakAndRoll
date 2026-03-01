using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// Manages the collection of dice owned by a participant.
/// Handles drawing random dice for rolls and managing the inventory.
/// </summary>
public class DiceBag : MonoBehaviour
{
    [Header("Starting Dice")]
    [SerializeField] private List<DieData> startingDice = new List<DieData>();

    [Header("Settings")]
    [SerializeField] private int dicePerRoll = 2; // How many dice to draw per roll

    [Header("Visual Bag Display")]
    [SerializeField] private Transform spawnPoint; // Where dice appear when showing bag
    [SerializeField] private Transform gridCenter; // Center of the grid layout
    [SerializeField] private Vector2 gridSpacing = new Vector2(1f, 1f); // Space between dice in X and Z
    [SerializeField] private int gridColumns = 5; // Number of columns in the grid
    [SerializeField] private float instantiationDelay = 0.1f; // Delay between instantiating each die
    [SerializeField] private float positioningDelay = 0.05f; // Delay between moving each die to grid

    private List<DieData> diceCollection = new List<DieData>();
    private List<DB_DiceController> instantiatedDice = new List<DB_DiceController>(); // Track instantiated dice
    private System.Random random = new System.Random();
    private bool isBagOpen = false;

    #region Lifecycle

    private void Start()
    {
        InitializeBag();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize the dice bag with starting dice.
    /// </summary>
    public void InitializeBag()
    {
        diceCollection.Clear();
        
        if (startingDice != null)
        {
            diceCollection.AddRange(startingDice);
            Debug.Log($"Dice bag initialized with {diceCollection.Count} dice");
        }
        else
        {
            Debug.LogWarning("No starting dice assigned to DiceBag!");
        }
    }

    #endregion

    #region Drawing Dice

    /// <summary>
    /// Draw a random dice from the bag for rolling.
    /// </summary>
    public DieData DrawRandomDie()
    {
        if (diceCollection.Count == 0)
        {
            Debug.LogWarning("No dice in bag!");
            return null;
        }

        int index = random.Next(diceCollection.Count);
        return diceCollection[index];
    }

    /// <summary>
    /// Draw multiple random dice from the bag.
    /// </summary>
    public List<DieData> DrawRandomDice(int count)
    {
        List<DieData> drawn = new List<DieData>();

        if (diceCollection.Count == 0)
        {
            Debug.LogWarning("No dice in bag!");
            return drawn;
        }

        for (int i = 0; i < count && i < diceCollection.Count; i++)
        {
            int index = random.Next(diceCollection.Count);
            drawn.Add(diceCollection[index]);
        }

        Debug.Log($"Drew {drawn.Count} dice from bag. Bag now contains {diceCollection.Count} dice");
        return drawn;
    }

    /// <summary>
    /// Draw the default number of dice for a roll.
    /// </summary>
    public List<DieData> DrawRollDice()
    {
        return DrawRandomDice(dicePerRoll);
    }

    #endregion

    #region Inventory Management

    /// <summary>
    /// Add a die to the collection.
    /// </summary>
    public void AddDie(DieData die)
    {
        if (die != null)
        {
            diceCollection.Add(die);
            Debug.Log($"Added {die.dieName} to dice bag. Total dice: {diceCollection.Count}");
        }
    }

    /// <summary>
    /// Remove a specific die from the collection.
    /// </summary>
    public bool RemoveDie(DieData die)
    {
        if (diceCollection.Remove(die))
        {
            Debug.Log($"Removed {die.dieName} from dice bag. Total dice: {diceCollection.Count}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Get all dice in the bag.
    /// </summary>
    public List<DieData> GetAllDice()
    {
        return new List<DieData>(diceCollection);
    }

    /// <summary>
    /// Get the total count of dice in the bag.
    /// </summary>
    public int GetDiceCount()
    {
        return diceCollection.Count;
    }

    /// <summary>
    /// Count how many of a specific die type are in the bag.
    /// </summary>
    public int CountDieType(DieData die)
    {
        return diceCollection.Count(d => d == die);
    }

    /// <summary>
    /// Clear all dice from the bag.
    /// </summary>
    public void ClearBag()
    {
        diceCollection.Clear();
        Debug.Log("Dice bag cleared");
    }

    #endregion

    #region Visual Bag Display

    /// <summary>
    /// Show the dice bag by instantiating all dice and positioning them in a grid.
    /// </summary>
    public void ShowBag()
    {
        if (isBagOpen)
        {
            Debug.LogWarning("Dice bag is already open!");
            return;
        }

        if (diceCollection.Count == 0)
        {
            Debug.LogWarning("No dice to display! Dice bag is empty.");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point is not assigned! Cannot display dice bag.");
            return;
        }

        // Validate that all dice have prefabs assigned
        foreach (var die in diceCollection)
        {
            if (die.diePrefab == null)
            {
                Debug.LogError($"Die '{die.dieName}' does not have a prefab assigned!");
                return;
            }
        }

        isBagOpen = true;
        instantiatedDice.Clear();
        StartCoroutine(ShowBagCoroutine());
    }

    /// <summary>
    /// Close the dice bag by moving all dice back to spawn position and destroying them.
    /// </summary>
    public void CloseBag()
    {
        if (!isBagOpen)
        {
            Debug.LogWarning("Dice bag is not open!");
            return;
        }

        isBagOpen = false;
        StartCoroutine(CloseBagCoroutine());
    }

    /// <summary>
    /// Coroutine to instantiate dice and move them to grid positions one by one.
    /// </summary>
    private IEnumerator ShowBagCoroutine()
    {
        // Instantiate all dice at spawn point
        for (int i = 0; i < diceCollection.Count; i++)
        {
            DieData dieData = diceCollection[i];
            GameObject dicInstance = Instantiate(dieData.diePrefab, spawnPoint.position, Quaternion.identity);
            DB_DiceController diceController = dicInstance.GetComponent<DB_DiceController>();
            
            if (diceController != null)
            {
                // Disable colliders to prevent interaction
                Collider[] colliders = dicInstance.GetComponentsInChildren<Collider>();
                foreach (var collider in colliders)
                {
                    collider.enabled = false;
                }

                // Disable gravity and make kinematic to prevent falling
                Rigidbody rb = dicInstance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                // Set rotation to show highest face value
                int highestFaceValue = GetHighestFaceValue(dieData);
                diceController.SetRotationForFaceValue(highestFaceValue);

                instantiatedDice.Add(diceController);
                diceController.Initialize(spawnPoint.position);
            }
            else
            {
                Debug.LogError("Instantiated dice does not have DB_DiceController component!");
                Destroy(dicInstance);
            }

            yield return new WaitForSeconds(instantiationDelay);
        }

        // Move dice to grid positions one by one
        foreach (var diceController in instantiatedDice)
        {
            Vector3 gridPosition = CalculateGridPosition(instantiatedDice.IndexOf(diceController));
            StartCoroutine(MoveDiceToPosition(diceController, gridPosition));
            yield return new WaitForSeconds(positioningDelay);
        }

        Debug.Log($"Dice bag opened with {instantiatedDice.Count} dice displayed");
    }

    /// <summary>
    /// Coroutine to move dice back to spawn position and destroy them in reverse order.
    /// </summary>
    private IEnumerator CloseBagCoroutine()
    {
        // Move dice back to spawn position in reverse order
        for (int i = instantiatedDice.Count - 1; i >= 0; i--)
        {
            if (instantiatedDice[i] != null)
            {
                StartCoroutine(MoveDiceToPosition(instantiatedDice[i], spawnPoint.position, true));
                yield return new WaitForSeconds(positioningDelay);
            }
        }

        // Wait for all dice to finish moving back
        yield return new WaitForSeconds(0.5f);

        // Destroy all dice
        for (int i = instantiatedDice.Count - 1; i >= 0; i--)
        {
            if (instantiatedDice[i] != null)
            {
                Destroy(instantiatedDice[i].gameObject);
            }
        }

        instantiatedDice.Clear();
        Debug.Log("Dice bag closed");
    }

    /// <summary>
    /// Move a die to a target position using the DB_DiceController's lerp method.
    /// </summary>
    private IEnumerator MoveDiceToPosition(DB_DiceController diceController, Vector3 targetPosition, bool destroy = false)
    {
        if (diceController == null)
            yield break;

        diceController.LerpToPosition(targetPosition);

        // Wait for the lerp to complete (matching moveLerpDuration from DB_DiceController)
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// Calculate the grid position for a die based on its index.
    /// </summary>
    private Vector3 CalculateGridPosition(int index)
    {
        int row = index / gridColumns;
        int column = index % gridColumns;

        float offsetX = (column - (gridColumns - 1) * 0.5f) * gridSpacing.x;
        float offsetZ = -row * gridSpacing.y;

        Vector3 centerPos = gridCenter != null ? gridCenter.position : Vector3.zero;
        return centerPos + new Vector3(offsetX, 0f, offsetZ);
    }

    /// <summary>
    /// Get the highest face value from a die's face values.
    /// </summary>
    private int GetHighestFaceValue(DieData dieData)
    {
        if (dieData == null || dieData.faceValues == null || dieData.faceValues.Length == 0)
        {
            Debug.LogWarning("DieData has no face values, defaulting to 6");
            return 6;
        }

        int highest = dieData.faceValues[0];
        foreach (int value in dieData.faceValues)
        {
            if (value > highest)
            {
                highest = value;
            }
        }

        return highest;
    }

    #endregion

    #region Utility

    /// <summary>
    /// Get a summary of what's in the bag.
    /// </summary>
    public string GetBagSummary()
    {
        if (diceCollection.Count == 0)
            return "Dice Bag is empty";

        // Group by die type
        var grouped = diceCollection
            .GroupBy(d => d.dieName)
            .Select(g => $"{g.Key} x{g.Count()}");

        return "Dice: " + string.Join(", ", grouped);
    }

    #endregion

    #region Gizmos

    /// <summary>
    /// Draw gizmos for grid visualization in the editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            // Draw spawn point
            if (spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(spawnPoint.position, 0.02f);
                Gizmos.DrawWireCube(spawnPoint.position, Vector3.one * 0.04f);
            }

            // Draw grid preview
            DrawGridGizmos();
        }
    }

    /// <summary>
    /// Draw the grid layout for dice positioning.
    /// </summary>
    private void DrawGridGizmos()
    {
        if (startingDice.Count == 0)
            return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);

        // Draw grid lines
        int rows = (startingDice.Count + gridColumns - 1) / gridColumns;

        // Draw horizontal lines (along X axis)
        Vector3 centerPos = gridCenter != null ? gridCenter.position : Vector3.zero;
        for (int row = 0; row <= rows; row++)
        {
            float offsetZ = -row * gridSpacing.y;
            float startX = centerPos.x - (gridColumns - 1) * 0.5f * gridSpacing.x;
            float endX = centerPos.x + (gridColumns - 1) * 0.5f * gridSpacing.x;

            Gizmos.DrawLine(
                new Vector3(startX, centerPos.y, centerPos.z + offsetZ),
                new Vector3(endX, centerPos.y, centerPos.z + offsetZ)
            );
        }

        // Draw vertical lines (along Z axis)
        for (int col = 0; col <= gridColumns; col++)
        {
            float offsetX = (col - (gridColumns - 1) * 0.5f) * gridSpacing.x;
            float startZ = centerPos.z;
            float endZ = centerPos.z - (rows - 1) * gridSpacing.y;

            Gizmos.DrawLine(
                new Vector3(centerPos.x + offsetX, centerPos.y, startZ),
                new Vector3(centerPos.x + offsetX, centerPos.y, endZ)
            );
        }

        // Draw grid center point
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(centerPos, 0.15f);

        // Draw preview dice positions
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        for (int i = 0; i < startingDice.Count; i++)
        {
            Vector3 gridPos = CalculateGridPosition(i);
            Gizmos.DrawWireCube(gridPos, Vector3.one * 0.03f);
        }
    }

    #endregion
}
