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
    [SerializeField] private float settleTime = 3f;
    [SerializeField] private float moveLerpDuration = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip rollSound;
    [SerializeField] private AudioClip[] collisionSounds;
    [SerializeField] private float minCollisionVelocity = 0.5f;
    [SerializeField] private float collisionCooldown = 0.1f;

    [Header("Face Detection")]
    [SerializeField] private GameObject facePositionsPrefab;
    
    #endregion

    #region Private Fields
    
    private Vector3 idlePosition;
    private GameObject facePositionsInstance;
    private Rigidbody rb;
    private bool isRolling = false;
    private bool isLerping = false;
    private int lastRollValue = 0;
    private System.Action<int> onRollComplete;
    private float lastCollisionTime = 0f;
    
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        if (facePositionsPrefab != null && facePositionsInstance == null)
        {
            facePositionsInstance = Instantiate(facePositionsPrefab, transform);
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

    public void SetOnRollCompleteCallback(System.Action<int> callback)
    {
        onRollComplete = callback;
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
    
    #endregion

    #region Core Roll Logic

    private IEnumerator ExecuteRoll(Vector3 launchPosition)
    {
        // Set random rotation
        transform.rotation = Random.rotation;

        // Apply physics forces from launch position
        ApplyRollForces(launchPosition);

        // Wait for dice to settle
        yield return new WaitForSeconds(settleTime);

        // Determine final value
        int faceValue = GetDiceFaceValue();
        lastRollValue = faceValue;
        Debug.Log($"Dice rolled: {faceValue}");

        // Move back to idle position
        yield return StartCoroutine(LerpToPositionInternal(idlePosition));

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
        
        // Use a 45-degree launch angle for good arc trajectory
        float launchAngle = 45f * Mathf.Deg2Rad;
        
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
        if (facePositionsInstance == null)
        {
            Debug.LogError("[DB_DiceController] Face Positions instance is not set.");
            return 0;
        }

        Transform upwardFace = null;
        float highestDot = -1f;

        foreach (Transform child in facePositionsInstance.transform)
        {
            Vector3 directionToFace = (child.position - transform.position).normalized;
            float dotProduct = Vector3.Dot(directionToFace, Vector3.up);

            if (dotProduct > highestDot)
            {
                highestDot = dotProduct;
                upwardFace = child;
            }
        }

        if (upwardFace != null)
        {
            if (int.TryParse(upwardFace.name, out int faceValue))
            {
                return faceValue;
            }

            Debug.LogError("[DB_DiceController] Face name is not a number.");
        }
        else
        {
            Debug.LogError("[DB_DiceController] No face positions found.");
        }

        return 0;
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
        Gizmos.DrawSphere(idlePosition, 0.2f);
        
        // Visualize launch direction toward target area or board center
        if (targetArea != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 targetCenter = targetArea.GetCenterPoint();
            Gizmos.DrawLine(idlePosition, targetCenter);
            Gizmos.DrawSphere(targetCenter, 0.15f);
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
