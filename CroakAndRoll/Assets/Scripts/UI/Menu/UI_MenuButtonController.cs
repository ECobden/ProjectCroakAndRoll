using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class UI_MenuButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Animation Settings")]
    [SerializeField] private float highlightBounceX = 50f;
    [SerializeField] private float highlightDuration = 0.3f;
    [SerializeField] private Ease highlightEase = Ease.OutBack;
    [SerializeField] private float highlightSlideFromLeft = 20f;
    [SerializeField] private float highlightCooldown = 0.12f;
    
    [SerializeField] private float clickBounceX = 20f;
    [SerializeField] private float clickDuration = 0.15f;
    [SerializeField] private Ease clickEase = Ease.InOutQuad;
    
    [SerializeField] private float showDuration = 0.5f;
    [SerializeField] private float showDelay = 0f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private float showOffsetX = -300f;
    
    [SerializeField] private float hideDuration = 0.3f;
    [SerializeField] private Ease hideEase = Ease.InBack;
    
    [Header("Scale Animation")]
    [SerializeField] private bool enableScaleOnHighlight = true;
    [SerializeField] private float highlightScale = 1.1f;
    [SerializeField] private float scaleDuration = 0.2f;
    
    [Header("UI References")]
    [SerializeField] private RectTransform buttonRect;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Graphic clickTargetGraphic;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Dice Integration")]
    [SerializeField] private UI_MainMenuDice menuDice;
    [SerializeField] private bool useRandomDiceFace;
    [SerializeField] private int diceFaceNumber = 1;
    
    [Header("Events")]
    public UnityEvent onButtonClick;
    public UnityEvent onButtonHighlight;
    public UnityEvent onButtonUnhighlight;
    
    private Vector2 originalAnchoredPosition;
    private Vector3 originalScale;
    private Sequence currentAnimation;
    private bool isHighlighted = false;
    private bool isDisabled = false;
    private bool isVisible = false;
    private float nextHighlightAllowedTime;
    private bool isInitialized;
    
    public void Initialize()
    {
        if (isInitialized)
            return;

        // Setup references
        if (buttonRect == null)
            buttonRect = GetComponent<RectTransform>();

        if (clickTargetGraphic == null)
            clickTargetGraphic = GetComponent<Graphic>();
        
        // Store original values
        originalAnchoredPosition = buttonRect.anchoredPosition;
        originalScale = buttonRect.localScale;

        if (clickTargetGraphic != null)
            clickTargetGraphic.raycastTarget = true;

        buttonRect.anchoredPosition = originalAnchoredPosition + new Vector2(showOffsetX, 0);
        isInitialized = true;
    }
    
    private void OnDestroy()
    {
        // Clean up tweens
        currentAnimation?.Kill();
        DOTween.Kill(buttonRect);
        DOTween.Kill(transform);
    }
    
    #region Pointer Events
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDisabled || !isVisible || isHighlighted) return;

        if (Time.unscaledTime < nextHighlightAllowedTime)
            return;
        
        isHighlighted = true;
        nextHighlightAllowedTime = Time.unscaledTime + Mathf.Max(0f, highlightCooldown);
        AnimateHighlight(true);
        PlaySound(hoverSound);
        onButtonHighlight?.Invoke();
        
        // Trigger dice animation if connected
        if (menuDice != null)
        {
            if (useRandomDiceFace)
                menuDice.ShowRandomFace();
            else
                menuDice.ShowFace(diceFaceNumber);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDisabled || !isVisible) return;

        if (!isHighlighted)
            return;
        
        isHighlighted = false;
        AnimateHighlight(false);
        onButtonUnhighlight?.Invoke();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDisabled || !isVisible) return;
        
        AnimateClick();
        PlaySound(clickSound);
        onButtonClick?.Invoke();
    }
    
    #endregion
    
    #region Animation Methods
    
    /// <summary>
    /// Shows the button with a slide-in animation from the left
    /// </summary>
    public void Show()
    {
        if (isVisible) return;
        
        isVisible = true;
        isDisabled = false;
        currentAnimation?.Kill();

        currentAnimation = DOTween.Sequence();

        // Slide in from left with bounce
        currentAnimation.Join(
            buttonRect.DOAnchorPos(originalAnchoredPosition, showDuration)
                .SetEase(showEase)
                .SetDelay(showDelay)
        );
    }
    
    /// <summary>
    /// Shows the button with a custom delay
    /// </summary>
    public void Show(float delay)
    {
        showDelay = delay;
        Show();
    }
    
    /// <summary>
    /// Hides the button with a slide-out animation
    /// </summary>
    public void Hide()
    {
        if (!isVisible) return;
        
        isVisible = false;
        currentAnimation?.Kill();

        currentAnimation = DOTween.Sequence();

        // Slide out to left
        currentAnimation.Join(
            buttonRect.DOAnchorPos(originalAnchoredPosition + new Vector2(showOffsetX, 0), hideDuration)
                .SetEase(hideEase)
        );
        
        currentAnimation.OnComplete(() => 
        {
            isHighlighted = false;
        });
    }
    
    /// <summary>
    /// Animates the button when highlighted (hovered)
    /// </summary>
    private void AnimateHighlight(bool highlight)
    {
        currentAnimation?.Kill();
        
        currentAnimation = DOTween.Sequence();

        Vector2 targetPosition = highlight
            ? originalAnchoredPosition + new Vector2(highlightBounceX, 0)
            : originalAnchoredPosition;

        if (highlight)
        {
            // Add a quick left-to-right sweep so hover motion matches the menu intro feel.
            Vector2 sweepStart = originalAnchoredPosition + new Vector2(-Mathf.Abs(highlightSlideFromLeft), 0f);
            currentAnimation.Append(
                buttonRect.DOAnchorPos(sweepStart, highlightDuration * 0.25f)
                    .SetEase(Ease.OutQuad)
            );

            currentAnimation.Append(
                buttonRect.DOAnchorPos(targetPosition, highlightDuration * 0.75f)
                    .SetEase(highlightEase)
            );
        }
        else
        {
            currentAnimation.Append(
                buttonRect.DOAnchorPos(targetPosition, highlightDuration)
                    .SetEase(highlightEase)
            );
        }
        
        // Optional scale effect
        if (enableScaleOnHighlight)
        {
            Vector3 targetScale = highlight 
                ? originalScale * highlightScale 
                : originalScale;
            
            currentAnimation.Join(
                buttonRect.DOScale(targetScale, scaleDuration)
                    .SetEase(highlightEase)
            );
        }
    }
    
    /// <summary>
    /// Animates the button when clicked
    /// </summary>
    private void AnimateClick()
    {
        // Quick bounce in and out
        Sequence clickSequence = DOTween.Sequence();
        
        // Bounce in
        clickSequence.Append(
            buttonRect.DOAnchorPos(
                originalAnchoredPosition + new Vector2(isHighlighted ? highlightBounceX - clickBounceX : -clickBounceX, 0),
                clickDuration
            ).SetEase(clickEase)
        );
        
        // Bounce back to highlight or normal position
        Vector2 returnPosition = isHighlighted 
            ? originalAnchoredPosition + new Vector2(highlightBounceX, 0)
            : originalAnchoredPosition;
        
        clickSequence.Append(
            buttonRect.DOAnchorPos(returnPosition, clickDuration)
                .SetEase(Ease.OutQuad)
        );
    }
    
    #endregion
    
    #region Public Methods
    
    public void SetButtonText(string text)
    {
        if (buttonText != null)
        {
            buttonText.text = text;
        }
    }
    
    public void SetDiceFace(int faceNumber)
    {
        diceFaceNumber = Mathf.Clamp(faceNumber, 1, 6);
    }

    public void SetUseRandomDiceFace(bool useRandom)
    {
        useRandomDiceFace = useRandom;
    }
    
    public void SetMenuDiceReference(UI_MainMenuDice dice)
    {
        menuDice = dice;
    }
    
    public void Enable()
    {
        isDisabled = false;
    }
    
    public void Disable()
    {
        isDisabled = true;

        // Reset to normal state
        if (isHighlighted)
        {
            isHighlighted = false;
            AnimateHighlight(false);
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    #endregion
}
