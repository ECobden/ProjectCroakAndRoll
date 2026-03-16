using UnityEngine;

public class UI_MainMenuDice : MonoBehaviour
{
    [Header("Dice Settings")]
    [SerializeField] private Transform diceTransform;
    
    [Header("Animation Settings")]
    [SerializeField] private float jumpHeight = 0.5f;
    [SerializeField] private float jumpDuration = 0.4f;
    [SerializeField] private float rotateDuration = 0.3f;
    [SerializeField] private AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve rotateCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Idle Animation")]
    [SerializeField] private bool enableIdleRotation = true;
    [SerializeField] private float idleRotationSpeed = 20f;
    [SerializeField] private Vector3 idleRotationAxis = new Vector3(0, 1, 0);
    
    [Header("Face Rotations")]
    [Tooltip("Rotation to show face 1 (Play button)")]
    [SerializeField] private Vector3 face1Rotation = new Vector3(0, 0, 0);
    
    [Tooltip("Rotation to show face 2 (Options button)")]
    [SerializeField] private Vector3 face2Rotation = new Vector3(0, 90, 0);
    
    [Tooltip("Rotation to show face 3 (Exit button)")]
    [SerializeField] private Vector3 face3Rotation = new Vector3(0, 180, 0);
    
    [Tooltip("Rotation to show face 4")]
    [SerializeField] private Vector3 face4Rotation = new Vector3(90, 0, 0);
    
    [Tooltip("Rotation to show face 5")]
    [SerializeField] private Vector3 face5Rotation = new Vector3(-90, 0, 0);
    
    [Tooltip("Rotation to show face 6")]
    [SerializeField] private Vector3 face6Rotation = new Vector3(0, 0, 90);
    
    private Vector3 originalPosition;
    private Coroutine animationCoroutine;
    private bool isAnimating;
    private int lastShownFace = 1;
    
    private void Start()
    {
        if (diceTransform == null)
        {
            diceTransform = transform;
        }
        
        originalPosition = diceTransform.localPosition;
        
        // Show default face (face 1) on start
        diceTransform.localRotation = Quaternion.Euler(face1Rotation);
        lastShownFace = 1;
        
    }

    private void Update()
    {
        if (diceTransform == null || isAnimating || !enableIdleRotation)
            return;

        diceTransform.Rotate(idleRotationAxis.normalized * idleRotationSpeed * Time.deltaTime, Space.Self);
    }
    
    private void OnDestroy()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
    }
    
    #region Public Methods
    
    /// <summary>
    /// Animate dice to show a specific face (1-6)
    /// </summary>
    public void ShowFace(int faceNumber)
    {
        if (isAnimating)
            return;

        lastShownFace = Mathf.Clamp(faceNumber, 1, 6);
        Vector3 targetRotation = GetFaceRotation(faceNumber);
        AnimateDice(targetRotation);
    }

    /// <summary>
    /// Animate dice to show a random face (1-6).
    /// </summary>
    public void ShowRandomFace()
    {
        int randomFace = Random.Range(1, 7);
        if (randomFace == lastShownFace)
            randomFace = randomFace == 6 ? 1 : randomFace + 1;

        ShowFace(randomFace);
    }
    
    /// <summary>
    /// Animate dice when Play button is highlighted
    /// </summary>
    public void OnPlayButtonHighlight()
    {
        ShowFace(1);
    }
    
    /// <summary>
    /// Animate dice when Options button is highlighted
    /// </summary>
    public void OnOptionsButtonHighlight()
    {
        ShowFace(2);
    }
    
    /// <summary>
    /// Animate dice when Exit button is highlighted
    /// </summary>
    public void OnExitButtonHighlight()
    {
        ShowFace(3);
    }
    
    /// <summary>
    /// Animate dice for a custom button (face 4-6)
    /// </summary>
    public void OnCustomButtonHighlight(int faceNumber)
    {
        ShowFace(faceNumber);
    }
    
    #endregion
    
    #region Animation Methods
    
    private void AnimateDice(Vector3 targetRotation)
    {
        if (diceTransform == null)
            return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateDiceCoroutine(Quaternion.Euler(targetRotation)));
    }
    
    private System.Collections.IEnumerator AnimateDiceCoroutine(Quaternion targetRotation)
    {
        isAnimating = true;

        float jumpTime = Mathf.Max(0.01f, jumpDuration);
        float rotationTime = Mathf.Max(0.01f, rotateDuration);
        float totalTime = Mathf.Max(jumpTime, rotationTime);

        Quaternion startRotation = diceTransform.localRotation;

        float elapsed = 0f;
        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;

            float jumpT = Mathf.Clamp01(elapsed / jumpTime);
            float jumpProgress = jumpCurve != null ? jumpCurve.Evaluate(jumpT) : jumpT;
            float jumpY = Mathf.Sin(jumpProgress * Mathf.PI) * jumpHeight;
            diceTransform.localPosition = originalPosition + Vector3.up * jumpY;

            float rotateT = Mathf.Clamp01(elapsed / rotationTime);
            float rotateProgress = rotateCurve != null ? rotateCurve.Evaluate(rotateT) : rotateT;
            diceTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, rotateProgress);

            yield return null;
        }

        diceTransform.localPosition = originalPosition;
        diceTransform.localRotation = targetRotation;
        isAnimating = false;
        animationCoroutine = null;
    }
    
    private Vector3 GetFaceRotation(int faceNumber)
    {
        return faceNumber switch
        {
            1 => face1Rotation,
            2 => face2Rotation,
            3 => face3Rotation,
            4 => face4Rotation,
            5 => face5Rotation,
            6 => face6Rotation,
            _ => face1Rotation
        };
    }
    
    #endregion
}
