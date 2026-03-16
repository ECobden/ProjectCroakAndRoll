using UnityEngine;
using DG.Tweening;

public class UI_Element_RotateTween : MonoBehaviour
{
    public enum PlayTrigger
    {
        OnStart,
        OnEnable,
        Manual
    }

    public enum RotationMode
    {
        Spin,         // Rotate by rotationPerCycle continuously
        BounceRange   // Oscillate between fromRotation and toRotation
    }

    [Header("Target")]
    [SerializeField] private RectTransform target;

    [Header("Play Trigger")]
    [SerializeField] private PlayTrigger playTrigger = PlayTrigger.OnStart;

    [Header("Rotation")]
    [SerializeField] private RotationMode rotationMode = RotationMode.Spin;

    [Tooltip("Spin mode: euler angles rotated per cycle")]
    [SerializeField] private Vector3 rotationPerCycle = new Vector3(0f, 0f, 360f);
    [SerializeField] private RotateMode rotateMode = RotateMode.FastBeyond360;
    [SerializeField] private bool relativeRotation = true;

    [Tooltip("BounceRange mode: explicit start/end euler angles")]
    [SerializeField] private Vector3 fromRotation = new Vector3(0f, 0f, -15f);
    [SerializeField] private Vector3 toRotation = new Vector3(0f, 0f, 15f);

    [SerializeField] private float duration = 1f;
    [SerializeField] private float delay = 0f;
    [SerializeField] private Ease ease = Ease.InOutSine;

    [Header("Loop")]
    [SerializeField] private bool loop = true;
    [SerializeField] private int loopCount = -1;
    [SerializeField] private LoopType loopType = LoopType.Restart;

    [Header("Runtime")]
    [SerializeField] private bool ignoreTimeScale = true;
    [SerializeField] private bool stopOnDisable = true;

    private Tween rotationTween;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (playTrigger == PlayTrigger.OnStart)
            Play();
    }

    private void OnEnable()
    {
        if (playTrigger == PlayTrigger.OnEnable)
            Play();
    }

    private void OnDisable()
    {
        if (stopOnDisable)
            Stop();
    }

    private void OnDestroy()
    {
        Stop();
    }

    [ContextMenu("Play Tween")]
    public void Play()
    {
        if (target == null)
            return;

        Stop();

        float safeDuration = Mathf.Max(0.01f, duration);
        float safeDelay = Mathf.Max(0f, delay);
        int loops = loop ? (loopCount <= 0 ? -1 : loopCount) : 0;

        if (rotationMode == RotationMode.BounceRange)
        {
            // Snap immediately to fromRotation, then tween to toRotation with Yoyo looping
            target.localRotation = Quaternion.Euler(fromRotation);

            rotationTween = target
                .DOLocalRotate(toRotation, safeDuration)
                .SetEase(ease)
                .SetDelay(safeDelay)
                .SetUpdate(ignoreTimeScale)
                .SetLoops(loops == 0 ? 1 : loops, LoopType.Yoyo);
        }
        else
        {
            rotationTween = target
                .DOLocalRotate(rotationPerCycle, safeDuration, rotateMode)
                .SetEase(ease)
                .SetDelay(safeDelay)
                .SetUpdate(ignoreTimeScale);

            if (relativeRotation)
                rotationTween.SetRelative();

            if (loop)
                rotationTween.SetLoops(loops, loopType);
        }
    }

    [ContextMenu("Stop Tween")]
    public void Stop()
    {
        if (rotationTween != null && rotationTween.IsActive())
            rotationTween.Kill();

        rotationTween = null;
    }

    [ContextMenu("Restart Tween")]
    public void RestartTween()
    {
        Play();
    }
}
