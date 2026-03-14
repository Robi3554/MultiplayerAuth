using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

/// <summary>
/// Builds a modern mobile controls overlay at runtime.
/// All action buttons use OnScreenButton to simulate gamepad inputs,
/// which flow through the existing PlayerInput/InputSystem bindings.
/// </summary>
public class MobileControlsCanvas : MonoBehaviour
{
    [Header("Joystick")]
    [SerializeField] private float joystickRadius = 110f;

    [Header("Button Sizes")]
    [SerializeField] private float primaryBtnSize = 130f;   // Attack
    [SerializeField] private float secondaryBtnSize = 90f;  // Jump, Reload, Dash
    [SerializeField] private float weaponArrowSize = 65f;
    [SerializeField] private float topBarBtnSize = 55f;     // Pause, Scoreboard

    [Header("Appearance")]
    [SerializeField] private Color primaryColor   = new Color(0.90f, 0.25f, 0.20f, 0.70f);  // red-ish attack
    [SerializeField] private Color secondaryColor = new Color(0.20f, 0.55f, 0.85f, 0.55f);  // blue action buttons
    [SerializeField] private Color weaponColor    = new Color(0.95f, 0.75f, 0.15f, 0.55f);  // gold weapon arrows
    [SerializeField] private Color topBarColor    = new Color(0.15f, 0.15f, 0.15f, 0.60f);  // dark top-bar buttons
    [SerializeField] private Color joystickBg     = new Color(1f, 1f, 1f, 0.10f);
    [SerializeField] private Color joystickKnob   = new Color(1f, 1f, 1f, 0.50f);
    [SerializeField] private Color labelColor     = Color.white;

    private void Awake()
    {
        Build();
    }

    // ───────────────────────────────────────────────────────────
    private void Build()
    {
        // Canvas
        var canvas  = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        EnsureInputSystemEventSystem();

        // ── Left : Joystick ──
        BuildJoystick();

        // ── Right : Action cluster ──
        //    Attack (large, center-right), others arranged around it
        BuildOnScreenButton("AttackBtn",  "\u2022",  primaryBtnSize,
            new Vector2(-170, 200), "<Gamepad>/rightShoulder",
            primaryColor, TextAnchor.LowerRight);

        BuildOnScreenButton("JumpBtn",   "JMP",  secondaryBtnSize,
            new Vector2(-300, 130),  "<Gamepad>/buttonSouth",
            secondaryColor, TextAnchor.LowerRight);

        BuildOnScreenButton("ReloadBtn", "RLD",  secondaryBtnSize,
            new Vector2(-300, 260), "<Gamepad>/buttonNorth",
            secondaryColor, TextAnchor.LowerRight);

        BuildOnScreenButton("DashBtn",   "DSH",  secondaryBtnSize,
            new Vector2(-80, 95),   "<Gamepad>/rightStickPress",
            secondaryColor, TextAnchor.LowerRight);

        // ── Weapon prev / next (top-right, below top bar) ──
        BuildOnScreenButton("WpnPrev", "\u25C0", weaponArrowSize,
            new Vector2(-150, -90), "<Gamepad>/dpad/left",
            weaponColor, TextAnchor.UpperRight);

        BuildOnScreenButton("WpnNext", "\u25B6", weaponArrowSize,
            new Vector2(-60, -90),  "<Gamepad>/dpad/right",
            weaponColor, TextAnchor.UpperRight);

        // ── Top bar : Pause + Scoreboard ──
        BuildOnScreenButton("PauseBtn", "\u2759\u2759", topBarBtnSize,
            new Vector2(-20, -20), "<Gamepad>/start",
            topBarColor, TextAnchor.UpperRight);

        BuildOnScreenButton("ScoreBtn", "SCR", topBarBtnSize,
            new Vector2(-85, -20), "<Gamepad>/select",
            topBarColor, TextAnchor.UpperRight);
    }

    // ─────────────────── Joystick ────────────────────────────
    private void BuildJoystick()
    {
        float diameter = joystickRadius * 2f;

        // Outer ring (background)
        var bg = MakeRect("JoystickBG", transform, diameter, diameter);
        Anchor(bg, TextAnchor.LowerLeft);
        bg.anchoredPosition = new Vector2(190, 190);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.color = joystickBg;
        bgImg.raycastTarget = true;
        MakeCircle(bgImg);

        // Inner knob
        float knobSize = joystickRadius * 0.75f;
        var knob = MakeRect("JoystickKnob", bg, knobSize, knobSize);
        knob.anchoredPosition = Vector2.zero;
        var knobImg = knob.gameObject.AddComponent<Image>();
        knobImg.color = joystickKnob;
        knobImg.raycastTarget = true;
        MakeCircle(knobImg);

        // OnScreenStick on the knob
        var stick = knob.gameObject.AddComponent<OnScreenStick>();
        stick.controlPath = "<Gamepad>/leftStick";
        stick.movementRange = joystickRadius;
    }

    // ─────────────────── Generic OnScreen Button ──────────────
    private void BuildOnScreenButton(string name, string label, float size,
        Vector2 offset, string controlPath, Color color, TextAnchor anchor)
    {
        var rt = MakeRect(name, transform, size, size);
        Anchor(rt, anchor);
        rt.anchoredPosition = offset;

        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        MakeCircle(img);

        // Outline for depth
        var outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.35f);
        outline.effectDistance = new Vector2(2, -2);

        // Label
        var textGO = new GameObject("Label");
        textGO.transform.SetParent(rt, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var txt = textGO.AddComponent<Text>();
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = Mathf.Max(16, (int)(size * 0.28f));
        txt.fontStyle = FontStyle.Bold;
        txt.color = labelColor;
        txt.raycastTarget = false;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Shadow on text
        var shadow = textGO.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(1, -1);

        // OnScreenButton simulates the gamepad path
        var btn = rt.gameObject.AddComponent<OnScreenButton>();
        btn.controlPath = controlPath;
    }

    // ─────────────────── Helpers ─────────────────────────────
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
            TextAnchor.LowerLeft  => new Vector2(0, 0),
            TextAnchor.LowerRight => new Vector2(1, 0),
            TextAnchor.UpperLeft  => new Vector2(0, 1),
            TextAnchor.UpperRight => new Vector2(1, 1),
            _ => new Vector2(0.5f, 0.5f)
        };
        rt.anchorMin = v;
        rt.anchorMax = v;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// Makes an Image appear circular by enabling a procedural sprite mask.
    /// Uses Unity's built-in Knob sprite which is a filled circle.
    /// </summary>
    private static void MakeCircle(Image img)
    {
        img.sprite = Resources.Load<Sprite>("UI/Skin/Knob");
        if (img.sprite != null)
            img.type = Image.Type.Simple;
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
        var legacy = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (legacy != null)
            Destroy(legacy);

        if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }
}
