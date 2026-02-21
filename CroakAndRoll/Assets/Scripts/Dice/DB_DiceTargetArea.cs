using UnityEngine;

public class DB_DiceTargetArea : MonoBehaviour
{
    [Header("Target Area Dimensions")]
    [SerializeField] private float width = 2f;
    [SerializeField] private float height = 2f;

    [Header("Visualization")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f);

    private Vector3 lastSelectedPoint;
    private bool hasSelectedPoint = false;

    /// <summary>
    /// Gets a random point within the target area in world space
    /// </summary>
    public Vector3 GetRandomPointInArea()
    {
        // Generate random offset within the plane bounds
        float randomX = Random.Range(-width * 0.5f, width * 0.5f);
        float randomY = Random.Range(-height * 0.5f, height * 0.5f);

        // Calculate local position on the plane
        Vector3 localPoint = new Vector3(randomX, randomY, 0f);

        // Convert to world space using the transform
        lastSelectedPoint = transform.TransformPoint(localPoint);
        hasSelectedPoint = true;
        
        return lastSelectedPoint;
    }

    /// <summary>
    /// Gets the center point of the target area in world space
    /// </summary>
    public Vector3 GetCenterPoint()
    {
        return transform.position;
    }

    private void OnDrawGizmos()
    {
        // Draw the target area plane
        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;

        // Draw filled plane
        Vector3 center = Vector3.zero;
        Vector3 size = new Vector3(width, height, 0.01f);
        Gizmos.DrawCube(center, size);

        // Draw outline
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(center, size);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw corner markers when selected
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;

        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;

        Vector3[] corners = new Vector3[]
        {
            new Vector3(-halfWidth, -halfHeight, 0f),
            new Vector3(halfWidth, -halfHeight, 0f),
            new Vector3(halfWidth, halfHeight, 0f),
            new Vector3(-halfWidth, halfHeight, 0f)
        };

        foreach (Vector3 corner in corners)
        {
            Gizmos.DrawSphere(corner, 0.05f);
        }

        // Draw center cross
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-0.1f, 0f, 0f), new Vector3(0.1f, 0f, 0f));
        Gizmos.DrawLine(new Vector3(0f, -0.1f, 0f), new Vector3(0f, 0.1f, 0f));
        
        // Reset gizmo matrix for world space drawing
        Gizmos.matrix = Matrix4x4.identity;
        
        // Draw the last selected random point
        if (hasSelectedPoint)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastSelectedPoint, 0.15f);
            Gizmos.DrawLine(transform.position, lastSelectedPoint);
        }
    }
}
