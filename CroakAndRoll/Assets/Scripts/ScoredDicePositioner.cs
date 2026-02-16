using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages positioning and tracking of scored dice in rows.
/// Each roll creates a new row with 2 dice positions.
/// Can be used for both player and house scoring areas.
/// </summary>
public class ScoredDicePositioner : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Spacing Settings")]
    [SerializeField] private float rowSpacing = 0.3f;     // Z-axis spacing between rows
    [SerializeField] private float diceSpacing = 0.15f;   // X-axis spacing between dice in a row
    [SerializeField] private float heightOffset = 0.0f;   // Y-axis offset from base position
    
    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private float rotationDuration = 0.3f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Gizmo Settings")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.cyan;
    [SerializeField] private float gizmoSize = 0.05f;
    [SerializeField] private int maxRowsToShow = 10;
    
    #endregion
    
    #region Private Fields
    
    // Track all rows and their dice
    private List<DiceRow> diceRows = new List<DiceRow>();
    
    // Callback for when score changes
    private System.Action onScoreChanged;
    
    #endregion
    
    #region Nested Classes
    
    [System.Serializable]
    public class DiceRow
    {
        public DB_DiceController diceA;
        public DB_DiceController diceB;
        public Vector3 positionA;
        public Vector3 positionB;
        public int rowIndex;
        
        public DiceRow(int index)
        {
            rowIndex = index;
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Set callback for when score changes
    /// </summary>
    public void SetScoreChangedCallback(System.Action callback)
    {
        onScoreChanged = callback;
    }
    
    /// <summary>
    /// Add a pair of dice to a new row
    /// </summary>
    public void AddDiceRow(DB_DiceController diceA, DB_DiceController diceB)
    {
        StartCoroutine(AddDiceRowCoroutine(diceA, diceB));
    }
    
    /// <summary>
    /// Add a pair of dice to a new row and wait for movement to complete
    /// </summary>
    public IEnumerator AddDiceRowCoroutine(DB_DiceController diceA, DB_DiceController diceB)
    {
        if (diceA == null || diceB == null)
        {
            Debug.LogWarning("Cannot add dice row with null dice!");
            yield break;
        }
        
        int rowIndex = diceRows.Count;
        DiceRow newRow = new DiceRow(rowIndex);
        
        // Calculate positions for this row
        Vector3 basePosition = transform.position;
        Vector3 rowOffset = new Vector3(0, heightOffset, rowIndex * rowSpacing);
        
        newRow.positionA = basePosition + rowOffset + new Vector3(-diceSpacing / 2, 0, 0);
        newRow.positionB = basePosition + rowOffset + new Vector3(diceSpacing / 2, 0, 0);
        newRow.diceA = diceA;
        newRow.diceB = diceB;
        
        diceRows.Add(newRow);
        
        // Move and straighten the dice - start both coroutines
        Coroutine moveA = StartCoroutine(MoveDiceToPosition(diceA, newRow.positionA));
        Coroutine moveB = StartCoroutine(MoveDiceToPosition(diceB, newRow.positionB));
        
        // Wait for both movements to complete
        yield return moveA;
        yield return moveB;
        
        Debug.Log($"{gameObject.name}: Added row {rowIndex} with dice at positions {newRow.positionA} and {newRow.positionB}");
        
        // Notify that score has changed
        onScoreChanged?.Invoke();
    }
    
    /// <summary>
    /// Get the total number of rows
    /// </summary>
    public int GetRowCount()
    {
        return diceRows.Count;
    }
    
    /// <summary>
    /// Get a specific row
    /// </summary>
    public DiceRow GetRow(int index)
    {
        if (index >= 0 && index < diceRows.Count)
            return diceRows[index];
        return null;
    }
    
    /// <summary>
    /// Clear all dice and reset
    /// </summary>
    public void ClearAllDice()
    {
        foreach (var row in diceRows)
        {
            if (row.diceA != null)
                Destroy(row.diceA.gameObject);
            if (row.diceB != null)
                Destroy(row.diceB.gameObject);
        }
        
        diceRows.Clear();
        Debug.Log($"{gameObject.name}: Cleared all scored dice");
    }
    
    /// <summary>
    /// Get all dice values in the collection
    /// </summary>
    public List<int> GetAllDiceValues()
    {
        List<int> values = new List<int>();
        foreach (var row in diceRows)
        {
            if (row.diceA != null)
                values.Add(row.diceA.GetLastRollValue());
            if (row.diceB != null)
                values.Add(row.diceB.GetLastRollValue());
        }
        return values;
    }
    
    /// <summary>
    /// Get the last row (most recent roll)
    /// </summary>
    public DiceRow GetLastRow()
    {
        if (diceRows.Count == 0) return null;
        return diceRows[diceRows.Count - 1];
    }
    
    /// <summary>
    /// Remove a specific die from the collection
    /// </summary>
    public void RemoveDie(DB_DiceController die)
    {
        if (die == null) return;
        
        foreach (var row in diceRows)
        {
            if (row.diceA == die)
            {
                Destroy(row.diceA.gameObject);
                row.diceA = null;
                Debug.Log($"{gameObject.name}: Removed diceA from row {row.rowIndex}");
                
                // Notify that score has changed
                onScoreChanged?.Invoke();
                return;
            }
            if (row.diceB == die)
            {
                Destroy(row.diceB.gameObject);
                row.diceB = null;
                Debug.Log($"{gameObject.name}: Removed diceB from row {row.rowIndex}");
                
                // Notify that score has changed
                onScoreChanged?.Invoke();
                return;
            }
        }
    }
    
    /// <summary>
    /// Find a die by its value in the collection
    /// </summary>
    public DB_DiceController FindDieByValue(int value)
    {
        foreach (var row in diceRows)
        {
            if (row.diceA != null && row.diceA.GetLastRollValue() == value)
                return row.diceA;
            if (row.diceB != null && row.diceB.GetLastRollValue() == value)
                return row.diceB;
        }
        return null;
    }
    
    /// <summary>
    /// Get the total score of all dice
    /// </summary>
    public int GetTotalScore()
    {
        int total = 0;
        foreach (var row in diceRows)
        {
            if (row.diceA != null)
                total += row.diceA.GetLastRollValue();
            if (row.diceB != null)
                total += row.diceB.GetLastRollValue();
        }
        return total;
    }
    
    /// <summary>
    /// Highlight all dice with a specific value
    /// </summary>
    public void HighlightDiceWithValue(int value, Color highlightColor, System.Action<DB_DiceController> clickCallback)
    {
        foreach (var row in diceRows)
        {
            if (row.diceA != null && row.diceA.GetLastRollValue() == value)
            {
                row.diceA.Highlight(highlightColor);
                row.diceA.SetClickable(true, clickCallback);
            }
            if (row.diceB != null && row.diceB.GetLastRollValue() == value)
            {
                row.diceB.Highlight(highlightColor);
                row.diceB.SetClickable(true, clickCallback);
            }
        }
    }
    
    /// <summary>
    /// Highlight specific dice in the last row
    /// </summary>
    public void HighlightLastRowDice(bool highlightA, bool highlightB, Color highlightColor, System.Action<DB_DiceController> clickCallback)
    {
        var lastRow = GetLastRow();
        if (lastRow == null) return;
        
        if (highlightA && lastRow.diceA != null)
        {
            lastRow.diceA.Highlight(highlightColor);
            lastRow.diceA.SetClickable(true, clickCallback);
        }
        
        if (highlightB && lastRow.diceB != null)
        {
            lastRow.diceB.Highlight(highlightColor);
            lastRow.diceB.SetClickable(true, clickCallback);
        }
    }
    
    /// <summary>
    /// Remove all highlights and clickability
    /// </summary>
    public void ClearAllHighlights()
    {
        foreach (var row in diceRows)
        {
            if (row.diceA != null)
            {
                row.diceA.RemoveHighlight();
                row.diceA.SetClickable(false);
            }
            if (row.diceB != null)
            {
                row.diceB.RemoveHighlight();
                row.diceB.SetClickable(false);
            }
        }
    }
    
    #endregion
    
    #region Movement and Rotation
    
    /// <summary>
    /// Move a die to target position and straighten its rotation
    /// </summary>
    private IEnumerator MoveDiceToPosition(DB_DiceController dice, Vector3 targetPosition)
    {
        if (dice == null) yield break;
        
        Vector3 startPosition = dice.transform.position;
        Quaternion startRotation = dice.transform.rotation;
        
        // Keep the current rotation - dice already has correct face up from settle
        // We don't change rotation to preserve the face detection accuracy
        Quaternion targetRotation = startRotation;
        
        // Disable physics during movement
        Rigidbody rb = dice.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Set velocities to zero BEFORE making kinematic (can't set velocities on kinematic body)
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        float elapsed = 0f;
        
        while (elapsed < moveDuration)
        {
            if (dice == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            float curveT = moveCurve.Evaluate(t);
            
            // Lerp position only, keep rotation unchanged
            dice.transform.position = Vector3.Lerp(startPosition, targetPosition, curveT);
            
            yield return null;
        }
        
        // Ensure final position
        dice.transform.position = targetPosition;
        
        // Keep kinematic
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }
    
    /// <summary>
    /// Get the straight rotation for a specific dice face to be facing up
    /// Assumes standard dice layout where opposite faces add to 7
    /// </summary>
    private Quaternion GetStraightRotationForFace(int faceValue)
    {
        // Standard dice face rotations
        // This assumes a standard dice model setup
        switch (faceValue)
        {
            case 1: // Face 1 up
                return Quaternion.Euler(0, 0, 0);
            case 2: // Face 2 up
                return Quaternion.Euler(0, 0, 90);
            case 3: // Face 3 up
                return Quaternion.Euler(0, 0, 0); // Forward
            case 4: // Face 4 up
                return Quaternion.Euler(0, 0, 180);
            case 5: // Face 5 up
                return Quaternion.Euler(0, 0, -90);
            case 6: // Face 6 up
                return Quaternion.Euler(180, 0, 0);
            default:
                return Quaternion.identity;
        }
        
        // Note: You may need to adjust these rotations based on your dice model's actual face layout
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = gizmoColor;
        
        // Draw base position
        Gizmos.DrawWireSphere(transform.position, gizmoSize * 2);
        
        // Draw potential dice positions for visualization
        for (int row = 0; row < maxRowsToShow; row++)
        {
            Vector3 basePosition = transform.position;
            Vector3 rowOffset = new Vector3(0, heightOffset, row * rowSpacing);
            
            Vector3 posA = basePosition + rowOffset + new Vector3(-diceSpacing / 2, 0, 0);
            Vector3 posB = basePosition + rowOffset + new Vector3(diceSpacing / 2, 0, 0);
            
            // Draw position markers
            Gizmos.DrawWireSphere(posA, gizmoSize);
            Gizmos.DrawWireSphere(posB, gizmoSize);
            
            // Draw connection line between dice in row
            Gizmos.DrawLine(posA, posB);
            
            // Draw cube to represent dice
            Gizmos.DrawWireCube(posA, Vector3.one * gizmoSize * 2);
            Gizmos.DrawWireCube(posB, Vector3.one * gizmoSize * 2);
            
            // Reduce alpha for rows further away
            float alpha = 1f - (row / (float)maxRowsToShow);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, alpha);
        }
        
        // Draw actual dice positions if they exist
        Gizmos.color = Color.yellow;
        foreach (var row in diceRows)
        {
            if (row.diceA != null)
                Gizmos.DrawWireSphere(row.positionA, gizmoSize * 1.5f);
            if (row.diceB != null)
                Gizmos.DrawWireSphere(row.positionB, gizmoSize * 1.5f);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // Draw more detailed info when selected
        Gizmos.color = Color.green;
        
        // Draw forward direction
        Gizmos.DrawRay(transform.position, transform.forward * 0.2f);
        
        // Draw labels for each row in editor
        #if UNITY_EDITOR
        for (int row = 0; row < maxRowsToShow; row++)
        {
            Vector3 basePosition = transform.position;
            Vector3 rowOffset = new Vector3(0, heightOffset, row * rowSpacing);
            Vector3 labelPos = basePosition + rowOffset;
            
            UnityEditor.Handles.Label(labelPos, $"Row {row}");
        }
        #endif
    }
    
    #endregion
}
