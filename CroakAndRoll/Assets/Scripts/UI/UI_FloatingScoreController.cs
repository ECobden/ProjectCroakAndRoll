using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;

public class UI_FloatingScoreController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private RectTransform scoreVisualElement;

    [Header("Animation Settings")]
    [SerializeField] private float scorePunchScale = 1.3f;
    [SerializeField] private float scorePunchDuration = 0.3f;
    [SerializeField] private float scoreTransferDelay = 0.5f;
    [SerializeField] private float scoreTransferSpeed = 0.05f;
    
    [Header("Visual Element Animation")]
    [SerializeField] private float visualElementTargetScale = 0.66f;
    [SerializeField] private float visualElementScaleDuration = 0.3f;
    [SerializeField] private float visualElementRotation = 45f;
    [SerializeField] private float visualElementPunchScale = 0.2f;
    [SerializeField] private float visualElementTimeOffset = 0f;

    #endregion

    #region Private Fields

    private Coroutine scoreTransferCoroutine;
    private int currentTurnTotal = 0;

    #endregion

    #region Public API

    public void ClearScore()
    {
        if (scoreText != null)
            scoreText.text = "";
        currentTurnTotal = 0;
        
        // Stop any ongoing score transfer animation
        if (scoreTransferCoroutine != null)
        {
            StopCoroutine(scoreTransferCoroutine);
            scoreTransferCoroutine = null;
        }
        
        // Reset visual element
        if (scoreVisualElement != null)
        {
            scoreVisualElement.DOKill();
            scoreVisualElement.localScale = Vector3.zero;
            scoreVisualElement.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Calculates the total duration of the score transfer animation for a given roll value.
    /// </summary>
    public float GetScoreTransferDuration(int rollValue)
    {
        return scorePunchDuration + scoreTransferDelay + (rollValue * scoreTransferSpeed) + visualElementScaleDuration;
    }

    /// <summary>
    /// Returns true if a score transfer animation is currently playing.
    /// </summary>
    public bool IsAnimating()
    {
        return scoreTransferCoroutine != null;
    }

    public void UpdateScore(int targetScore)
    {
        // Stop any existing animation
        if (scoreTransferCoroutine != null)
        {
            StopCoroutine(scoreTransferCoroutine);
        }

        // Start count-up animation
        scoreTransferCoroutine = StartCoroutine(CountUpAnimation(targetScore));
    }

    #endregion

    #region Animation

    private IEnumerator CountUpAnimation(int targetScore)
    {
        // Show the roll value with punch animation
        if (scoreText != null)
        {
            scoreText.text = targetScore.ToString();
            scoreText.transform.DOKill();
            scoreText.transform.localScale = Vector3.one;
            scoreText.transform.DOPunchScale(Vector3.one * (scorePunchScale - 1f), scorePunchDuration, 5, 0.5f);
        }
        
        // Animate visual element
        if (scoreVisualElement != null)
        {
            scoreVisualElement.DOKill();
            scoreVisualElement.localScale = Vector3.zero;
            scoreVisualElement.DOScale(visualElementTargetScale, visualElementScaleDuration)
                .SetEase(Ease.OutBack)
                .SetDelay(visualElementTimeOffset);
            scoreVisualElement.DOPunchScale(Vector3.one * visualElementPunchScale, scorePunchDuration, 5, 0.5f)
                .SetDelay(visualElementScaleDuration + visualElementTimeOffset);
            scoreVisualElement.localRotation = Quaternion.identity;
            scoreVisualElement.DOLocalRotate(new Vector3(0, 0, visualElementRotation), visualElementScaleDuration + scorePunchDuration)
                .SetEase(Ease.OutCubic)
                .SetDelay(visualElementTimeOffset);
        }

        // Wait for punch animation
        yield return new WaitForSeconds(scorePunchDuration + scoreTransferDelay);
        
        // Clear score text
        if (scoreText != null)
        {
            scoreText.text = "";
        }
        
        // Scale down visual element
        if (scoreVisualElement != null)
        {
            scoreVisualElement.DOScale(0f, visualElementScaleDuration)
                .SetEase(Ease.InBack);
        }

        scoreTransferCoroutine = null;
    }

    #endregion
}
