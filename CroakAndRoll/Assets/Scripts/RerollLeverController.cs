using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles click detection for the reroll lever.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RerollLeverController : MonoBehaviour
{
    [Header("Events")]
    [Tooltip("Invoked when the lever is clicked")]
    public UnityEvent onLeverClicked;

    [Header("Settings")]
    [SerializeField] private bool isClickable = true;

    private void OnMouseDown()
    {
        if (isClickable)
        {
            //Debug.Log("Reroll lever clicked");
            onLeverClicked?.Invoke();
        }
    }

    /// <summary>
    /// Enable or disable clicking on the lever.
    /// </summary>
    public void SetClickable(bool clickable)
    {
        isClickable = clickable;
    }
}
