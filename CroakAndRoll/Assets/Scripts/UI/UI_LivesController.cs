using UnityEngine;
using UnityEngine.UI;

public class UI_LivesController : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Life Images")]
    [SerializeField] private Image[] lifeImages;
    
    [Header("Visual Settings")]
    [Tooltip("Alpha value when life is active")]
    [SerializeField] private float activeAlpha = 1f;
    [Tooltip("Alpha value when life is lost")]
    [SerializeField] private float inactiveAlpha = 0.3f;
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Updates the lives display based on current and max lives
    /// </summary>
    /// <param name="currentLives">Number of lives remaining</param>
    /// <param name="maxLives">Maximum number of lives</param>
    public void UpdateLivesDisplay(int currentLives, int maxLives)
    {
        if (lifeImages == null || lifeImages.Length == 0)
        {
            Debug.LogWarning("UI_LivesController: No life images assigned!");
            return;
        }
        
        // Loop through all life images
        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] == null) continue;
            
            // Show image if within max lives range
            if (i < maxLives)
            {
                lifeImages[i].gameObject.SetActive(true);
                
                // Set alpha based on whether this life is active
                Color color = lifeImages[i].color;
                color.a = (i < currentLives) ? activeAlpha : inactiveAlpha;
                lifeImages[i].color = color;
            }
            else
            {
                // Hide images beyond max lives
                lifeImages[i].gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Initialize the display with starting values
    /// </summary>
    public void Initialize(int currentLives, int maxLives)
    {
        UpdateLivesDisplay(currentLives, maxLives);
    }
    
    #endregion
}
