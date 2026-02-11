using UnityEngine;

public class FrameRateLimiter : MonoBehaviour
{
    [Header("Frame Rate")]
    public bool useVSync = false;
    [Tooltip("VSync count (0 = off). If >0, Application.targetFrameRate is ignored.")]
    public int vSyncCount = 0;
    [Tooltip("If VSync off, target frame rate used.")]
    public int targetFrameRate = 60;

    void Awake()
    {
        ApplyFrameRateSettings();
    }

    private void ApplyFrameRateSettings()
    {
        if (useVSync)
        {
            // Enable VSync (typically 1 = every VBlank, 2 = every second VBlank)
            QualitySettings.vSyncCount = Mathf.Max(1, vSyncCount);
            // VSync overrides targetFrameRate, but set to -1 to indicate no limit beyond VSync
            Application.targetFrameRate = -1;
            Debug.Log($"[FrameRateLimiter] VSync enabled with count: {QualitySettings.vSyncCount}");
        }
        else
        {
            // Disable VSync
            QualitySettings.vSyncCount = 0;
            // Set target frame rate
            Application.targetFrameRate = targetFrameRate;
            
            #if UNITY_EDITOR
            Debug.LogWarning($"[FrameRateLimiter] Target FPS set to {targetFrameRate}. Note: Application.targetFrameRate may not work reliably in Unity Editor. Use VSync or test in a build for accurate frame rate limiting.");
            #else
            Debug.Log($"[FrameRateLimiter] Target FPS set to {targetFrameRate}");
            #endif
        }
        
        // Force Unity to respect the settings
        QualitySettings.maxQueuedFrames = 0;
    }

    // Optional runtime setters
    public void SetTargetFrameRate(int fps)
    {
        targetFrameRate = fps;
        ApplyFrameRateSettings();
    }

    public void SetVSync(bool enabled, int count = 1)
    {
        useVSync = enabled;
        vSyncCount = count;
        ApplyFrameRateSettings();
    }
}
