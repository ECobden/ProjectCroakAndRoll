using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using MoreMountains.Feedbacks;

public class DB_RoundManager : MonoBehaviour
{
    [Header("Round Settings")]
    [SerializeField] private int currentRound = 0;
    
    [Header("UI Elements")]
    [SerializeField] private UI_Title uiTitle;
    [SerializeField] private CanvasGroup roundCountCanvasGroup;
    [SerializeField] private TextMeshProUGUI roundCountText;
    
    [Header("Round Text Animation")]
    [SerializeField] private float deleteSpeed = 0.05f;
    [SerializeField] private float typeSpeed = 0.05f;
    
    public static DB_RoundManager Instance { get; private set; }
    
    public int CurrentRound => currentRound;
    
    public event Action<int> OnRoundChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    public void InitializeRound()
    {
        UpdateRoundUI();
    }
    
    public void CountUpRound()
    {
        currentRound++;
        UpdateRoundUI();

        OnRoundChanged?.Invoke(currentRound);
    }
    
    private void UpdateRoundUI()
    {
        if (roundCountText != null)
        {
            StartCoroutine(AnimateRoundTextChange($"Round {currentRound}"));
        }
    }
    
    private IEnumerator AnimateRoundTextChange(string newText)
    {
        // Delete current text character by character
        string currentText = roundCountText.text;
        while (currentText.Length > 0)
        {
            currentText = currentText.Substring(0, currentText.Length - 1);
            roundCountText.text = currentText;
            yield return new WaitForSeconds(deleteSpeed);
        }
        
        // Type new text character by character
        for (int i = 0; i <= newText.Length; i++)
        {
            roundCountText.text = newText.Substring(0, i);
            yield return new WaitForSeconds(typeSpeed);
        }
    }
    
    public void ResetRounds()
    {
        currentRound = 1;
        UpdateRoundUI();
    }

    public void ShowRoundUi()
    {
        if (roundCountCanvasGroup != null)
        {
            roundCountCanvasGroup.alpha = 1f;
        }
    }

    public void HideRoundUi()
    {
        if (roundCountCanvasGroup != null)
        {
            roundCountCanvasGroup.alpha = 0f;
        }
    }
}
