using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

/// <summary>
/// Programmatically creates the mobile controls UI at runtime:
/// - Virtual joystick (left)        → simulates <Gamepad>/leftStick  → Move action
/// - Attack button (right, large)   → simulates <Gamepad>/rightShoulder → Damage action
/// - Jump button                    → simulates <Gamepad>/buttonSouth   → Jump action
/// - Reload button                  → simulates <Gamepad>/buttonNorth   → Reload action
/// - Dash button                    → simulates <Gamepad>/rightStickPress → Sprint/Dash action
/// - Weapon prev / next arrows      → simulates dpad left/right         → ChangeWeaponSlot action
/// - Pause button (top-right)       → calls PauseMenuManager.TogglePause()
///
/// Attach this MonoBehaviour to an empty GameObject in the game scene.
/// The canvas is hidden by default; MobileInputManager activates it on mobile.
/// </summary>
public class MobileControlsCanvas : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PauseMenuManager pauseMenuManager;

    [Header("Joystick Settings")]
    [SerializeField] private float joystickRadius = 100f;

    [Header("Button Sizes")]
    [SerializeField] private float attackButtonSize = 120f;
    [SerializeField] private float actionButtonSize = 80f;
    [SerializeField] private float weaponArrowSize = 60f;
    [SerializeField] private float pauseButtonSize = 50f;

    [Header("Colors")]
    [SerializeField] private Color buttonColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] private Color joystickBgColor = new Color(0f, 0f, 0f, 0.25f);
    [SerializeField] private Color joystickHandleColor = new Color(1f, 1f, 1f, 0.6f);

    private Canvas _canvas;

    private void Awake()
    {
        BuildCanvas();
    }

    private void BuildCanvas()
    {
        // ── Canvas ──
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // Ensure an EventSystem exists
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ── Virtual Joystick (bottom-left) ──
        BuildJoystick();

        // ── Action Buttons (bottom-right) ──
        BuildActionButton("AttackBtn", "ATK", attackButtonSize,
            new Vector2(-160, 140), "<Gamepad>/rightShoulder");

        BuildActionButton("JumpBtn", "JMP", actionButtonSize,
            new Vector2(-280, 80), "<Gamepad>/buttonSouth");

        BuildActionButton("ReloadBtn", "RLD", actionButtonSize,
            new Vector2(-280, 200), "<Gamepad>/buttonNorth");

        BuildActionButton("DashBtn", "DSH", actionButtonSize,
            new Vector2(-100, 40), "<Gamepad>/rightStickPress");

        // ── Weapon Prev / Next (top-right) ──
        BuildActionButton("WeaponPrev", "◀", weaponArrowSize,
            new Vector2(-140, -80), "<Gamepad>/dpad/left",
            TextAnchor.UpperRight);

        BuildActionButton("WeaponNext", "▶", weaponArrowSize,
            new Vector2(-60, -80), "<Gamepad>/dpad/right",
            TextAnchor.UpperRight);

        // ── Pause Button (top-right corner) ──
        BuildPauseButton();
    }

    // ─────────────────────── Joystick ───────────────────────

    private void BuildJoystick()
    {
        // Background circle
        var bg = CreateUIElement("JoystickBG", transform, joystickRadius * 2f, joystickRadius * 2f);
        SetAnchor(bg, TextAnchor.LowerLeft);
        bg.anchoredPosition = new Vector2(180, 180);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.color = joystickBgColor;
        bgImg.raycastTarget = true;

        // Handle (draggable knob)
        var handle = CreateUIElement("JoystickHandle", bg, joystickRadius * 0.8f, joystickRadius * 0.8f);
        handle.anchoredPosition = Vector2.zero;
        var handleImg = handle.gameObject.AddComponent<Image>();
        handleImg.color = joystickHandleColor;
        handleImg.raycastTarget = true;

        // OnScreenStick on the handle — simulates left stick for movement
        var stick = handle.gameObject.AddComponent<OnScreenStick>();
        stick.controlPath = "<Gamepad>/leftStick";
        stick.movementRange = joystickRadius;
    }

    // ─────────────────────── Action Button ───────────────────────

    private void BuildActionButton(string name, string label, float size,
        Vector2 offset, string controlPath, TextAnchor anchor = TextAnchor.LowerRight)
    {
        var rt = CreateUIElement(name, transform, size, size);
        SetAnchor(rt, anchor);
        rt.anchoredPosition = offset;

        var img = rt.gameObject.AddComponent<Image>();
        img.color = buttonColor;
        img.raycastTarget = true;

        // Label
        var textGO = new GameObject("Label");
        textGO.transform.SetParent(rt, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = (int)(size * 0.3f);
        text.color = Color.white;
        text.raycastTarget = false;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // OnScreenButton — simulates the specified gamepad control
        var btn = rt.gameObject.AddComponent<OnScreenButton>();
        btn.controlPath = controlPath;
    }

    // ─────────────────────── Pause Button ───────────────────────

    private void BuildPauseButton()
    {
        var rt = CreateUIElement("PauseBtn", transform, pauseButtonSize, pauseButtonSize);
        SetAnchor(rt, TextAnchor.UpperRight);
        rt.anchoredPosition = new Vector2(-20, -20);

        var img = rt.gameObject.AddComponent<Image>();
        img.color = buttonColor;
        img.raycastTarget = true;

        // Label
        var textGO = new GameObject("Label");
        textGO.transform.SetParent(rt, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var text = textGO.AddComponent<Text>();
        text.text = "| |";
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 22;
        text.color = Color.white;
        text.raycastTarget = false;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Unity Button component — wired to PauseMenuManager.TogglePause()
        var button = rt.gameObject.AddComponent<Button>();
        if (pauseMenuManager != null)
            button.onClick.AddListener(pauseMenuManager.TogglePause);
    }

    // ─────────────────────── Helpers ───────────────────────

    private static RectTransform CreateUIElement(string name, Transform parent, float width, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
        return rt;
    }

    private static void SetAnchor(RectTransform rt, TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.LowerLeft:
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                break;
            case TextAnchor.LowerRight:
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                break;
            case TextAnchor.UpperRight:
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 0.5f);
                break;
            case TextAnchor.UpperLeft:
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0.5f, 0.5f);
                break;
        }
    }
}
