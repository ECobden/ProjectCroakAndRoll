using UnityEngine;
using TMPro;
using System.Collections;

public class UI_MoneyController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI popupText;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private string prefix = "$";
    
    [Header("Popup Settings")]
    [SerializeField] private float popupFadeInDuration = 0.2f;
    [SerializeField] private float popupDisplayDuration = 1f;
    [SerializeField] private float popupFadeOutDuration = 0.3f;
    [SerializeField] private float popupMoveDistance = 30f;
    
    private int currentDisplayedValue = 0;
    private Coroutine countCoroutine;
    private Coroutine popupCoroutine;
    private Vector3 popupStartPosition;
    
    private void Start()
    {
        UpdateTextImmediate(currentDisplayedValue);
        
        if (popupText != null)
        {
            popupStartPosition = popupText.transform.localPosition;
            Color c = popupText.color;
            c.a = 0f;
            popupText.color = c;
        }
    }
    
    public void SetMoneyValue(int newValue)
    {
        int difference = newValue - currentDisplayedValue;
        
        // Spawn popup if there's a change
        if (difference != 0)
        {
            SpawnPopup(difference);
        }
        
        if (countCoroutine != null)
        {
            StopCoroutine(countCoroutine);
        }
        
        countCoroutine = StartCoroutine(CountToValue(newValue));
    }
    
    public void SetMoneyValueImmediate(int newValue)
    {
        if (countCoroutine != null)
        {
            StopCoroutine(countCoroutine);
            countCoroutine = null;
        }
        
        currentDisplayedValue = newValue;
        UpdateTextImmediate(newValue);
    }
    
    private IEnumerator CountToValue(int targetValue)
    {
        int startValue = currentDisplayedValue;
        int difference = targetValue - startValue;
        
        if (difference == 0)
        {
            countCoroutine = null;
            yield break;
        }
        
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            
            currentDisplayedValue = (int)Mathf.Lerp(startValue, targetValue, t);
            UpdateTextImmediate(currentDisplayedValue);
            
            yield return null;
        }
        
        // Ensure we end exactly at target value
        currentDisplayedValue = targetValue;
        UpdateTextImmediate(targetValue);
        
        countCoroutine = null;
    }
    
    private void UpdateTextImmediate(int value)
    {
        if (moneyText != null)
        {
            moneyText.text = prefix + value.ToString();
        }
    }
    
    private void SpawnPopup(int amount)
    {
        if (popupText == null)
        {
            return;
        }
        
        // Stop any existing popup animation
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
        }
        
        popupCoroutine = StartCoroutine(AnimatePopup(amount));
    }
    
    private IEnumerator AnimatePopup(int amount)
    {
        // Set text
        string prefix = amount > 0 ? "+" : "";
        popupText.text = prefix + amount.ToString();
        
        // Reset to start position
        popupText.transform.localPosition = popupStartPosition;
        
        Color textColor = popupText.color;
        textColor.a = 0f;
        popupText.color = textColor;
        
        float totalDuration = popupFadeInDuration + popupDisplayDuration + popupFadeOutDuration;
        float elapsed = 0f;
        
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / totalDuration;
            
            // Calculate alpha based on current phase
            float alpha;
            if (elapsed < popupFadeInDuration)
            {
                alpha = Mathf.Lerp(0f, 1f, elapsed / popupFadeInDuration);
            }
            else if (elapsed < popupFadeInDuration + popupDisplayDuration)
            {
                alpha = 1f;
            }
            else
            {
                float fadeOutElapsed = elapsed - popupFadeInDuration - popupDisplayDuration;
                alpha = Mathf.Lerp(1f, 0f, fadeOutElapsed / popupFadeOutDuration);
            }
            
            textColor.a = alpha;
            popupText.color = textColor;
            
            // Smooth upward movement
            float moveProgress = Mathf.SmoothStep(0f, 1f, normalizedTime);
            popupText.transform.localPosition = popupStartPosition + Vector3.up * (popupMoveDistance * moveProgress);
            
            yield return null;
        }
        
        // Ensure fully transparent and reset position
        textColor.a = 0f;
        popupText.color = textColor;
        popupText.transform.localPosition = popupStartPosition;
        
        popupCoroutine = null;
    }
}
