using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

/// <summary>
/// Builds the mobile controls overlay in the same "Fortnite-ish" visual language
/// as the lobby (see <see cref="LobbyLayoutBuilder"/>): chunky rounded-rect buttons,
/// thick yellow accents, bold uppercase typography, drop shadows, sheen + inner glow.
///
/// Unlike a pure runtime builder, this one is designed to be <b>baked once in edit
/// mode</b> so every control becomes a real, hand-editable GameObject you can move,
/// resize and restyle directly in the scene:
///   - "Rebuild Mobile Controls" — clears previously generated children and rebuilds
///     from the current inspector values (works in edit mode, recommended).
///   - "Clear" — removes only the children this component generated.
///
/// Recommended workflow: assign the TMP font, run "Rebuild Mobile Controls" once,
/// reposition/resize to taste, then leave <see cref="rebuildOnAwake"/> OFF so Play
/// mode never overwrites your manual edits. The visuals persist in the editor via
/// <see cref="LobbyProceduralSprite"/>.
///
/// Input is unchanged: every button keeps an <see cref="OnScreenButton"/> and the
/// joystick keeps an <see cref="OnScreenStick"/> with the same gamepad control paths,
/// so touches still flow through the existing InputSystem bindings.
/// </summary>
public class MobileControlsCanvas : MonoBehaviour
{
    [Header("Build Behavior")]
    [Tooltip("If true, the overlay is (re)built in Awake at runtime. Leave OFF once you have baked + hand-edited the hierarchy so Play mode does not clobber your tweaks.")]
    [SerializeField] private bool rebuildOnAwake = false;

    [Header("Font")]
    [Tooltip("TMP font for the button labels. Assign the same font used by LobbyLayoutBuilder for a consistent look. Falls back to the TMP default if empty.")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Joystick")]
    [SerializeField] private float joystickRadius = 150f;

    [Header("Button Sizes")]
    [SerializeField] private float primaryBtnSize = 130f;   // Attack
    [SerializeField] private float secondaryBtnSize = 90f;  // Jump, Reload, Dash
    [SerializeField] private float weaponArrowSize = 65f;
    [SerializeField] private float topBarBtnSize = 65f;     // Pause, Scoreboard

    [Header("Palette — Lobby")]
    [SerializeField] private Color accentYellow = new(1.00f, 0.82f, 0.18f, 1f);
    [SerializeField] private Color outlineDark = new(0.02f, 0.05f, 0.14f, 1f);
    [SerializeField] private Color labelColor = Color.white;

    [Header("Palette — Action Tints")]
    [SerializeField] private Color attackTint = new(0.95f, 0.30f, 0.34f, 0.88f);   // red — primary attack
    [SerializeField] private Color actionTint = new(0.30f, 0.55f, 1.00f, 0.85f);   // blue — jump / reload / dash
    [SerializeField] private Color weaponTint = new(1.00f, 0.65f, 0.18f, 0.85f);   // gold — weapon arrows
    [SerializeField] private Color topBarTint = new(0.13f, 0.21f, 0.52f, 0.80f);   // deep blue — pause / scoreboard
    [SerializeField] private Color joystickBgColor = new(0.10f, 0.16f, 0.40f, 0.55f);
    [SerializeField] private Color joystickKnobColor = new(0.30f, 0.55f, 1.00f, 0.85f);

    // Children this builder created (so Clear() never touches manually-added siblings).
    private readonly List<GameObject> _generatedChildren = new();

    // Top-level objects we generate — used as a fallback for Clear() after a domain
    // reload wipes _generatedChildren in the editor.
    private static readonly HashSet<string> GeneratedNames = new()
    {
        "JoystickBG", "AttackBtn", "JumpBtn", "ReloadBtn", "DashBtn",
        "WpnPrev", "WpnNext", "PauseBtn", "ScoreBtn"
    };

    private void Awake()
    {
        // Runtime-only: make sure touch input can reach the OnScreen* components.
        EnsureInputSystemEventSystem();

        if (rebuildOnAwake)
            Rebuild();
    }

    // ─── Editor-friendly entry points (right-click on this component) ──
    [ContextMenu("Rebuild Mobile Controls")]
    public void Rebuild()
    {
        Clear();
        EnsureCanvas();
        Build();
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = _generatedChildren.Count - 1; i >= 0; i--)
        {
            var go = _generatedChildren[i];
            if (go == null) continue;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
        _generatedChildren.Clear();

        // Belt + braces: remove any loose top-level children whose names match the
        // objects we generate (handles _generatedChildren being cleared by a reload).
        var toDelete = new List<GameObject>();
        foreach (Transform child in transform)
            if (GeneratedNames.Contains(child.name))
                toDelete.Add(child.gameObject);
        foreach (var go in toDelete)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }

    // ─── Canvas setup ─────────────────────────────────────────────────
    private void EnsureCanvas()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    // ─── Layout ───────────────────────────────────────────────────────
    private void Build()
    {
        // ── Left : Joystick ──
        BuildJoystick();

        // ── Right : Action cluster (center-right, raised toward middle) ──
        BuildButton("AttackBtn", "FIRE", primaryBtnSize,
            new Vector2(-180, 340), "<Gamepad>/rightShoulder", attackTint, TextAnchor.LowerRight, isPrimary: true);

        BuildButton("JumpBtn", "JUMP", secondaryBtnSize,
            new Vector2(-320, 260), "<Gamepad>/buttonSouth", actionTint, TextAnchor.LowerRight);

        BuildButton("ReloadBtn", "RLD", secondaryBtnSize,
            new Vector2(-320, 410), "<Gamepad>/buttonNorth", actionTint, TextAnchor.LowerRight);

        BuildButton("DashBtn", "DASH", secondaryBtnSize,
            new Vector2(-80, 220), "<Gamepad>/rightStickPress", actionTint, TextAnchor.LowerRight);

        // ── Weapon prev / next (next to action cluster) ──
        BuildButton("WpnPrev", "<", weaponArrowSize,
            new Vector2(-420, 310), "<Gamepad>/dpad/left", weaponTint, TextAnchor.LowerRight);

        BuildButton("WpnNext", ">", weaponArrowSize,
            new Vector2(-420, 390), "<Gamepad>/dpad/right", weaponTint, TextAnchor.LowerRight);

        // ── Pause + Scoreboard (upper-right) ──
        BuildButton("PauseBtn", "II", topBarBtnSize,
            new Vector2(-80, -80), "<Gamepad>/start", topBarTint, TextAnchor.UpperRight);

        BuildButton("ScoreBtn", "SCR", topBarBtnSize,
            new Vector2(-160, -80), "<Gamepad>/select", topBarTint, TextAnchor.UpperRight);
    }

    // ─── Joystick ─────────────────────────────────────────────────────
    private void BuildJoystick()
    {
        float diameter = joystickRadius * 2f;

        // Outer ring (deep blue fill + thick yellow accent ring, near-circular).
        var bg = MakeRect("JoystickBG", transform, diameter, diameter);
        Track(bg.gameObject);
        Anchor(bg, TextAnchor.LowerLeft);
        bg.anchoredPosition = new Vector2(220, 340);

        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.color = Color.white;
        bgImg.raycastTarget = true;
        AttachRoundedRect(bgImg, CircleRadius(diameter), 6, joystickBgColor, accentYellow);
        bgImg.pixelsPerUnitMultiplier = 1f;

        var bgShadow = bg.gameObject.AddComponent<Shadow>();
        bgShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        bgShadow.effectDistance = new Vector2(0f, -4f);

        // Inner knob (lighter blue, dark border).
        float knobSize = joystickRadius * 0.75f;
        var knob = MakeRect("JoystickKnob", bg, knobSize, knobSize);
        SetCenter(knob);
        knob.anchoredPosition = Vector2.zero;

        var knobImg = knob.gameObject.AddComponent<Image>();
        knobImg.color = Color.white;
        knobImg.raycastTarget = true;
        AttachRoundedRect(knobImg, CircleRadius(knobSize), 3, joystickKnobColor, outlineDark);
        knobImg.pixelsPerUnitMultiplier = 1f;

        // Soft top sheen on the knob.
        var sheen = MakeRect("Sheen", knob, knobSize * 0.8f, knobSize * 0.45f);
        sheen.anchorMin = new Vector2(0.5f, 1f);
        sheen.anchorMax = new Vector2(0.5f, 1f);
        sheen.pivot = new Vector2(0.5f, 1f);
        sheen.anchoredPosition = new Vector2(0f, -knobSize * 0.12f);
        var sheenImg = sheen.gameObject.AddComponent<Image>();
        AttachVerticalGradient(sheenImg, new Color(1f, 1f, 1f, 0.22f), new Color(1f, 1f, 1f, 0f));
        sheenImg.color = Color.white;
        sheenImg.raycastTarget = false;

        // OnScreenStick drives the virtual gamepad left stick.
        // ExactPositionWithDynamicOrigin = "floating/follow" joystick: a press anywhere
        // within dynamicOriginRange (centered on the knob) spawns the stick under the
        // finger and tracks from there, instead of requiring you to grab the small knob.
        var stick = knob.gameObject.AddComponent<OnScreenStick>();
        stick.controlPath = "<Gamepad>/leftStick";
        stick.movementRange = joystickRadius;
        stick.behaviour = OnScreenStick.Behaviour.ExactPositionWithDynamicOrigin;
        stick.dynamicOriginRange = joystickRadius;
    }

    // ─── Generic chunky button ────────────────────────────────────────
    private void BuildButton(string name, string label, float size, Vector2 offset,
        string controlPath, Color tint, TextAnchor anchor, bool isPrimary = false)
    {
        var rt = MakeRect(name, transform, size, size);
        Track(rt.gameObject);
        Anchor(rt, anchor);
        rt.anchoredPosition = offset;

        // Rounded-rect fill (near-circular) with dark chunky border.
        var img = rt.gameObject.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = true;
        AttachRoundedRect(img, CircleRadius(size), isPrimary ? 4 : 3, tint, outlineDark);
        img.pixelsPerUnitMultiplier = 1f;

        var shadow = rt.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.70f);
        shadow.effectDistance = new Vector2(0f, isPrimary ? -4f : -3f);

        // Hover outline (yellow), invisible at rest — animated by UIButtonHoverEffect.
        var outline = rt.gameObject.AddComponent<Outline>();
        Color outlineCol = accentYellow; outlineCol.a = 0f;
        outline.effectColor = outlineCol;
        outline.effectDistance = new Vector2(isPrimary ? 2.5f : 2f, isPrimary ? -2.5f : -2f);

        // Inner radial glow (behind the label), invisible at rest.
        var glow = MakeRect("InnerGlow", rt, size * 1.3f, size * 1.3f);
        SetCenter(glow);
        glow.anchoredPosition = Vector2.zero;
        var glowImg = glow.gameObject.AddComponent<Image>();
        AttachRadialGlow(glowImg, accentYellow);
        glowImg.color = new Color(1f, 1f, 1f, 0f);
        glowImg.raycastTarget = false;
        glow.SetSiblingIndex(0);

        // Top sheen highlight.
        var sheen = MakeRect("Sheen", rt, size * 0.78f, size * 0.42f);
        sheen.anchorMin = new Vector2(0.5f, 1f);
        sheen.anchorMax = new Vector2(0.5f, 1f);
        sheen.pivot = new Vector2(0.5f, 1f);
        sheen.anchoredPosition = new Vector2(0f, -size * 0.10f);
        var sheenImg = sheen.gameObject.AddComponent<Image>();
        AttachVerticalGradient(sheenImg, new Color(1f, 1f, 1f, 0.18f), new Color(1f, 1f, 1f, 0f));
        sheenImg.color = Color.white;
        sheenImg.raycastTarget = false;

        // Bold uppercase TMP label.
        float fontSize = label.Length <= 2 ? size * 0.46f : size * 0.26f;
        CreateLabel(name + "_Label", rt, label, fontSize);

        // Hover/press polish (coexists with OnScreenButton on the same object).
        var hover = rt.gameObject.AddComponent<UIButtonHoverEffect>();
        hover.Bind(outline, glowImg);

        // OnScreenButton simulates the gamepad path — preserves existing input.
        var btn = rt.gameObject.AddComponent<OnScreenButton>();
        btn.controlPath = controlPath;
    }

    // ─── Procedural-sprite helpers (persist in editor via LobbyProceduralSprite) ─
    private static void AttachRoundedRect(Image img, int corner, int border, Color fill, Color borderColor)
    {
        var sp = img.gameObject.GetComponent<LobbyProceduralSprite>();
        if (sp == null) sp = img.gameObject.AddComponent<LobbyProceduralSprite>();
        sp.SetRoundedRect(corner, border, fill, borderColor);
    }

    private static void AttachRadialGlow(Image img, Color center, int size = 256)
    {
        var sp = img.gameObject.GetComponent<LobbyProceduralSprite>();
        if (sp == null) sp = img.gameObject.AddComponent<LobbyProceduralSprite>();
        sp.SetRadialGlow(center, size);
    }

    private static void AttachVerticalGradient(Image img, Color top, Color bottom, int height = 256)
    {
        var sp = img.gameObject.GetComponent<LobbyProceduralSprite>();
        if (sp == null) sp = img.gameObject.AddComponent<LobbyProceduralSprite>();
        sp.SetVerticalGradient(top, bottom, height);
    }

    // ─── UI factory helpers ───────────────────────────────────────────
    private TMP_Text CreateLabel(string name, Transform parent, string text, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = labelColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = fontSize;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;

        var ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.85f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        return tmp;
    }

    private static RectTransform MakeRect(string name, Transform parent, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    private static void Anchor(RectTransform rt, TextAnchor corner)
    {
        Vector2 v = corner switch
        {
            TextAnchor.LowerLeft => new Vector2(0, 0),
            TextAnchor.LowerRight => new Vector2(1, 0),
            TextAnchor.UpperLeft => new Vector2(0, 1),
            TextAnchor.UpperRight => new Vector2(1, 1),
            _ => new Vector2(0.5f, 0.5f)
        };
        rt.anchorMin = v;
        rt.anchorMax = v;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetCenter(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// Corner radius that makes a square rounded-rect render as a near-circle when
    /// drawn Sliced: the corner regions span the full half-size so they meet in the
    /// middle, leaving no flat edge.
    /// </summary>
    private static int CircleRadius(float size) => Mathf.Max(2, Mathf.RoundToInt(size * 0.5f));

    private void Track(GameObject go)
    {
        if (go == null) return;
        _generatedChildren.Add(go);
    }

    /// <summary>
    /// Ensures the scene has an EventSystem with InputSystemUIInputModule.
    /// Removes legacy StandaloneInputModule if present (blocks touch on mobile).
    /// </summary>
    private static void EnsureInputSystemEventSystem()
    {
        var eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            return;
        }

        // Remove legacy module that blocks touch input
        var legacy = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacy != null)
            Destroy(legacy);

        if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }
}
