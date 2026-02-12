using UnityEngine;
using UnityEngine.UI;

public class UI_HeatController : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Heat Images")]
    [SerializeField] private Image[] heatImages;
    
    [Header("Visual Settings")]
    [Tooltip("Alpha value when heat level is active")]
    [SerializeField] private float activeAlpha = 1f;
    [Tooltip("Alpha value when heat level is inactive")]
    [SerializeField] private float inactiveAlpha = 0.3f;
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Updates the heat display based on current and max heat
    /// </summary>
    /// <param name="currentHeat">Current heat level (0-8)</param>
    /// <param name="maxHeat">Maximum heat level (typically 8)</param>
    public void UpdateHeatDisplay(int currentHeat, int maxHeat)
    {
        if (heatImages == null || heatImages.Length == 0)
        {
            Debug.LogWarning("UI_HeatController: No heat images assigned!");
            return;
        }
        
        // Loop through all heat images
        for (int i = 0; i < heatImages.Length; i++)
        {
            if (heatImages[i] == null) continue;
            
            // Show image if within max heat range
            if (i < maxHeat)
            {
                heatImages[i].gameObject.SetActive(true);
                
                // Set alpha based on whether this heat level is active
                // Heat is 1-indexed in display (heat level 1 = first image)
                Color color = heatImages[i].color;
                color.a = (i < currentHeat) ? activeAlpha : inactiveAlpha;
                heatImages[i].color = color;
            }
            else
            {
                // Hide images beyond max heat
                heatImages[i].gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Initialize the display with starting values
    /// </summary>
    public void Initialize(int currentHeat, int maxHeat)
    {
        UpdateHeatDisplay(currentHeat, maxHeat);
    }
    
    #endregion
}
