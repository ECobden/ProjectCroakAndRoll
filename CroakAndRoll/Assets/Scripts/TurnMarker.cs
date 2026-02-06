using UnityEngine;
using System.Collections;

public class TurnMarker : MonoBehaviour
{
    [Header("Position Settings")]
    [SerializeField] private Transform playerTurnPosition;
    [SerializeField] private Transform houseTurnPosition;
    
    [Header("Animation Settings")]
    [SerializeField] private float moveSpeed = 10f;
    
    private Coroutine moveCoroutine;
    
    public void SetPlayerTurnPosition()
    {
        if (playerTurnPosition != null)
        {
            MoveTo(playerTurnPosition.position);
        }
    }
    
    public void SetHouseTurnPosition()
    {
        if (houseTurnPosition != null)
        {
            MoveTo(houseTurnPosition.position);
        }
    }
    
    private void MoveTo(Vector3 targetPosition)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        
        moveCoroutine = StartCoroutine(MoveToPosition(targetPosition));
    }
    
    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
            yield return null;
        }
        
        transform.position = targetPosition;
        moveCoroutine = null;
    }
}
