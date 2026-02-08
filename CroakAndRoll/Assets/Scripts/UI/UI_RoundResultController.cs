using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;

public class UI_RoundResultController : MonoBehaviour
{
    #region Serialized Fields

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Event Messages")]
    [SerializeField] private string playerBustMessage = "PLAYER BUST!";
    [SerializeField] private string houseBustMessage = "HOUSE BUST!";
    [SerializeField] private string player21Message = "PERFECT 21!";
    [SerializeField] private string house21Message = "HOUSE HITS 21!";
    [SerializeField] private string houseWinsMessage = "HOUSE WINS!";
    [SerializeField] private string playerWinsMessage = "PLAYER WINS!";

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float moveUpDistance = 30f;
    [SerializeField] private Ease fadeInEase = Ease.OutCubic;
    [SerializeField] private Ease moveEase = Ease.OutCubic;
    [SerializeField] private Ease fadeOutEase = Ease.InCubic;

    #endregion

    #region Private Fields

    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Coroutine currentAnimation;

    #endregion

    #region Initialization

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            originalPosition = rectTransform.anchoredPosition;

        // Start invisible
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Show the player bust message.
    /// </summary>
    public void ShowPlayerBust()
    {
        ShowMessage(playerBustMessage);
    }

    /// <summary>
    /// Show the house bust message.
    /// </summary>
    public void ShowHouseBust()
    {
        ShowMessage(houseBustMessage);
    }

    /// <summary>
    /// Show the player hit 21 message.
    /// </summary>
    public void ShowPlayer21()
    {
        ShowMessage(player21Message);
    }

    /// <summary>
    /// Show the house hit 21 message.
    /// </summary>
    public void ShowHouse21()
    {
        ShowMessage(house21Message);
    }

    /// <summary>
    /// Show the house wins message.
    /// </summary>
    public void ShowHouseWins()
    {
        ShowMessage(houseWinsMessage);
    }

    /// <summary>
    /// Show the player wins message.
    /// </summary>
    public void ShowPlayerWins()
    {
        ShowMessage(playerWinsMessage);
    }

    /// <summary>
    /// Show a custom message.
    /// </summary>
    public void ShowMessage(string message)
    {
        // Stop any current animation
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        // Start new animation
        currentAnimation = StartCoroutine(AnimateMessage(message));
    }

    /// <summary>
    /// Immediately hide the message.
    /// </summary>
    public void Hide()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
        }

        if (rectTransform != null)
        {
            rectTransform.DOKill();
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    #endregion

    #region Animation

    private IEnumerator AnimateMessage(string message)
    {
        // Set the text
        if (resultText != null)
            resultText.text = message;

        // Reset position
        if (rectTransform != null)
            rectTransform.anchoredPosition = originalPosition;

        // Kill any existing tweens
        if (canvasGroup != null)
            canvasGroup.DOKill();
        if (rectTransform != null)
            rectTransform.DOKill();

        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeInDuration).SetEase(fadeInEase);
        }

        // Move up
        if (rectTransform != null)
        {
            Vector3 targetPosition = originalPosition + new Vector3(0, moveUpDistance, 0);
            rectTransform.DOAnchorPos(targetPosition, fadeInDuration + holdDuration).SetEase(moveEase);
        }

        // Wait for fade in and hold duration
        yield return new WaitForSeconds(fadeInDuration + holdDuration);

        // Fade out
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, fadeOutDuration).SetEase(fadeOutEase);
        }

        // Wait for fade out
        yield return new WaitForSeconds(fadeOutDuration);

        // Reset position
        if (rectTransform != null)
            rectTransform.anchoredPosition = originalPosition;

        currentAnimation = null;
    }

    #endregion
}
