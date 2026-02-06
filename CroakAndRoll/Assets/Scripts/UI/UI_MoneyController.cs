using UnityEngine;
using TMPro;
using System.Collections;

public class UI_MoneyController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI moneyText;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private string prefix = "$";
    
    private int currentDisplayedValue = 0;
    private Coroutine countCoroutine;
    
    private void Start()
    {
        UpdateTextImmediate(currentDisplayedValue);
    }
    
    public void SetMoneyValue(int newValue)
    {
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
}
