using UnityEngine;

/// <summary>
/// Controls the shop machine's movement on and off screen with rotation effects.
/// Manages the reroll lever rotation animation.
/// </summary>
public class ShopMachineController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Transform offScreenPosition;
    [SerializeField] private Transform onScreenPosition;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotationAmount = new Vector3(0, 0, 5f);
    [SerializeField] private float rotationSpeed = 1f;

    [Header("Reroll Lever")]
    [SerializeField] private GameObject rerollLever;
    [SerializeField] private Vector3 leverRotationAxis = new Vector3(1, 0, 0);
    [SerializeField] private float leverRotationAngle = 45f;
    [SerializeField] private float leverRotationSpeed = 3f;

    private Vector3 targetPosition;
    private Quaternion initialRotation;
    private Quaternion leverInitialRotation;
    private bool isMoving = false;
    private float moveProgress = 0f;
    private bool isOnScreen = false;
    private RerollLeverController leverController;

    private void Start()
    {
        initialRotation = transform.rotation;
        
        if (rerollLever != null)
        {
            leverInitialRotation = rerollLever.transform.localRotation;
            
            // Set up lever click handler
            leverController = rerollLever.GetComponent<RerollLeverController>();
            if (leverController != null)
            {
                leverController.onLeverClicked.AddListener(RotateRerollLever);
            }
            else
            {
                Debug.LogWarning("RerollLeverController component not found on reroll lever GameObject. Add it to enable clicking.");
            }
        }

        // Start off screen
        if (offScreenPosition != null)
        {
            transform.position = offScreenPosition.position;
            transform.rotation = offScreenPosition.rotation;
        }
    }

    private void OnDestroy()
    {
        // Clean up listener
        if (leverController != null)
        {
            leverController.onLeverClicked.RemoveListener(RotateRerollLever);
        }
    }

    private void Update()
    {
        if (isMoving)
        {
            UpdateMovement();
        }
    }

    /// <summary>
    /// Move the shop machine onto the screen.
    /// </summary>
    public void ShowShop()
    {
        if (onScreenPosition != null && !isOnScreen)
        {
            targetPosition = onScreenPosition.position;
            isMoving = true;
            moveProgress = 0f;
            isOnScreen = true;
            Debug.Log("Shop machine moving on screen");
        }
    }

    /// <summary>
    /// Move the shop machine off the screen.
    /// </summary>
    public void HideShop()
    {
        if (offScreenPosition != null && isOnScreen)
        {
            targetPosition = offScreenPosition.position;
            isMoving = true;
            moveProgress = 0f;
            isOnScreen = false;
            Debug.Log("Shop machine moving off screen");
        }
    }

    /// <summary>
    /// Update the movement animation.
    /// </summary>
    private void UpdateMovement()
    {
        moveProgress += Time.deltaTime * moveSpeed;
        float curvedProgress = movementCurve.Evaluate(Mathf.Clamp01(moveProgress));

        // Move position
        Vector3 startPos = isOnScreen ? offScreenPosition.position : onScreenPosition.position;
        transform.position = Vector3.Lerp(startPos, targetPosition, curvedProgress);

        // Apply slight rotation during movement
        float rotationT = Mathf.Sin(curvedProgress * Mathf.PI);
        Quaternion targetRotation = isOnScreen ? onScreenPosition.rotation : offScreenPosition.rotation;
        Quaternion wobbleRotation = Quaternion.Euler(rotationAmount * rotationT * rotationSpeed);
        transform.rotation = Quaternion.Slerp(
            isOnScreen ? offScreenPosition.rotation : onScreenPosition.rotation,
            targetRotation,
            curvedProgress
        ) * wobbleRotation;

        // Check if movement is complete
        if (moveProgress >= 1f)
        {
            isMoving = false;
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
    }

    /// <summary>
    /// Rotate the reroll lever (call when player uses reroll).
    /// </summary>
    public void RotateRerollLever()
    {
        if (rerollLever != null)
        {
            StartCoroutine(AnimateRerollLever());
        }
    }

    /// <summary>
    /// Animate the reroll lever rotation.
    /// </summary>
    private System.Collections.IEnumerator AnimateRerollLever()
    {
        // Disable clicking during animation
        if (leverController != null)
        {
            leverController.SetClickable(false);
        }

        Quaternion startRotation = rerollLever.transform.localRotation;
        Quaternion targetRotation = leverInitialRotation * Quaternion.AngleAxis(leverRotationAngle, leverRotationAxis);
        
        float elapsed = 0f;
        float duration = 1f / leverRotationSpeed;

        // Rotate to target
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rerollLever.transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        elapsed = 0f;

        // Rotate back
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rerollLever.transform.localRotation = Quaternion.Slerp(targetRotation, leverInitialRotation, t);
            yield return null;
        }

        rerollLever.transform.localRotation = leverInitialRotation;

        // Re-enable clicking after animation
        if (leverController != null)
        {
            leverController.SetClickable(true);
        }
    }

    /// <summary>
    /// Check if the shop is currently on screen.
    /// </summary>
    public bool IsOnScreen()
    {
        return isOnScreen && !isMoving;
    }

    /// <summary>
    /// Check if the shop is currently moving.
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }
}
