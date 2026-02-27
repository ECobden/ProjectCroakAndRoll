using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;

public class UI_GoalTextController : MonoBehaviour
{
    #region Serialized Fields

    [SerializeField] private TextMeshProUGUI goalText;
    
    [Header("Animation Settings")]
    [SerializeField] private float deleteSpeed = 0.03f;
    [SerializeField] private float typeSpeed = 0.05f;
    [SerializeField] private float countUpDuration = 0.3f;

    #endregion

    #region Private Fields

    private Coroutine currentAnimation;
    private int currentRollValue = 0;
    private bool isPlayerTurn = true;
    private const int MAX_ROLL_VALUE = 21;

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the goal text with a typewriter animation (delete then type).
    /// Use this for state transitions like "Select your bet", "Roll Closest to 21", etc.
    /// </summary>
    public void SetGoalText(string newText)
    {
        if (goalText == null) return;

        // Cancel any ongoing animation
        StopCurrentAnimation();

        // Start the delete-then-type animation
        currentAnimation = StartCoroutine(DeleteAndTypeText(newText));
    }

    /// <summary>
    /// Updates the roll progress text with a counting animation.
    /// Use this during gameplay to show "Player: X / 21" or "House: X / 21" with the number counting up.
    /// </summary>
    public void UpdateRollProgress(int newValue, bool isPlayerTurn)
    {
        if (goalText == null) return;

        this.isPlayerTurn = isPlayerTurn;

        // Cancel any ongoing animation
        StopCurrentAnimation();

        // Animate counting from current value to new value
        currentAnimation = StartCoroutine(CountUpRollValue(currentRollValue, newValue));
    }

    /// <summary>
    /// Sets the roll progress value immediately without animation.
    /// Use this when coordinating with other animations like the floating score.
    /// </summary>
    public void SetRollProgressImmediate(int value)
    {
        if (goalText == null) return;

        currentRollValue = value;

        // Cancel any ongoing animation
        StopCurrentAnimation();

        goalText.text = $"{value} / {MAX_ROLL_VALUE}";
    }

    /// <summary>
    /// Sets the text immediately without animation.
    /// </summary>
    public void SetTextImmediate(string text)
    {
        if (goalText == null) return;

        StopCurrentAnimation();
        goalText.text = text;
    }

    /// <summary>
    /// Resets the roll progress counter to zero.
    /// </summary>
    public void ResetRollProgress()
    {
        currentRollValue = 0;
    }

    #endregion

    #region Animation Coroutines

    private IEnumerator DeleteAndTypeText(string newText)
    {
        if (goalText == null) yield break;

        string currentText = goalText.text;

        // Delete existing text character by character
        while (currentText.Length > 0)
        {
            currentText = currentText.Substring(0, currentText.Length - 1);
            goalText.text = currentText;
            yield return new WaitForSeconds(deleteSpeed);
        }

        // Small pause between delete and type
        yield return new WaitForSeconds(0.1f);

        // Type new text character by character
        currentText = "";
        foreach (char c in newText)
        {
            currentText += c;
            goalText.text = currentText;
            yield return new WaitForSeconds(typeSpeed);
        }

        currentAnimation = null;
    }

    private IEnumerator CountUpRollValue(int fromValue, int toValue)
    {
        if (goalText == null) yield break;

        string playerName = isPlayerTurn ? "Player" : "House";
        float elapsed = 0f;
        
        while (elapsed < countUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / countUpDuration);
            
            // Lerp between values
            int displayValue = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, t));
            goalText.text = $"{playerName}: {displayValue} / {MAX_ROLL_VALUE}";
            
            yield return null;
        }

        // Ensure final value is set
        currentRollValue = toValue;
        goalText.text = $"{playerName}: {currentRollValue} / {MAX_ROLL_VALUE}";
        
        currentAnimation = null;
    }

    private void StopCurrentAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }

    #endregion
}
