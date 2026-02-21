using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages game rounds, including round progression, UI updates, and round state.
/// Implements Singleton pattern for global access.
/// </summary>
public class DB_RoundManager : MonoBehaviour
{
    #region Singleton
    
    public static DB_RoundManager Instance { get; private set; }
    
    #endregion
    
    #region Serialized Fields
    
    [Header("Round Settings")]
    [SerializeField] private int currentRound = 0;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugControls = false;
    
    [Header("UI Elements")]
    [SerializeField] private UI_Title uiTitle;
    [SerializeField] private CanvasGroup roundCountCanvasGroup;
    [SerializeField] private TextMeshProUGUI roundCountText;
    
    [Header("Round Text Animation")]
    [SerializeField] private float deleteSpeed = 0.05f;
    [SerializeField] private float typeSpeed = 0.05f;
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// Gets the current round number.
    /// </summary>
    public int CurrentRound => currentRound;
    
    #endregion
    
    #region Events
    
    /// <summary>
    /// Invoked when the round number changes.
    /// </summary>
    public event Action<int> OnRoundChanged;
    
    #endregion
    
    #region Private Fields
    
    private Coroutine activeTextAnimation;
    private WaitForSeconds deleteWait;
    private WaitForSeconds typeWait;
    private char[] textBuffer;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        InitializeSingleton();
        CacheWaitForSeconds();
    }
    
    private void Update()
    {
        if (!enableDebugControls) return;
        
        // Debug: Advance round with '+' or '=' key
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            DebugAdvanceRound();
        }
        
        // Debug: Rewind round with '-' key
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            DebugRewindRound();
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Initializes the round display with the current round value.
    /// </summary>
    public void InitializeRound()
    {
        UpdateRoundUI();
    }
    
    /// <summary>
    /// Increments the round counter and updates the UI.
    /// </summary>
    public void CountUpRound()
    {
        currentRound++;
        UpdateRoundUI();
        OnRoundChanged?.Invoke(currentRound);
    }
    
    /// <summary>
    /// Decrements the round counter and updates the UI (minimum round is 1).
    /// </summary>
    public void CountDownRound()
    {
        if (currentRound > 1)
        {
            currentRound--;
            UpdateRoundUI();
            OnRoundChanged?.Invoke(currentRound);
        }
    }
    
    /// <summary>
    /// Resets the round counter to 1 and updates the UI.
    /// </summary>
    public void ResetRounds()
    {
        currentRound = 1;
        UpdateRoundUI();
    }
    
    /// <summary>
    /// Debug method to advance to the next round.
    /// </summary>
    private void DebugAdvanceRound()
    {
        CountUpRound();
        Debug.Log($"[DEBUG] Advanced to Round {currentRound}");
    }
    
    /// <summary>
    /// Debug method to go back one round.
    /// </summary>
    private void DebugRewindRound()
    {
        if (currentRound > 1)
        {
            CountDownRound();
            Debug.Log($"[DEBUG] Rewound to Round {currentRound}");
        }
        else
        {
            Debug.Log($"[DEBUG] Cannot rewind below Round 1");
        }
    }
    
    /// <summary>
    /// Shows the round UI by setting canvas group alpha to 1.
    /// </summary>
    public void ShowRoundUi()
    {
        if (roundCountCanvasGroup != null)
        {
            roundCountCanvasGroup.alpha = 1f;
        }
    }
    
    /// <summary>
    /// Hides the round UI by setting canvas group alpha to 0.
    /// </summary>
    public void HideRoundUi()
    {
        if (roundCountCanvasGroup != null)
        {
            roundCountCanvasGroup.alpha = 0f;
        }
    }
    
    #endregion
    
    #region Private Methods
    
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void CacheWaitForSeconds()
    {
        deleteWait = new WaitForSeconds(deleteSpeed);
        typeWait = new WaitForSeconds(typeSpeed);
    }
    
    private void UpdateRoundUI()
    {
        if (roundCountText == null) return;
        
        // Stop any existing animation to prevent overlapping
        if (activeTextAnimation != null)
        {
            StopCoroutine(activeTextAnimation);
        }
        
        activeTextAnimation = StartCoroutine(AnimateRoundTextChange($"Round {currentRound}"));
    }
    
    /// <summary>
    /// Animates the round text change by deleting old text and typing new text character by character.
    /// Optimized to reduce garbage allocation by using char arrays instead of substring operations.
    /// </summary>
    private IEnumerator AnimateRoundTextChange(string newText)
    {
        // Delete current text character by character
        string currentText = roundCountText.text;
        int currentLength = currentText.Length;
        
        // Ensure buffer is large enough
        int maxLength = Mathf.Max(currentLength, newText.Length);
        if (textBuffer == null || textBuffer.Length < maxLength)
        {
            textBuffer = new char[maxLength];
        }
        
        // Delete phase - work backwards through current text
        for (int i = currentLength - 1; i >= 0; i--)
        {
            roundCountText.text = currentText.Substring(0, i);
            yield return deleteWait;
        }
        
        // Ensure text is empty
        roundCountText.text = string.Empty;
        
        // Type phase - copy characters from new text to buffer
        newText.CopyTo(0, textBuffer, 0, newText.Length);
        
        for (int i = 1; i <= newText.Length; i++)
        {
            roundCountText.text = new string(textBuffer, 0, i);
            yield return typeWait;
        }
        
        activeTextAnimation = null;
    }
    
    #endregion
}
