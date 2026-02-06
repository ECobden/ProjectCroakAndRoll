using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class UI_ButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverUpAmount = 30f;
    
    [Header("Click Settings")]
    [SerializeField] private float clickDownAmount = 20f;
    
    [Header("Animation Settings")]
    [SerializeField] private float lerpSpeed = 10f;
    
    [Header("Activation Settings")]
    [SerializeField] private float offscreenOffset = -200f;
    [SerializeField] private float activationDuration = 0.5f;
    [SerializeField] private AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI buttonText;
    
    [Header("Events")]
    public UnityEngine.Events.UnityEvent onButtonClick;
    
    private System.Action customAction;
    
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private Coroutine moveCoroutine;
    private Coroutine activationCoroutine;
    private bool isHovering = false;
    private bool isPressed = false;
    private bool isDisabled = false;
    private bool isActive = false;
    private bool isAnimating = false;

    private void Awake()
    {
        originalPosition = transform.localPosition;
        targetPosition = originalPosition;
    }

    private void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * lerpSpeed);
    }
    
    // Called when the pointer enters the button area
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isAnimating || !isActive) return;
        
        Debug.Log($"Pointer entered: {gameObject.name}");
        isHovering = true;
        if (!isPressed && !isDisabled)
        {
            targetPosition = originalPosition + new Vector3(0, hoverUpAmount, 0);
            PlaySound(hoverSound);
        }
    }

    // Called when the pointer exits the button area
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"Pointer exited: {gameObject.name}");
        isHovering = false;
        
        if (isDisabled)
        {
            // Re-enable the button and reset position
            isDisabled = false;
            targetPosition = originalPosition;
        }
        else if (!isPressed)
        {
            targetPosition = originalPosition;
        }
    }

    // Called when the button is pressed down
    public void OnPointerDown(PointerEventData eventData)
    {
        if (isDisabled || isAnimating || !isActive) return;
        
        Debug.Log($"Button pressed: {gameObject.name}");
        isPressed = true;
        targetPosition = originalPosition + new Vector3(0, -clickDownAmount, 0);
    }

    // Called when the button is released
    public void OnPointerUp(PointerEventData eventData)
    {
        if (isDisabled || isAnimating || !isActive) return;
        
        Debug.Log($"Button released: {gameObject.name}");
        isPressed = false;
        isDisabled = true;
        
        targetPosition = originalPosition;
        PlaySound(clickSound);
        
        // Invoke custom action if set, otherwise use UnityEvent
        if (customAction != null)
        {
            customAction.Invoke();
        }
        else
        {
            onButtonClick?.Invoke();
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    public void ActivateButton()
    {
        if (isActive || isAnimating) return;
        
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
        }
        
        activationCoroutine = StartCoroutine(AnimateActivation(true));
    }
    
    public void DeactivateButton()
    {
        if (!isActive || isAnimating) return;
        
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
        }
        
        activationCoroutine = StartCoroutine(AnimateActivation(false));
    }
    
    private IEnumerator AnimateActivation(bool activate)
    {
        isAnimating = true;
        isDisabled = false;
        isPressed = false;
        isHovering = false;
        
        Vector3 startPos = activate ? originalPosition + new Vector3(0, offscreenOffset, 0) : originalPosition;
        Vector3 endPos = activate ? originalPosition : originalPosition + new Vector3(0, offscreenOffset, 0);
        
        float elapsed = 0f;
        
        while (elapsed < activationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / activationDuration;
            float curveValue = bounceCurve.Evaluate(t);
            
            transform.localPosition = Vector3.Lerp(startPos, endPos, curveValue);
            targetPosition = transform.localPosition;
            
            yield return null;
        }
        
        transform.localPosition = endPos;
        targetPosition = endPos;
        
        isActive = activate;
        isAnimating = false;
    }
    
    public void SetButtonAction(System.Action action)
    {
        customAction = action;
    }
    
    public void ClearButtonAction()
    {
        customAction = null;
    }
    
    public void SetButtonText(string text)
    {
        if (buttonText != null)
        {
            buttonText.text = text;
        }
    }
}
