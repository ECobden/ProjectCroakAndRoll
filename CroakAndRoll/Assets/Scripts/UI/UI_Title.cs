using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;

public class UI_Title : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float textSequenceDelay = 0.5f;
    
    [Header("Round Animation")]
    [SerializeField] private float roundCountDuration = 1f;
    
    private void Awake()
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
        }
    }
    
    /// <summary>
    /// Shows the panel with a fade-in effect and displays a sequence of text
    /// </summary>
    public void ShowTextSequence(string[] textSequence)
    {
        StartCoroutine(ShowTextSequenceCoroutine(textSequence));
    }
    
    /// <summary>
    /// Shows the panel with the round number and animates it counting up
    /// </summary>
    public void ShowRoundChange(int fromRound, int toRound)
    {
        StartCoroutine(ShowRoundChangeCoroutine(fromRound, toRound));
    }
    
    /// <summary>
    /// Fades the panel in
    /// </summary>
    public IEnumerator FadeIn()
    {
        panelCanvasGroup.DOFade(1f, fadeDuration);
        yield return new WaitForSeconds(fadeDuration);
    }
    
    /// <summary>
    /// Fades the panel out
    /// </summary>
    public IEnumerator FadeOut()
    {
        panelCanvasGroup.DOFade(0f, fadeDuration);
        yield return new WaitForSeconds(fadeDuration);
    }
    
    private IEnumerator ShowTextSequenceCoroutine(string[] textSequence)
    {
        // Display each text in sequence
        foreach (string text in textSequence)
        {
            if (titleText != null)
            {
                titleText.text = text;
            }
            yield return new WaitForSeconds(textSequenceDelay);
        }
        
        // Hold on the last text
        yield return new WaitForSeconds(displayDuration);
        
    }
    
    private IEnumerator ShowRoundChangeCoroutine(int fromRound, int toRound)
    {
        // Animate round number counting up
        float elapsedTime = 0f;
        while (elapsedTime < roundCountDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / roundCountDuration;
            int currentDisplayRound = Mathf.RoundToInt(Mathf.Lerp(fromRound, toRound, t));
            
            if (titleText != null)
            {
                titleText.text = $"Round {currentDisplayRound}";
            }
            
            yield return null;
        }
        
        // Ensure final round number is displayed
        if (titleText != null)
        {
            titleText.text = $"Round {toRound}";
        }
        
        // Hold on the final round number
        yield return new WaitForSeconds(displayDuration);
    }
}
