using UnityEngine;
using DG.Tweening;

public class UI_MainMenuDice : MonoBehaviour
{
    [Header("Dice Settings")]
    [SerializeField] private Transform diceTransform;
    
    [Header("Animation Settings")]
    [SerializeField] private float jumpHeight = 0.5f;
    [SerializeField] private float jumpDuration = 0.4f;
    [SerializeField] private float rotateDuration = 0.3f;
    [SerializeField] private Ease jumpEase = Ease.OutQuad;
    [SerializeField] private Ease rotateEase = Ease.OutBack;
    
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
    private Sequence currentAnimation;
    private bool isIdleRotating = false;
    
    private void Start()
    {
        if (diceTransform == null)
        {
            diceTransform = transform;
        }
        
        originalPosition = diceTransform.localPosition;
        
        // Show default face (face 1) on start
        diceTransform.localRotation = Quaternion.Euler(face1Rotation);
        
        // Start idle rotation if enabled
        if (enableIdleRotation)
        {
            StartIdleRotation();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up tweens
        currentAnimation?.Kill();
        DOTween.Kill(diceTransform);
    }
    
    #region Public Methods
    
    /// <summary>
    /// Animate dice to show a specific face (1-6)
    /// </summary>
    public void ShowFace(int faceNumber)
    {
        Vector3 targetRotation = GetFaceRotation(faceNumber);
        AnimateDice(targetRotation);
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
        // Kill any existing animation
        currentAnimation?.Kill();
        StopIdleRotation();
        
        // Create jump and rotation sequence
        currentAnimation = DOTween.Sequence();
        
        // Jump up
        currentAnimation.Append(
            diceTransform.DOLocalMoveY(originalPosition.y + jumpHeight, jumpDuration * 0.5f)
                .SetEase(jumpEase)
        );
        
        // Rotate while at peak
        currentAnimation.Join(
            diceTransform.DOLocalRotate(targetRotation, rotateDuration)
                .SetEase(rotateEase)
        );
        
        // Fall back down
        currentAnimation.Append(
            diceTransform.DOLocalMoveY(originalPosition.y, jumpDuration * 0.5f)
                .SetEase(Ease.InQuad)
        );
        
        // Resume idle rotation when done
        if (enableIdleRotation)
        {
            currentAnimation.OnComplete(() => StartIdleRotation());
        }
    }
    
    private void StartIdleRotation()
    {
        if (isIdleRotating) return;
        
        isIdleRotating = true;
        diceTransform.DOLocalRotate(
            diceTransform.localRotation.eulerAngles + (idleRotationAxis * 360f),
            360f / idleRotationSpeed,
            RotateMode.LocalAxisAdd
        )
        .SetLoops(-1, LoopType.Incremental)
        .SetEase(Ease.Linear);
    }
    
    private void StopIdleRotation()
    {
        if (!isIdleRotating) return;
        
        isIdleRotating = false;
        DOTween.Kill(diceTransform);
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
