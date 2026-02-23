using UnityEngine;
using System.Collections;

public class DB_DiceController : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Target Area")]
    [SerializeField] private DB_DiceTargetArea targetArea;

    [Header("Roll Physics")]
    [SerializeField] private float rollForce = 5f;
    [SerializeField] private float rollForceRandomness = 0.5f;
    [SerializeField] private float rollTorque = 10f;
    [SerializeField] private float maxSettleTime = 5f; // Failsafe timeout if dice falls through map
    [SerializeField] private float settleVelocityThreshold = 0.1f;
    [SerializeField] private float settleAngularVelocityThreshold = 0.1f;
    [SerializeField] private float settleDoubleCheckDelay = 0.5f; // Time to wait before confirming dice has stopped
    [SerializeField] private float moveLerpDuration = 0.5f;
    
    [Header("Flip Animation")]
    [SerializeField] private float flipJumpHeight = 0.3f;
    [SerializeField] private float flipDuration = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip rollSound;
    [SerializeField] private AudioClip[] collisionSounds;
    [SerializeField] private float minCollisionVelocity = 0.5f;
    [SerializeField] private float collisionCooldown = 0.1f;
    
    [Header("Highlight Settings")]
    [SerializeField] private GameObject destroyHighlightObject;
    [SerializeField] private GameObject swapHighlightObject;
    
    [Header("Particle Effects")]
    [SerializeField] private GameObject destroyParticleEffectPrefab;
    
    #endregion

    #region Private Fields
    
    private Vector3 idlePosition;
    private Rigidbody rb;
    private bool isRolling = false;
    private bool isLerping = false;
    private bool isFlipping = false;
    private int lastRollValue = 0;
    private System.Action<int> onRollComplete;
    private float lastCollisionTime = 0f;
    
    // Highlighting system
    private bool isHighlighted = false;
    private System.Action<DB_DiceController> onDiceClicked;
    private bool isClickable = false;
    
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Ensure highlight objects start disabled
        if (destroyHighlightObject != null)
            destroyHighlightObject.SetActive(false);
        if (swapHighlightObject != null)
            swapHighlightObject.SetActive(false);
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    private void OnMouseDown()
    {
        if (isClickable && onDiceClicked != null)
        {
            Debug.Log($"Dice clicked with value {lastRollValue}");
            onDiceClicked.Invoke(this);
        }
    }
    
    #endregion

    #region Public API

    public void RollDice()
    {
        if (isRolling) return;

        StartCoroutine(RollDiceCoroutine());
    }

    public void RollFromLaunchPosition(Vector3 launchPosition)
    {
        if (isRolling) return;

        StartCoroutine(RollFromLaunchPositionCoroutine(launchPosition));
    }

    public void Initialize(Vector3 newIdlePosition)
    {
        idlePosition = newIdlePosition;
        PlaceAtIdlePosition();
    }

    public void SetIdlePosition(Vector3 newIdlePosition)
    {
        idlePosition = newIdlePosition;
    }

    public void ReturnToIdlePosition()
    {
        if (!isRolling && !isLerping)
        {
            StartCoroutine(LerpToPositionInternal(idlePosition));
        }
    }

    public void SetTargetArea(DB_DiceTargetArea area)
    {
        targetArea = area;
    }

    public int GetLastRollValue() => lastRollValue;

    public bool IsRolling() => isRolling;
    
    public bool IsFlipping() => isFlipping;

    public void SetOnRollCompleteCallback(System.Action<int> callback)
    {
        onRollComplete = callback;
    }
    
    public void SetClickable(bool clickable, System.Action<DB_DiceController> clickCallback = null)
    {
        isClickable = clickable;
        onDiceClicked = clickCallback;
    }
    
    public void Highlight(Color color)
    {
        if (isHighlighted)
        {
            Debug.LogWarning($"Die {lastRollValue} is already highlighted");
            return;
        }
        
        isHighlighted = true;
        
        // Determine which highlight to show based on color
        // Red = destroy, Blue = swap
        if (color == Color.red && destroyHighlightObject != null)
        {
            destroyHighlightObject.SetActive(true);
            Debug.Log($"Highlighting die {lastRollValue} with destroy highlight (red)");
        }
        else if (color == Color.blue && swapHighlightObject != null)
        {
            swapHighlightObject.SetActive(true);
            Debug.Log($"Highlighting die {lastRollValue} with swap highlight (blue)");
        }
        else
        {
            Debug.LogWarning($"No highlight object set for color {color} on die {lastRollValue}");
        }
    }
    
    public void RemoveHighlight()
    {
        if (!isHighlighted) return;
        
        isHighlighted = false;
        
        // Turn off all highlight objects
        if (destroyHighlightObject != null)
            destroyHighlightObject.SetActive(false);
        if (swapHighlightObject != null)
            swapHighlightObject.SetActive(false);
        
        Debug.Log($"Removed highlight from die {lastRollValue}");
    }
    
    /// <summary>
    /// Flip the dice to show its opposite face with a jump and 180-degree rotation animation.
    /// </summary>
    public void FlipToOppositeFace(int newFaceValue)
    {
        if (isFlipping) return;
        StartCoroutine(FlipDiceCoroutine(newFaceValue));
    }
    
    /// <summary>
    /// Destroys the dice with a particle effect if one is assigned.
    /// </summary>
    public void DestroyWithEffect()
    {
        if (destroyParticleEffectPrefab != null)
        {
            GameObject effect = Instantiate(destroyParticleEffectPrefab, transform.position, Quaternion.identity);
            // Automatically destroy the particle effect after its lifetime
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                // Fallback: destroy after 2 seconds if no particle system found
                Destroy(effect, 2f);
            }
        }
        
        Destroy(gameObject);
    }
    
    #endregion

    #region Roll Coroutines

    private IEnumerator RollFromLaunchPositionCoroutine(Vector3 launchPosition)
    {
        isRolling = true;

        // Lerp to launch position
        yield return StartCoroutine(LerpToPositionInternal(launchPosition));

        // Small delay at launch position
        yield return new WaitForSeconds(0.2f);

        // Execute the roll from the launch position
        yield return StartCoroutine(ExecuteRoll(launchPosition));

        isRolling = false;
    }

    private IEnumerator RollDiceCoroutine()
    {
        isRolling = true;

        // Execute the roll from current position
        yield return StartCoroutine(ExecuteRoll(transform.position));

        isRolling = false;
    }
    
    private IEnumerator FlipDiceCoroutine(int targetFaceValue)
    {
        isFlipping = true;
        
        // Update face value immediately for instant score feedback
        lastRollValue = targetFaceValue;
        Debug.Log($"Dice flipping to: {targetFaceValue}");
        
        // Make dice kinematic for controlled animation
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        
        // Calculate the rotation needed to show the target face value
        Quaternion targetRotation = GetRotationForFaceValue(targetFaceValue);
        
        float elapsed = 0f;
        
        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flipDuration;
            
            // Smooth interpolation
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            // Jump animation (parabolic arc)
            float jumpProgress = Mathf.Sin(t * Mathf.PI);
            Vector3 jumpOffset = Vector3.up * (jumpProgress * flipJumpHeight);
            
            // Apply position and rotation
            transform.position = startPosition + jumpOffset;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
            
            yield return null;
        }
        
        // Ensure final position and rotation
        transform.position = startPosition;
        transform.rotation = targetRotation;
        
        Debug.Log($"Dice flip animation completed: {targetFaceValue}");
        
        isFlipping = false;
    }
    
    /// <summary>
    /// Calculates the rotation needed to show the specified face value pointing up.
    /// Uses the current rotation as a base and adjusts it to align the correct local axis with world up.
    /// </summary>
    private Quaternion GetRotationForFaceValue(int faceValue)
    {
        // Determine which local axis should point up for this face value
        // Face 2: Local Y+ (up)
        // Face 5: Local Y- (down)
        // Face 1: Local X+ (right)
        // Face 6: Local X- (left)
        // Face 3: Local Z+ (forward)
        // Face 4: Local Z- (back)
        
        Vector3 localAxisToPointUp;
        
        switch (faceValue)
        {
            case 2:
                localAxisToPointUp = Vector3.up;      // Local Y+
                break;
            case 5:
                localAxisToPointUp = Vector3.down;    // Local Y-
                break;
            case 1:
                localAxisToPointUp = Vector3.right;   // Local X+
                break;
            case 6:
                localAxisToPointUp = Vector3.left;    // Local X-
                break;
            case 3:
                localAxisToPointUp = Vector3.forward; // Local Z+
                break;
            case 4:
                localAxisToPointUp = Vector3.back;    // Local Z-
                break;
            default:
                Debug.LogWarning($"[DB_DiceController] Invalid face value {faceValue}, defaulting to face 2");
                localAxisToPointUp = Vector3.up;
                break;
        }
        
        // Create a rotation that aligns the local axis with world up
        // We want: rotation * localAxisToPointUp = Vector3.up
        Quaternion targetRotation = Quaternion.FromToRotation(localAxisToPointUp, Vector3.up);
        
        // Add random rotation around the Y axis to keep it interesting while maintaining the face up
        float randomYRotation = Random.Range(0f, 360f);
        targetRotation = Quaternion.Euler(0, randomYRotation, 0) * targetRotation;
        
        return targetRotation;
    }
    
    #endregion

    #region Core Roll Logic

    private IEnumerator ExecuteRoll(Vector3 launchPosition)
    {
        // Set random rotation
        transform.rotation = Random.rotation;

        // Apply physics forces from launch position
        ApplyRollForces(launchPosition);

        // Wait for dice to settle or timeout (failsafe)
        float elapsedTime = 0f;
        bool settled = false;
        
        while (elapsedTime < maxSettleTime && !settled)
        {
            // Check if dice has settled (both linear and angular velocity below threshold)
            if (rb != null && 
                rb.linearVelocity.magnitude < settleVelocityThreshold && 
                rb.angularVelocity.magnitude < settleAngularVelocityThreshold)
            {
                // Wait for double-check delay
                yield return new WaitForSeconds(settleDoubleCheckDelay);
                elapsedTime += settleDoubleCheckDelay;
                
                // Double-check that dice is still settled
                if (rb != null && 
                    rb.linearVelocity.magnitude < settleVelocityThreshold && 
                    rb.angularVelocity.magnitude < settleAngularVelocityThreshold)
                {
                    Debug.Log($"Dice settled after {elapsedTime:F2} seconds");
                    settled = true;
                }
                else
                {
                    Debug.Log("Dice started moving again during double-check, continuing to monitor...");
                }
            }

            if (!settled)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        if (elapsedTime >= maxSettleTime && !settled)
        {
            Debug.LogWarning($"Dice settle timeout reached ({maxSettleTime}s) - using failsafe");
        }

        // Determine final value
        int faceValue = GetDiceFaceValue();
        lastRollValue = faceValue;
        //Debug.Log($"Dice rolled: {faceValue}");

        // DON'T move back to idle position - dice will be moved to scoring position by DiceManager
        // The dice should stay in its settled position to preserve the correct face orientation
        
        // Notify completion
        OnDiceRollComplete(faceValue);
        onRollComplete?.Invoke(faceValue);
    }

    private void ApplyRollForces(Vector3 launchPosition)
    {
        if (rb == null) return;

        // Reset velocities and enable physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.WakeUp();

        // Play roll sound
        PlayRollSound();

        // Calculate launch velocity to pass through target area
        Vector3 launchVelocity = CalculateLaunchVelocity(launchPosition);
        
        // Apply randomness to the velocity
        Vector3 randomSpread = new Vector3(
            Random.Range(-rollForceRandomness, rollForceRandomness),
            Random.Range(-rollForceRandomness, rollForceRandomness),
            Random.Range(-rollForceRandomness, rollForceRandomness)
        );
        
        launchVelocity += randomSpread;
        
        Vector3 randomTorque = new Vector3(
            Random.Range(-rollTorque, rollTorque),
            Random.Range(-rollTorque, rollTorque),
            Random.Range(-rollTorque, rollTorque)
        );

        rb.linearVelocity = launchVelocity;
        rb.AddTorque(randomTorque, ForceMode.Impulse);
    }

    private Vector3 CalculateLaunchVelocity(Vector3 launchPosition)
    {
        // Get target point (random if target area exists, otherwise world zero)
        Vector3 targetPoint;
        if (targetArea != null)
        {
            targetPoint = targetArea.GetRandomPointInArea();
        }
        else
        {
            targetPoint = Vector3.zero;
        }

        // Calculate displacement
        Vector3 displacement = targetPoint - launchPosition;
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0f, displacement.z);
        float horizontalDistance = horizontalDisplacement.magnitude;
        float verticalDisplacement = displacement.y;

        // Get gravity magnitude
        float gravity = Mathf.Abs(Physics.gravity.y);
        
        // Use a low launch angle for direct trajectory
        float launchAngle = 20f * Mathf.Deg2Rad;
        
        // Calculate required speed to reach target at this angle
        // Using projectile motion equations:
        // horizontalDistance = (v^2 * sin(2*angle)) / g
        // Solving for v: v = sqrt((horizontalDistance * g) / sin(2*angle))
        float sin2Angle = Mathf.Sin(2f * launchAngle);
        
        // Adjust for vertical displacement
        // Using: y = x*tan(angle) - (g*x^2)/(2*v^2*cos^2(angle))
        // Rearranged to solve for v considering vertical offset
        float tanAngle = Mathf.Tan(launchAngle);
        float cosAngle = Mathf.Cos(launchAngle);
        float cos2Angle = cosAngle * cosAngle;
        
        // Calculate required velocity considering both horizontal distance and vertical offset
        float numerator = gravity * horizontalDistance * horizontalDistance;
        float denominator = 2f * cos2Angle * (horizontalDistance * tanAngle - verticalDisplacement);
        
        float speed;
        if (denominator > 0.001f)
        {
            speed = Mathf.Sqrt(numerator / denominator);
        }
        else
        {
            // Fallback calculation if target is too close or behind
            speed = Mathf.Sqrt((horizontalDistance * gravity) / Mathf.Max(sin2Angle, 0.1f));
        }
        
        // Scale by rollForce parameter (treat rollForce as a multiplier)
        speed *= rollForce;
        
        // Calculate velocity vector
        Vector3 horizontalDirection = horizontalDisplacement.normalized;
        float horizontalSpeed = speed * cosAngle;
        float verticalSpeed = speed * Mathf.Sin(launchAngle);
        
        return horizontalDirection * horizontalSpeed + Vector3.up * verticalSpeed;
    }
    
    #endregion

    #region Movement
    private void PlaceAtIdlePosition()
    {
        transform.position = idlePosition;
        transform.rotation = Quaternion.identity;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private IEnumerator LerpToPositionInternal(Vector3 targetPosition)
    {
        isLerping = true;

        if (rb != null)
        {
            // Must set kinematic first before clearing velocities
            rb.isKinematic = true;
        }

        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < moveLerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveLerpDuration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        
        // Check if we're returning to idle position - if so, stay kinematic
        if (rb != null)
        {
            if (Vector3.Distance(targetPosition, idlePosition) < 0.01f)
            {
                // At idle position - stay kinematic for score calculation
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            else
            {
                // At launch position - prepare for physics roll
                rb.isKinematic = false;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        
        isLerping = false;
    }
    
    #endregion

    #region Face Value Detection

    private int GetDiceFaceValue()
    {
        // Standard dice orientation: Face 2 points up (local Y+) when dice is at identity rotation
        // Face layout:
        // - Local Y+ (up) = face 1
        // - Local Y- (down) = face 6
        // - Local X+ (right) = face 4
        // - Local X- (left) = face 3
        // - Local Z+ (forward) = face 2
        // - Local Z- (back) = face 5
        
        // Get the six local axes in world space
        Vector3 localUp = transform.up;        // Local Y+
        Vector3 localDown = -transform.up;      // Local Y-
        Vector3 localRight = transform.right;   // Local X+
        Vector3 localLeft = -transform.right;   // Local X-
        Vector3 localForward = transform.forward; // Local Z+
        Vector3 localBack = -transform.forward;   // Local Z-
        
        // World up direction
        Vector3 worldUp = Vector3.up;
        
        // Calculate dot products to find which local axis is most aligned with world up
        float dotUp = Vector3.Dot(localUp, worldUp);
        float dotDown = Vector3.Dot(localDown, worldUp);
        float dotRight = Vector3.Dot(localRight, worldUp);
        float dotLeft = Vector3.Dot(localLeft, worldUp);
        float dotForward = Vector3.Dot(localForward, worldUp);
        float dotBack = Vector3.Dot(localBack, worldUp);
        
        // Find the maximum dot product
        float maxDot = Mathf.Max(dotUp, dotDown, dotRight, dotLeft, dotForward, dotBack);
        
        // Return the corresponding face value
        if (maxDot == dotUp) return 1;
        if (maxDot == dotDown) return 6;
        if (maxDot == dotRight) return 4;
        if (maxDot == dotLeft) return 3;
        if (maxDot == dotForward) return 2;
        if (maxDot == dotBack) return 5;
        
        // Fallback (should never reach here)
        Debug.LogWarning("[DB_DiceController] Could not determine face value from rotation.");
        return 1;
    }

    private void OnDiceRollComplete(int faceValue)
    {
        // Override or extend for custom behavior
    }
    
    #endregion

    #region Audio

    private void PlayRollSound()
    {
        if (audioSource != null && rollSound != null)
        {
            audioSource.PlayOneShot(rollSound);
        }
    }

    private void PlayCollisionSound()
    {
        if (audioSource != null && collisionSounds != null && collisionSounds.Length > 0)
        {
            AudioClip randomClip = collisionSounds[Random.Range(0, collisionSounds.Length)];
            audioSource.PlayOneShot(randomClip);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only play collision sounds when actively rolling
        if (!isRolling) return;

        // Check if enough time has passed since last collision sound
        if (Time.time - lastCollisionTime < collisionCooldown) return;

        // Check if collision velocity is strong enough
        float collisionVelocity = collision.relativeVelocity.magnitude;
        if (collisionVelocity < minCollisionVelocity) return;

        // Play collision sound and update time
        PlayCollisionSound();
        lastCollisionTime = Time.time;
    }
    
    #endregion

    #region Debug Visualization

    private void OnDrawGizmosSelected()
    {
        // Visualize idle position
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(idlePosition, 0.01f);
        
        // Visualize launch direction toward target area or board center
        if (targetArea != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 targetCenter = targetArea.GetCenterPoint();
            Gizmos.DrawLine(idlePosition, targetCenter);
            Gizmos.DrawSphere(targetCenter, 0.01f);
        }
        else
        {
            Gizmos.color = Color.blue;
            Vector3 directionToCenter = (Vector3.zero - idlePosition).normalized;
            Gizmos.DrawLine(idlePosition, idlePosition + directionToCenter * rollForce);
        }
    }
    
    #endregion
}
