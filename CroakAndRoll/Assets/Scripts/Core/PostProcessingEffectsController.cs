using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Centralized controller for runtime post-processing effect pulses.
/// Any script can trigger effects via <see cref="Instance"/>.
/// </summary>
public class PostProcessingEffectsController : MonoBehaviour
{
    public static PostProcessingEffectsController Instance { get; private set; }

    [Header("Volume Reference")]
    [SerializeField] private Volume postProcessVolume;

    [Header("Chromatic Aberration Pulse")]
    [SerializeField] private float defaultChromaticIntensity = 0.15f;
    [SerializeField] private float defaultChromaticDuration = 0.5f;

    private ChromaticAberration chromaticAberration;
    private Coroutine chromaticPulseCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheEffectsFromVolume();
    }

    /// <summary>
    /// Pulses chromatic aberration to a target intensity and eases back to zero.
    /// </summary>
    public void PulseChromaticAberration()
    {
        PulseChromaticAberration(defaultChromaticIntensity, defaultChromaticDuration);
    }

    /// <summary>
    /// Pulses chromatic aberration to a target intensity and eases back to zero.
    /// </summary>
    public void PulseChromaticAberration(float intensity, float duration)
    {
        if (chromaticAberration == null)
        {
            CacheEffectsFromVolume();
            if (chromaticAberration == null)
                return;
        }

        if (chromaticPulseCoroutine != null)
            StopCoroutine(chromaticPulseCoroutine);

        chromaticPulseCoroutine = StartCoroutine(AnimateChromaticPulse(Mathf.Max(0f, intensity), Mathf.Max(0f, duration)));
    }

    private IEnumerator AnimateChromaticPulse(float intensity, float duration)
    {
        chromaticAberration.active = true;
        chromaticAberration.intensity.overrideState = true;
        chromaticAberration.intensity.value = intensity;

        if (duration <= 0f)
        {
            chromaticAberration.intensity.value = 0f;
            chromaticPulseCoroutine = null;
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            chromaticAberration.intensity.value = Mathf.Lerp(intensity, 0f, t);
            yield return null;
        }

        chromaticAberration.intensity.value = 0f;
        chromaticPulseCoroutine = null;
    }

    private void CacheEffectsFromVolume()
    {
        if (postProcessVolume == null)
            postProcessVolume = FindFirstObjectByType<Volume>();

        if (postProcessVolume == null || postProcessVolume.profile == null)
            return;

        postProcessVolume.profile.TryGet(out chromaticAberration);
    }
}