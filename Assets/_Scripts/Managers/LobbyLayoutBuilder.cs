using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Builds the Fortnite-style lobby layout programmatically at runtime.
/// Layout: Left panel (player list) | Center (3D character preview + arrows) | Right panel (team/mode/ready).
/// Attach this to the same Canvas that holds LobbyUI, or to a root UI object.
/// It creates all UI elements and wires them into LobbyUI and CharacterPreviewUI.
/// </summary>
public class LobbyLayoutBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyUI lobbyUI;
    [SerializeField] private CharacterPreviewUI characterPreview;

    [Header("Prefab")]
    [SerializeField] private GameObject playerEntryPrefab;

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Colors")]
    [SerializeField] private Color panelColor = new Color(0.06f, 0.06f, 0.1f, 0.85f);
    [SerializeField] private Color headerColor = new Color(0.9f, 0.9f, 0.95f);
    [SerializeField] private Color accentColor = new Color(0.35f, 0.75f, 1f);
    [SerializeField] private Color buttonTextColor = Color.white;

    // Built UI references (exposed for LobbyUI wiring)
    private Transform playerListContent;
    private Button rebelsButton, aiButton, noTeamButton;
    private Button ffaButton, tdmButton;
    private Button readyButton;
    private TMP_Text readyButtonText;
    private Image readyButtonImage;
    private TMP_Text statusText;
    private TMP_Text gameModeText;
    private GameObject lobbyContentRoot;

    private void Awake()
    {
        BuildLayout();
        WireReferences();
    }

    private void BuildLayout()
    {
        // Get or create Canvas
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("LobbyCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
            transform.SetParent(canvasObj.transform, false);
        }

        // Content root — the main container that gets shown/hidden
        lobbyContentRoot = CreatePanel("LobbyContent", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, Color.clear);
        var contentRT = lobbyContentRoot.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;

        // Background
        var bgImage = lobbyContentRoot.AddComponent<Image>();
        bgImage.color = new Color(0.04f, 0.04f, 0.06f, 0.95f);

        // === THREE-COLUMN LAYOUT ===
        // Left panel: 25% width
        BuildLeftPanel(lobbyContentRoot.transform);
        // Center panel: 50% width
        BuildCenterPanel(lobbyContentRoot.transform);
        // Right panel: 25% width
        BuildRightPanel(lobbyContentRoot.transform);

        // === STATUS BAR (bottom) ===
        BuildStatusBar(lobbyContentRoot.transform);
    }

    // ─── LEFT PANEL: Player List ──────────────────────────────────────

    private void BuildLeftPanel(Transform parent)
    {
        var panel = CreateAnchoredPanel("LeftPanel", parent,
            new Vector2(0f, 0.06f),    // anchorMin
            new Vector2(0.25f, 1f),    // anchorMax
            panelColor);

        // Header
        CreateText("PlayerListHeader", panel.transform, "PLAYERS", 22, headerColor,
            new Vector2(0f, 0.92f), new Vector2(1f, 1f), TextAlignmentOptions.Center,
            new Vector2(10, 0), new Vector2(-10, -5));

        // Separator line
        CreateSeparator(panel.transform, new Vector2(0f, 0.915f), new Vector2(1f, 0.92f));

        // Scrollable player list
        var scrollArea = CreateAnchoredPanel("PlayerScrollArea", panel.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0.91f), Color.clear);

        var scrollRect = scrollArea.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        var viewport = CreateAnchoredPanel("Viewport", scrollArea.transform,
            Vector2.zero, Vector2.one, Color.clear);
        viewport.AddComponent<RectMask2D>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.offsetMin = new Vector2(5, 0);
        contentRT.offsetMax = new Vector2(-5, 0);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        playerListContent = content.transform;
    }

    // ─── CENTER PANEL: Character Preview ──────────────────────────────

    private void BuildCenterPanel(Transform parent)
    {
        var panel = CreateAnchoredPanel("CenterPanel", parent,
            new Vector2(0.25f, 0.06f),
            new Vector2(0.75f, 1f),
            Color.clear);

        // Character name (top center)
        var charNameText = CreateText("CharacterName", panel.transform, "Select Character", 28, accentColor,
            new Vector2(0.2f, 0.9f), new Vector2(0.8f, 0.98f), TextAlignmentOptions.Center);

        // Character preview RawImage (fills most of center)
        var previewObj = new GameObject("CharacterPreviewImage");
        previewObj.transform.SetParent(panel.transform, false);
        var previewRT = previewObj.AddComponent<RectTransform>();
        previewRT.anchorMin = new Vector2(0.1f, 0.05f);
        previewRT.anchorMax = new Vector2(0.9f, 0.9f);
        previewRT.offsetMin = Vector2.zero;
        previewRT.offsetMax = Vector2.zero;
        var previewImage = previewObj.AddComponent<RawImage>();
        previewImage.color = Color.white;

        // Left arrow button
        var leftArrow = CreateArrowButton("LeftArrow", panel.transform, "<",
            new Vector2(0.02f, 0.4f), new Vector2(0.1f, 0.6f));

        // Right arrow button
        var rightArrow = CreateArrowButton("RightArrow", panel.transform, ">",
            new Vector2(0.9f, 0.4f), new Vector2(0.98f, 0.6f));

        // Wire to CharacterPreviewUI
        if (characterPreview != null)
        {
            characterPreview.SetPreviewImage(previewImage);
            characterPreview.SetArrowButtons(leftArrow, rightArrow);
            characterPreview.SetCharacterNameText(charNameText);
        }
    }

    // ─── RIGHT PANEL: Team, Mode, Ready ───────────────────────────────

    private void BuildRightPanel(Transform parent)
    {
        var panel = CreateAnchoredPanel("RightPanel", parent,
            new Vector2(0.75f, 0.06f),
            new Vector2(1f, 1f),
            panelColor);

        float yTop = 0.95f;
        float sectionSpacing = 0.02f;

        // ─── TEAM SELECTION ───
        CreateText("TeamHeader", panel.transform, "TEAM", 20, headerColor,
            new Vector2(0f, yTop - 0.05f), new Vector2(1f, yTop), TextAlignmentOptions.Center);
        yTop -= 0.06f;
        CreateSeparator(panel.transform, new Vector2(0.05f, yTop - 0.005f), new Vector2(0.95f, yTop));
        yTop -= sectionSpacing;

        float btnH = 0.06f;
        float btnGap = 0.015f;

        rebelsButton = CreateStyledButton("RebelsBtn", panel.transform, "REBELS",
            new Vector2(0.08f, yTop - btnH), new Vector2(0.92f, yTop),
            new Color(0.9f, 0.3f, 0.3f));
        yTop -= btnH + btnGap;

        aiButton = CreateStyledButton("AIBtn", panel.transform, "AI",
            new Vector2(0.08f, yTop - btnH), new Vector2(0.92f, yTop),
            new Color(0.3f, 0.5f, 0.9f));
        yTop -= btnH + btnGap;

        noTeamButton = CreateStyledButton("NoTeamBtn", panel.transform, "NO TEAM",
            new Vector2(0.08f, yTop - btnH), new Vector2(0.92f, yTop),
            new Color(0.45f, 0.45f, 0.5f));
        yTop -= btnH + sectionSpacing * 2;

        // ─── GAME MODE ───
        CreateText("ModeHeader", panel.transform, "GAME MODE", 20, headerColor,
            new Vector2(0f, yTop - 0.05f), new Vector2(1f, yTop), TextAlignmentOptions.Center);
        yTop -= 0.06f;
        CreateSeparator(panel.transform, new Vector2(0.05f, yTop - 0.005f), new Vector2(0.95f, yTop));
        yTop -= sectionSpacing;

        ffaButton = CreateStyledButton("FFABtn", panel.transform, "FREE FOR ALL",
            new Vector2(0.08f, yTop - btnH), new Vector2(0.92f, yTop),
            new Color(0.9f, 0.65f, 0.2f));
        yTop -= btnH + btnGap;

        tdmButton = CreateStyledButton("TDMBtn", panel.transform, "TEAM DEATHMATCH",
            new Vector2(0.08f, yTop - btnH), new Vector2(0.92f, yTop),
            new Color(0.2f, 0.75f, 0.5f));
        yTop -= btnH + sectionSpacing * 2;

        // ─── GAME MODE VOTE TEXT ───
        gameModeText = CreateText("GameModeVote", panel.transform, "", 16, new Color(0.7f, 0.7f, 0.75f),
            new Vector2(0.05f, yTop - 0.05f), new Vector2(0.95f, yTop), TextAlignmentOptions.Center);
        yTop -= 0.06f;

        // ─── READY BUTTON (large, bottom of right panel) ───
        float readyH = 0.08f;
        float readyY = 0.04f;
        var readyObj = CreateStyledButton("ReadyBtn", panel.transform, "Ready Up",
            new Vector2(0.08f, readyY), new Vector2(0.92f, readyY + readyH),
            new Color(0.2f, 0.7f, 0.2f));
        readyButton = readyObj;
        readyButtonText = readyObj.GetComponentInChildren<TMP_Text>();
        readyButtonImage = readyObj.GetComponent<Image>();
    }

    // ─── STATUS BAR (bottom) ─────────────────────────────────────────

    private void BuildStatusBar(Transform parent)
    {
        var bar = CreateAnchoredPanel("StatusBar", parent,
            new Vector2(0f, 0f), new Vector2(1f, 0.055f),
            new Color(0.03f, 0.03f, 0.05f, 0.95f));

        statusText = CreateText("StatusText", bar.transform, "Connecting...", 18, new Color(0.7f, 0.7f, 0.75f),
            new Vector2(0f, 0f), new Vector2(1f, 1f), TextAlignmentOptions.Center,
            new Vector2(20, 0), new Vector2(-20, 0));
    }

    // ─── WIRING ──────────────────────────────────────────────────────

    private void WireReferences()
    {
        if (lobbyUI == null) return;

        // Use reflection-free approach: expose a setup method on LobbyUI
        lobbyUI.SetupLayoutReferences(
            playerListContent: playerListContent,
            playerEntryPrefab: playerEntryPrefab,
            rebelsButton: rebelsButton,
            aiButton: aiButton,
            noTeamButton: noTeamButton,
            ffaButton: ffaButton,
            tdmButton: tdmButton,
            characterPreview: characterPreview,
            readyButton: readyButton,
            readyButtonText: readyButtonText,
            readyButtonImage: readyButtonImage,
            statusText: statusText,
            gameModeText: gameModeText,
            lobbyContentRoot: lobbyContentRoot
        );
    }

    // ─── UI Factory Helpers ──────────────────────────────────────────

    private GameObject CreatePanel(string name, Transform parent, Vector2 anchoredPos, Vector2 sizeDelta,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        if (color.a > 0)
        {
            var img = obj.AddComponent<Image>();
            img.color = color;
        }
        return obj;
    }

    private GameObject CreateAnchoredPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        if (color.a > 0)
        {
            var img = obj.AddComponent<Image>();
            img.color = color;
        }
        return obj;
    }

    private TMP_Text CreateText(string name, Transform parent, string text, int fontSize, Color color,
        Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions alignment = TextAlignmentOptions.Center,
        Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin ?? Vector2.zero;
        rt.offsetMax = offsetMax ?? Vector2.zero;

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = FontStyles.Bold;
        if (font != null) tmp.font = font;
        return tmp;
    }

    private void CreateSeparator(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var obj = new GameObject("Separator");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = obj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.4f, 0.5f);
    }

    private Button CreateStyledButton(string name, Transform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = obj.AddComponent<Image>();
        img.color = bgColor;
        img.type = Image.Type.Sliced;

        var btn = obj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.15f;
        colors.pressedColor = bgColor * 0.75f;
        colors.selectedColor = bgColor;
        btn.colors = colors;

        // Button label
        var textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform, false);
        var textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(5, 2);
        textRT.offsetMax = new Vector2(-5, -2);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.color = buttonTextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        if (font != null) tmp.font = font;

        return btn;
    }

    private Button CreateArrowButton(string name, Transform parent, string arrow,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = obj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

        var btn = obj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.35f, 0.9f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        btn.colors = colors;

        var textObj = new GameObject("Arrow");
        textObj.transform.SetParent(obj.transform, false);
        var textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = arrow;
        tmp.fontSize = 48;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        if (font != null) tmp.font = font;

        return btn;
    }
}
