using UnityEngine;
using TMPro;
using DG.Tweening;

public class UI_StandValueController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private RectTransform uiElement;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float visibleXPosition = 0f;
    [SerializeField] private float hiddenXPosition = 300f;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    private void Start()
    {
        // If uiElement is not assigned, use this component's RectTransform
        if (uiElement == null)
            uiElement = GetComponent<RectTransform>();

        // If canvasGroup is not assigned, try to get it from this object
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Start hidden
        if (uiElement != null)
        {
            Vector2 pos = uiElement.anchoredPosition;
            pos.x = hiddenXPosition;
            uiElement.anchoredPosition = pos;
        }
        
        // Set alpha to 0 as safety
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void Show(string text)
    {
        // Update text before showing
        if (valueText != null)
            valueText.text = text;

        // Set alpha to 1
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        // Slide in from right with bounce
        if (uiElement != null)
        {
            uiElement.DOKill();
            uiElement.DOAnchorPosX(visibleXPosition, animationDuration)
                .SetEase(showEase);
        }
    }

    public void Hide()
    {
        // Slide out to right with bounce
        if (uiElement != null)
        {
            uiElement.DOKill();
            uiElement.DOAnchorPosX(hiddenXPosition, animationDuration)
                .SetEase(hideEase)
                .OnComplete(() => {
                    // Set alpha to 0 as safety when hidden
                    if (canvasGroup != null)
                        canvasGroup.alpha = 0f;
                });
        }
    }
}
