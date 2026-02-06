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
        QualitySettings.vSyncCount = useVSync ? vSyncCount : 0;
        if (!useVSync)
            Application.targetFrameRate = targetFrameRate;
        // Optionally adjust physics timestep when changing frame rate:
        // Time.fixedDeltaTime = 1f / Mathf.Max(50f, targetFrameRate); // example
    }

    // Optional runtime setters
    public void SetTargetFrameRate(int fps)
    {
        targetFrameRate = fps;
        if (!useVSync) Application.targetFrameRate = fps;
    }

    public void SetVSync(bool enabled, int count = 1)
    {
        useVSync = enabled;
        vSyncCount = count;
        QualitySettings.vSyncCount = enabled ? count : 0;
        if (!enabled) Application.targetFrameRate = targetFrameRate;
    }
}
