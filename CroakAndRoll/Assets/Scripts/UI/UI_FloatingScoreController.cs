using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;

public class UI_FloatingScoreController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private UI_GoalTextController goalTextController;
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

    public void UpdateScore(int turnTotal, bool isPlayerTurn)
    {
        // Detect turn switch: if turnTotal is less than currentTurnTotal, we've started a new turn
        if (turnTotal < currentTurnTotal)
        {
            currentTurnTotal = 0;
        }
        
        // Calculate the roll value (difference from previous total)
        int rollValue = turnTotal - currentTurnTotal;
        currentTurnTotal = turnTotal;

        if (scoreText != null)
        {
            scoreText.text = rollValue.ToString();

            // Punch animation
            scoreText.transform.DOKill();
            scoreText.transform.localScale = Vector3.one;
            scoreText.transform.DOPunchScale(Vector3.one * (scorePunchScale - 1f), scorePunchDuration, 5, 0.5f);
        }
        
        // Animate visual element
        if (scoreVisualElement != null)
        {
            scoreVisualElement.DOKill();
            
            // Scale up to target scale with punch
            scoreVisualElement.localScale = Vector3.zero;
            scoreVisualElement.DOScale(visualElementTargetScale, visualElementScaleDuration)
                .SetEase(Ease.OutBack)
                .SetDelay(visualElementTimeOffset);
            scoreVisualElement.DOPunchScale(Vector3.one * visualElementPunchScale, scorePunchDuration, 5, 0.5f)
                .SetDelay(visualElementScaleDuration + visualElementTimeOffset);
            
            // Rotate to target rotation
            scoreVisualElement.localRotation = Quaternion.identity;
            scoreVisualElement.DOLocalRotate(new Vector3(0, 0, visualElementRotation), visualElementScaleDuration + scorePunchDuration)
                .SetEase(Ease.OutCubic)
                .SetDelay(visualElementTimeOffset);
        }

        // Stop any existing transfer animation
        if (scoreTransferCoroutine != null)
        {
            StopCoroutine(scoreTransferCoroutine);
        }

        // Start score transfer animation
        scoreTransferCoroutine = StartCoroutine(TransferScoreAnimation(rollValue, turnTotal, isPlayerTurn));
    }

    #endregion

    #region Animation

    private IEnumerator TransferScoreAnimation(int rollValue, int turnTotal, bool isPlayerTurn)
    {
        // Wait for punch animation to finish plus delay
        yield return new WaitForSeconds(scorePunchDuration + scoreTransferDelay);

        // Get starting value for goal text animation (previous total)
        int startingTotal = turnTotal - rollValue;

        // Simultaneously count down score text (roll value) and count up goal text (turn total)
        for (int i = 0; i <= rollValue; i++)
        {
            int remainingRoll = rollValue - i;
            int currentTotal = startingTotal + i;

            // Update score text (counting down the roll value)
            if (scoreText != null)
            {
                scoreText.text = remainingRoll > 0 ? remainingRoll.ToString() : "";
            }

            // Update goal text (counting up the turn total)
            if (goalTextController != null)
            {
                goalTextController.SetRollProgressImmediate(currentTotal, isPlayerTurn);
            }

            yield return new WaitForSeconds(scoreTransferSpeed);
        }
        
        // Scale down visual element when score reaches zero
        if (scoreVisualElement != null)
        {
            scoreVisualElement.DOScale(0f, visualElementScaleDuration)
                .SetEase(Ease.InBack);
        }

        scoreTransferCoroutine = null;
    }

    #endregion
}
