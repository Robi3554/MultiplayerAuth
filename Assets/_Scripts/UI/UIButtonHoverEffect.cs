using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Lightweight pointer/hover polish for UI buttons.
///
/// On pointer enter → lerp localScale up, brighten outline alpha.
/// On pointer down  → squish slightly.
/// On pointer exit  → settle back to baseline (or "selected" baseline if SetSelected(true)).
///
/// No DOTween dependency. Pure C#.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIButtonHoverEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.04f;
    [SerializeField] private float pressScale = 0.97f;
    [SerializeField] private float responseTime = 0.10f;

    [Header("Outline (optional)")]
    [Tooltip("Outline component on the same object whose alpha should pulse on hover/selected.")]
    [SerializeField] private Outline outline;
    [SerializeField, Range(0f, 1f)] private float outlineRestAlpha = 0.0f;
    [SerializeField, Range(0f, 1f)] private float outlineHoverAlpha = 0.55f;
    [SerializeField, Range(0f, 1f)] private float outlineSelectedAlpha = 1.0f;

    [Header("Inner Glow (optional)")]
    [Tooltip("A child Image used as a soft glow; its alpha is animated alongside outline.")]
    [SerializeField] private Image innerGlow;
    [SerializeField, Range(0f, 1f)] private float glowRestAlpha = 0.0f;
    [SerializeField, Range(0f, 1f)] private float glowHoverAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] private float glowSelectedAlpha = 0.8f;

    [Header("Selected Pulse")]
    [Tooltip("If true, while selected the outline alpha pulses softly (sine).")]
    [SerializeField] private bool pulseWhenSelected = false;
    [SerializeField] private float pulseSpeed = 2.4f;
    [SerializeField, Range(0f, 1f)] private float pulseAmplitude = 0.35f;

    private RectTransform _rt;
    private Vector3 _baseScale;
    private bool _hovered;
    private bool _pressed;
    private bool _selected;
    private float _scaleVel;
    private Vector3 _scaleVelVec;

    public bool IsSelected => _selected;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _baseScale = _rt.localScale;
        ApplyOutlineAlpha(outlineRestAlpha);
        ApplyGlowAlpha(glowRestAlpha);
    }

    private void OnDisable()
    {
        _hovered = false;
        _pressed = false;
        if (_rt != null) _rt.localScale = _baseScale;
    }

    private void Update()
    {
        // Smoothly lerp toward target scale
        Vector3 target = TargetScale();
        _rt.localScale = Vector3.SmoothDamp(_rt.localScale, target, ref _scaleVelVec, responseTime);

        if (pulseWhenSelected && _selected && !_hovered && !_pressed)
        {
            float s = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f; // 0..1
            float pulse = Mathf.Lerp(outlineSelectedAlpha - pulseAmplitude * 0.5f, outlineSelectedAlpha + pulseAmplitude * 0.5f, s);
            ApplyOutlineAlpha(Mathf.Clamp01(pulse));
            ApplyGlowAlpha(Mathf.Clamp01(Mathf.Lerp(glowSelectedAlpha - pulseAmplitude * 0.4f, glowSelectedAlpha, s)));
        }
    }

    private Vector3 TargetScale()
    {
        if (_pressed) return _baseScale * pressScale;
        if (_hovered) return _baseScale * hoverScale;
        if (_selected) return _baseScale * Mathf.Lerp(1f, hoverScale, 0.4f);
        return _baseScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        ApplyOutlineAlpha(_selected ? outlineSelectedAlpha : outlineHoverAlpha);
        ApplyGlowAlpha(_selected ? glowSelectedAlpha : glowHoverAlpha);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        _pressed = false;
        ApplyOutlineAlpha(_selected ? outlineSelectedAlpha : outlineRestAlpha);
        ApplyGlowAlpha(_selected ? glowSelectedAlpha : glowRestAlpha);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
    }

    /// <summary>
    /// Toggle the persistent "selected" state. Brightens the outline + glow and
    /// (optionally) starts pulsing.
    /// </summary>
    public void SetSelected(bool value)
    {
        _selected = value;
        ApplyOutlineAlpha(_selected ? outlineSelectedAlpha : (_hovered ? outlineHoverAlpha : outlineRestAlpha));
        ApplyGlowAlpha(_selected ? glowSelectedAlpha : (_hovered ? glowHoverAlpha : glowRestAlpha));
    }

    /// <summary>
    /// Wire the optional outline/glow at runtime if not assigned in the inspector.
    /// </summary>
    public void Bind(Outline outlineComp, Image glowImage)
    {
        outline = outlineComp;
        innerGlow = glowImage;
        ApplyOutlineAlpha(outlineRestAlpha);
        ApplyGlowAlpha(glowRestAlpha);
    }

    /// <summary>
    /// Enable a soft alpha pulse on the outline/glow while in the selected state.
    /// </summary>
    public void EnableSelectedPulse(float speed = 2.4f, float amplitude = 0.35f)
    {
        pulseWhenSelected = true;
        pulseSpeed = speed;
        pulseAmplitude = Mathf.Clamp01(amplitude);
    }

    private void ApplyOutlineAlpha(float a)
    {
        if (outline == null) return;
        Color c = outline.effectColor;
        c.a = Mathf.Clamp01(a);
        outline.effectColor = c;
    }

    private void ApplyGlowAlpha(float a)
    {
        if (innerGlow == null) return;
        Color c = innerGlow.color;
        c.a = Mathf.Clamp01(a);
        innerGlow.color = c;
    }
}
