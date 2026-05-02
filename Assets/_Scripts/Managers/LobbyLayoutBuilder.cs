using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a vibrant, game-y lobby layout procedurally.
///
/// Style direction is Fortnite-ish: saturated solid color blocks, chunky borders,
/// thick yellow accent strips, big bold uppercase typography, flat panels with
/// strong contrast — not the more subtle realistic/tech look.
///
/// All proportions and colors are exposed as serialized fields so they can be
/// tweaked from the Inspector. The build runs once in <see cref="Awake"/> by
/// default, and can be re-run on demand via the right-click context menu:
///   - "Rebuild Layout" — destroys the previously generated children under this
///     transform and rebuilds from the current inspector values. Works in edit
///     mode (recommended) and at runtime.
///   - "Clear Built UI" — removes everything this component generated under its
///     transform, leaving the canvas ready for a manual hierarchy or a fresh
///     rebuild.
///
/// Tip: to switch to a hand-edited canvas, run "Rebuild Layout" once in edit
/// mode, then disable this component. The generated GameObjects persist in the
/// scene and become normal hand-editable UI elements. After that you'll want to
/// wire the Lobby UI references on <see cref="LobbyUI"/> manually via the
/// Inspector since this builder will no longer call <see cref="LobbyUI.SetupLayoutReferences"/>.
/// </summary>
public class LobbyLayoutBuilder : MonoBehaviour
{
    // ─── Serialized inspector fields ───────────────────────────────────

    [Header("References")]
    [SerializeField] private LobbyUI lobbyUI;
    [SerializeField] private CharacterPreviewUI characterPreview;

    [Header("Prefab")]
    [SerializeField] private GameObject playerEntryPrefab;

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Build Behavior")]
    [Tooltip("If true, the layout is (re)built in Awake. Disable to preserve a manually-baked / hand-edited hierarchy.")]
    [SerializeField] private bool rebuildOnAwake = true;

    [Header("Title")]
    [SerializeField] private string lobbyTitle = "MULTIPLAYER LOBBY";
    [SerializeField, Range(20, 80)] private int titleFontSize = 38;
    [SerializeField, Range(0f, 20f)] private float titleCharacterSpacing = 8f;

    [Header("Layout — Outer (anchors, 0..1 of canvas)")]
    [Tooltip("Outer left/right margin from the canvas edge.")]
    [SerializeField, Range(0f, 0.10f)] private float panelMargin = 0.013f;

    [Tooltip("Horizontal gap between the side columns and the center panel.")]
    [SerializeField, Range(0f, 0.05f)] private float columnGap = 0.010f;

    [Tooltip("Width of the left players panel as a fraction of the canvas.")]
    [SerializeField, Range(0.10f, 0.45f)] private float leftColumnWidth = 0.232f;

    [Tooltip("Width of the right loadout panel as a fraction of the canvas.")]
    [SerializeField, Range(0.10f, 0.45f)] private float rightColumnWidth = 0.232f;

    [Tooltip("Top edge of the side panels (below the title).")]
    [SerializeField, Range(0.5f, 0.99f)] private float contentTop = 0.93f;

    [Tooltip("Bottom edge of the side panels (above the status bar).")]
    [SerializeField, Range(0.01f, 0.30f)] private float contentBottom = 0.075f;

    [Tooltip("Status bar bottom anchor (gap to canvas bottom).")]
    [SerializeField, Range(0f, 0.05f)] private float statusBarBottom = 0.012f;

    [Tooltip("Status bar top anchor (sets its height = top - bottom).")]
    [SerializeField, Range(0.02f, 0.15f)] private float statusBarTop = 0.062f;

    [Tooltip("Bottom anchor of the title text area.")]
    [SerializeField, Range(0.85f, 0.99f)] private float titleBarBottom = 0.945f;

    [Tooltip("Top anchor of the title text area (and bottom of the top yellow stripe).")]
    [SerializeField, Range(0.90f, 1f)] private float titleBarTop = 0.985f;

    [Header("Layout — Right Panel Internals")]
    [SerializeField, Range(0.02f, 0.12f)] private float panelHeader_Top = 0.985f;
    [SerializeField, Range(0.02f, 0.12f)] private float panelHeader_Bottom = 0.92f;
    [SerializeField, Range(0.02f, 0.20f)] private float buttonHeight = 0.075f;
    [SerializeField, Range(0f, 0.05f)] private float buttonGap = 0.014f;
    [SerializeField, Range(0f, 0.06f)] private float sectionGap = 0.018f;
    [SerializeField, Range(0.02f, 0.06f)] private float sectionHeaderHeight = 0.045f;
    [SerializeField, Range(0.05f, 0.25f)] private float readyButtonHeight = 0.11f;
    [SerializeField, Range(0f, 0.20f)] private float readyButtonBottom = 0.035f;

    [Header("Palette — Base")]
    [SerializeField] private Color bgPrimary = new(0.10f, 0.16f, 0.40f, 1f);
    [SerializeField] private Color bgSecondary = new(0.14f, 0.22f, 0.55f, 1f);
    [SerializeField] private Color panelFill = new(0.13f, 0.21f, 0.52f, 1f);
    [SerializeField] private Color panelBottom = new(0.05f, 0.09f, 0.25f, 1f);
    [SerializeField] private Color accentYellow = new(1.00f, 0.82f, 0.18f, 1f);
    [SerializeField] private Color outlineDark = new(0.02f, 0.05f, 0.14f, 1f);
    [SerializeField] private Color textWhite = new(0.99f, 1.00f, 1.00f, 1f);
    [SerializeField] private Color textDim = new(0.78f, 0.85f, 1.00f, 1f);

    [Header("Palette — Team / Mode")]
    [SerializeField] private Color rebelsColor = new(0.95f, 0.30f, 0.34f, 1f);
    [SerializeField] private Color aiColor = new(0.30f, 0.55f, 1.00f, 1f);
    [SerializeField] private Color noneTeamColor = new(0.55f, 0.58f, 0.70f, 1f);
    [SerializeField] private Color ffaColor = new(1.00f, 0.65f, 0.18f, 1f);
    [SerializeField] private Color tdmColor = new(0.32f, 0.86f, 0.55f, 1f);
    [SerializeField] private Color readyColor = new(1.00f, 0.82f, 0.18f, 1f);

    // ─── Built UI references (exposed for LobbyUI wiring) ──────────────

    private Transform playerListContent;
    private Button rebelsButton, aiButton, noTeamButton;
    private Button ffaButton, tdmButton;
    private Button readyButton;
    private TMP_Text readyButtonText;
    private Image readyButtonImage;
    private TMP_Text statusText;
    private TMP_Text gameModeText;
    private GameObject lobbyContentRoot;
    private TMP_Text characterTaglineText;

    // Tracks the children we created so Clear() can remove them without touching
    // anything the user added manually under this transform.
    private readonly List<GameObject> _generatedChildren = new();

    private void Awake()
    {
        if (!rebuildOnAwake) return;
        BuildLayout();
        WireReferences();
    }

    // ─── Editor-friendly entry points (right-click on this component) ──

    [ContextMenu("Rebuild Layout")]
    public void Rebuild()
    {
        Clear();
        BuildLayout();
        WireReferences();
    }

    [ContextMenu("Clear Built UI")]
    public void Clear()
    {
        // Tear down everything this builder added. We track our own children so
        // we don't accidentally delete other things the user parented under this
        // GameObject.
        for (int i = _generatedChildren.Count - 1; i >= 0; i--)
        {
            var go = _generatedChildren[i];
            if (go == null) continue;
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }
        _generatedChildren.Clear();

        // Belt + braces: also clear loose top-level children whose names match our
        // generated objects (in case _generatedChildren was wiped by a domain reload).
        var names = new HashSet<string> { "BG_Base", "BG_Pattern", "BG_TopStripe", "BG_TopStripeShadow", "LobbyContent" };
        var toDelete = new List<GameObject>();
        foreach (Transform child in transform)
            if (names.Contains(child.name))
                toDelete.Add(child.gameObject);
        foreach (var go in toDelete)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }

    // ─── Top-level layout ──────────────────────────────────────────────

    private void BuildLayout()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("LobbyCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            transform.SetParent(canvasObj.transform, false);
        }

        BuildBackground(transform);

        lobbyContentRoot = CreateAnchoredPanel("LobbyContent", transform, Vector2.zero, Vector2.one, Color.clear);
        Track(lobbyContentRoot);

        BuildLeftPanel(lobbyContentRoot.transform);
        BuildCenterPanel(lobbyContentRoot.transform);
        BuildRightPanel(lobbyContentRoot.transform);
        BuildStatusBar(lobbyContentRoot.transform);
        BuildTopTitle(lobbyContentRoot.transform);
    }

    private void BuildBackground(Transform parent)
    {
        // Solid saturated base: vertical step from BgPrimary (top) to BgSecondary (bottom).
        var bg = CreateAnchoredPanel("BG_Base", parent, Vector2.zero, Vector2.one, Color.white);
        Track(bg);
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = LobbyVisuals.GetVerticalGradient(bgPrimary, bgSecondary);
        bgImg.type = Image.Type.Simple;
        bgImg.raycastTarget = false;

        // Subtle diagonal scan-line pattern at low alpha
        var pattern = CreateAnchoredPanel("BG_Pattern", parent, Vector2.zero, Vector2.one, Color.white);
        Track(pattern);
        var patImg = pattern.GetComponent<Image>();
        patImg.sprite = LobbyVisuals.GetSubtlePattern(new Color(1f, 1f, 1f, 1f));
        patImg.type = Image.Type.Tiled;
        patImg.color = new Color(1f, 1f, 1f, 0.07f);
        patImg.raycastTarget = false;

        // Thick yellow accent stripe along the very top of the screen
        var topStripe = CreateAnchoredPanel("BG_TopStripe", parent,
            new Vector2(0f, titleBarTop), new Vector2(1f, 1f), accentYellow);
        Track(topStripe);
        topStripe.GetComponent<Image>().raycastTarget = false;

        // Hard shadow under the stripe
        var topShadow = CreateAnchoredPanel("BG_TopStripeShadow", parent,
            new Vector2(0f, titleBarTop - 0.007f), new Vector2(1f, titleBarTop), new Color(0f, 0f, 0f, 0.55f));
        Track(topShadow);
        topShadow.GetComponent<Image>().raycastTarget = false;
    }

    private void BuildTopTitle(Transform parent)
    {
        var bar = CreateAnchoredPanel("TitleBar", parent,
            new Vector2(0f, titleBarBottom), new Vector2(1f, titleBarTop), Color.clear);

        var title = CreateText("LobbyTitle", bar.transform, lobbyTitle, titleFontSize, accentYellow,
            new Vector2(0f, 0f), new Vector2(1f, 1f), TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        title.characterSpacing = titleCharacterSpacing;

        var titleOutline = title.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = outlineDark;
        titleOutline.effectDistance = new Vector2(2.2f, -2.2f);
        var titleShadow = title.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        titleShadow.effectDistance = new Vector2(0f, -3f);
    }

    // ─── LEFT PANEL ───────────────────────────────────────────────────

    private void BuildLeftPanel(Transform parent)
    {
        var panel = CreateChunkyPanel("LeftPanel", parent,
            new Vector2(panelMargin, contentBottom),
            new Vector2(panelMargin + leftColumnWidth, contentTop),
            panelFill, panelBottom);

        var headerStrip = CreateAnchoredPanel("HeaderStripe", panel.transform,
            new Vector2(0f, panelHeader_Bottom), new Vector2(1f, panelHeader_Top), accentYellow);
        headerStrip.GetComponent<Image>().raycastTarget = false;

        var header = CreateText("PlayerListHeader", panel.transform, "PLAYERS", 24, outlineDark,
            new Vector2(0f, panelHeader_Bottom), new Vector2(1f, panelHeader_Top), TextAlignmentOptions.Center,
            new Vector2(10, 0), new Vector2(-10, 0));
        header.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        header.characterSpacing = 10f;

        // Scrollable list area
        var scrollArea = CreateAnchoredPanel("PlayerScrollArea", panel.transform,
            new Vector2(0.025f, 0.04f), new Vector2(0.975f, panelHeader_Bottom - 0.015f), Color.clear);

        var scrollRect = scrollArea.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        var viewport = CreateAnchoredPanel("Viewport", scrollArea.transform, Vector2.zero, Vector2.one, Color.clear);
        viewport.AddComponent<RectMask2D>();

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRT = (RectTransform)content.transform;
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.offsetMin = new Vector2(6, 0);
        contentRT.offsetMax = new Vector2(-6, 0);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(4, 4, 8, 8);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;
        scrollRect.viewport = (RectTransform)viewport.transform;

        playerListContent = content.transform;
    }

    // ─── CENTER PANEL ─────────────────────────────────────────────────

    private void BuildCenterPanel(Transform parent)
    {
        // Center panel spans the gap between the two side columns.
        float centerLeft = panelMargin + leftColumnWidth + columnGap;
        float centerRight = 1f - panelMargin - rightColumnWidth - columnGap;

        var panel = CreateAnchoredPanel("CenterPanel", parent,
            new Vector2(centerLeft, contentBottom),
            new Vector2(centerRight, contentTop),
            Color.clear);

        var glow = CreateAnchoredPanel("PreviewGlow", panel.transform,
            new Vector2(-0.10f, -0.05f), new Vector2(1.10f, 0.92f), Color.white);
        var glowImg = glow.GetComponent<Image>();
        glowImg.sprite = LobbyVisuals.GetRadialGlow(accentYellow);
        glowImg.color = new Color(1f, 1f, 1f, 0.55f);
        glowImg.raycastTarget = false;

        var previewObj = new GameObject("CharacterPreviewImage", typeof(RectTransform));
        previewObj.transform.SetParent(panel.transform, false);
        var previewRT = (RectTransform)previewObj.transform;
        previewRT.anchorMin = new Vector2(0.14f, 0.10f);
        previewRT.anchorMax = new Vector2(0.86f, 0.86f);
        previewRT.offsetMin = Vector2.zero;
        previewRT.offsetMax = Vector2.zero;
        var previewImage = previewObj.AddComponent<RawImage>();
        previewImage.color = Color.white;
        previewImage.raycastTarget = false;

        var stageStripe = CreateAnchoredPanel("StageStripe", panel.transform,
            new Vector2(0.10f, 0.085f), new Vector2(0.90f, 0.110f), accentYellow);
        stageStripe.GetComponent<Image>().raycastTarget = false;
        var stageShadow = CreateAnchoredPanel("StageStripeShadow", panel.transform,
            new Vector2(0.10f, 0.075f), new Vector2(0.90f, 0.088f), new Color(0f, 0f, 0f, 0.45f));
        stageShadow.GetComponent<Image>().raycastTarget = false;

        var charNameText = CreateText("CharacterName", panel.transform, "Select Character", 48, accentYellow,
            new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.965f), TextAlignmentOptions.Center);
        charNameText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        charNameText.characterSpacing = 6f;
        var nameOutline = charNameText.gameObject.AddComponent<Outline>();
        nameOutline.effectColor = outlineDark;
        nameOutline.effectDistance = new Vector2(2.5f, -2.5f);
        var nameShadow = charNameText.gameObject.AddComponent<Shadow>();
        nameShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        nameShadow.effectDistance = new Vector2(0f, -4f);

        characterTaglineText = CreateText("CharacterTagline", panel.transform, "", 18, textDim,
            new Vector2(0.10f, 0.835f), new Vector2(0.90f, 0.880f), TextAlignmentOptions.Center);
        characterTaglineText.fontStyle = FontStyles.Italic;

        var leftArrow = CreateCircularArrowButton("LeftArrow", panel.transform, "<",
            new Vector2(0.00f, 0.36f), new Vector2(0.10f, 0.58f));
        var rightArrow = CreateCircularArrowButton("RightArrow", panel.transform, ">",
            new Vector2(0.90f, 0.36f), new Vector2(1.00f, 0.58f));

        if (characterPreview != null)
        {
            characterPreview.SetPreviewImage(previewImage);
            characterPreview.SetArrowButtons(leftArrow, rightArrow);
            characterPreview.SetCharacterNameText(charNameText);
            characterPreview.SetCharacterTaglineText(characterTaglineText);

            // Bake-friendly retinting: assign serialized refs the preview reads in
            // ShowCharacter, instead of subscribing a runtime-only C# event lambda.
            characterPreview.SetAccentGlow(glowImg);
            characterPreview.SetAccentNameOutline(nameOutline);
        }
    }

    // ─── RIGHT PANEL ──────────────────────────────────────────────────

    private void BuildRightPanel(Transform parent)
    {
        var panel = CreateChunkyPanel("RightPanel", parent,
            new Vector2(1f - panelMargin - rightColumnWidth, contentBottom),
            new Vector2(1f - panelMargin, contentTop),
            panelFill, panelBottom);

        var headerStrip = CreateAnchoredPanel("HeaderStripe", panel.transform,
            new Vector2(0f, panelHeader_Bottom), new Vector2(1f, panelHeader_Top), accentYellow);
        headerStrip.GetComponent<Image>().raycastTarget = false;
        var loadout = CreateText("LoadoutHeader", panel.transform, "LOADOUT", 24, outlineDark,
            new Vector2(0f, panelHeader_Bottom), new Vector2(1f, panelHeader_Top), TextAlignmentOptions.Center);
        loadout.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        loadout.characterSpacing = 12f;

        // Section layout starts just under the header strip
        float yTop = panelHeader_Bottom - 0.01f;

        // ─── TEAM ───
        CreateSectionHeader(panel.transform, "TEAM", yTop - sectionHeaderHeight, yTop);
        yTop -= sectionHeaderHeight + sectionGap;

        rebelsButton = CreateLobbyButton("RebelsBtn", panel.transform, "REBELS", "✦",
            new Vector2(0.07f, yTop - buttonHeight), new Vector2(0.93f, yTop), rebelsColor);
        yTop -= buttonHeight + buttonGap;

        aiButton = CreateLobbyButton("AIBtn", panel.transform, "A.I.", "◆",
            new Vector2(0.07f, yTop - buttonHeight), new Vector2(0.93f, yTop), aiColor);
        yTop -= buttonHeight + buttonGap;

        noTeamButton = CreateLobbyButton("NoTeamBtn", panel.transform, "NO TEAM", "—",
            new Vector2(0.07f, yTop - buttonHeight), new Vector2(0.93f, yTop), noneTeamColor);
        yTop -= buttonHeight + sectionGap * 2;

        // ─── GAME MODE ───
        CreateSectionHeader(panel.transform, "MODE", yTop - sectionHeaderHeight, yTop);
        yTop -= sectionHeaderHeight + sectionGap;

        ffaButton = CreateLobbyButton("FFABtn", panel.transform, "FREE FOR ALL", "●",
            new Vector2(0.07f, yTop - buttonHeight), new Vector2(0.93f, yTop), ffaColor);
        yTop -= buttonHeight + buttonGap;

        tdmButton = CreateLobbyButton("TDMBtn", panel.transform, "TEAM DEATHMATCH", "▲",
            new Vector2(0.07f, yTop - buttonHeight), new Vector2(0.93f, yTop), tdmColor);
        yTop -= buttonHeight + sectionGap;

        gameModeText = CreateText("GameModeVote", panel.transform, "", 14, textDim,
            new Vector2(0.06f, yTop - 0.05f), new Vector2(0.94f, yTop), TextAlignmentOptions.Center);
        gameModeText.fontStyle = FontStyles.Italic;

        // ─── READY (huge yellow CTA at the bottom) ───
        readyButton = CreateLobbyButton("ReadyBtn", panel.transform, "READY UP", "✓",
            new Vector2(0.06f, readyButtonBottom), new Vector2(0.94f, readyButtonBottom + readyButtonHeight),
            readyColor, big: true);
        readyButtonText = readyButton.GetComponentInChildren<TMP_Text>();
        readyButtonImage = readyButton.GetComponent<Image>();

        var readyHover = readyButton.GetComponent<UIButtonHoverEffect>();
        if (readyHover != null)
            readyHover.EnableSelectedPulse(speed: 2.6f, amplitude: 0.45f);
    }

    private void CreateSectionHeader(Transform parent, string text, float yMin, float yMax)
    {
        var header = CreateText($"Header_{text}", parent, text, 18, accentYellow,
            new Vector2(0f, yMin), new Vector2(1f, yMax), TextAlignmentOptions.MidlineLeft,
            new Vector2(20, 0), new Vector2(-20, 0));
        header.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        header.characterSpacing = 10f;

        var bar = CreateAnchoredPanel($"HeaderBar_{text}", parent,
            new Vector2(0.07f, yMin - 0.004f), new Vector2(0.93f, yMin + 0.001f), accentYellow);
        bar.GetComponent<Image>().raycastTarget = false;
    }

    // ─── STATUS BAR ────────────────────────────────────────────────────

    private void BuildStatusBar(Transform parent)
    {
        var bar = CreateChunkyPanel("StatusBar", parent,
            new Vector2(panelMargin, statusBarBottom), new Vector2(1f - panelMargin, statusBarTop),
            new Color(0.07f, 0.11f, 0.30f, 1f), new Color(0.03f, 0.05f, 0.16f, 1f));

        statusText = CreateText("StatusText", bar.transform, "Connecting...", 18, textWhite,
            new Vector2(0f, 0f), new Vector2(0.55f, 1f), TextAlignmentOptions.MidlineLeft,
            new Vector2(28, 0), new Vector2(-12, 0));
        statusText.fontStyle = FontStyles.Bold;

        var tip = CreateText("StatusTip", bar.transform, "PICK A TEAM, GAME MODE, AND READY UP", 14, accentYellow,
            new Vector2(0.55f, 0f), new Vector2(1f, 1f), TextAlignmentOptions.MidlineRight,
            new Vector2(12, 0), new Vector2(-28, 0));
        tip.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        tip.characterSpacing = 6f;
    }

    // ─── WIRING ────────────────────────────────────────────────────────

    private void WireReferences()
    {
        if (lobbyUI == null) return;

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

    // ─── UI Factory Helpers ────────────────────────────────────────────

    private static GameObject CreateAnchoredPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rt = (RectTransform)obj.transform;
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

    private GameObject CreateChunkyPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Color fill, Color bottomStrip, int cornerRadius = 10)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rt = (RectTransform)obj.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = obj.AddComponent<Image>();
        img.sprite = LobbyVisuals.GetRoundedRect(cornerRadius, 3, fill, outlineDark);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;

        var shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(0f, -4f);

        var strip = new GameObject("BottomStrip", typeof(RectTransform));
        strip.transform.SetParent(obj.transform, false);
        var stripRT = (RectTransform)strip.transform;
        stripRT.anchorMin = new Vector2(0f, 0f);
        stripRT.anchorMax = new Vector2(1f, 0.04f);
        stripRT.offsetMin = new Vector2(3, 3);
        stripRT.offsetMax = new Vector2(-3, 0);
        var stripImg = strip.AddComponent<Image>();
        stripImg.sprite = LobbyVisuals.GetRoundedRect(Mathf.Max(1, cornerRadius - 4), 0, bottomStrip, bottomStrip);
        stripImg.type = Image.Type.Sliced;
        stripImg.color = Color.white;
        stripImg.raycastTarget = false;

        return obj;
    }

    private TMP_Text CreateText(string name, Transform parent, string text, int fontSize, Color color,
        Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions alignment = TextAlignmentOptions.Center,
        Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rt = (RectTransform)obj.transform;
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
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button CreateLobbyButton(string name, Transform parent, string label, string iconGlyph,
        Vector2 anchorMin, Vector2 anchorMax, Color tint, bool big = false)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rt = (RectTransform)obj.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Color bottomCol = tint * 0.55f; bottomCol.a = 1f;
        int corner = big ? 14 : 10;
        var img = obj.AddComponent<Image>();
        img.sprite = LobbyVisuals.GetRoundedRect(corner, big ? 4 : 3, tint, outlineDark);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;

        var shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(0f, big ? -4f : -3f);

        var outline = obj.AddComponent<Outline>();
        Color outlineCol = accentYellow;
        outlineCol.a = 0f;
        outline.effectColor = outlineCol;
        outline.effectDistance = new Vector2(big ? 2.5f : 2f, big ? -2.5f : -2f);

        var btn = obj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.10f, 1.10f, 1.10f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
        btn.targetGraphic = img;

        var depth = new GameObject("DepthStrip", typeof(RectTransform));
        depth.transform.SetParent(obj.transform, false);
        var depthRT = (RectTransform)depth.transform;
        depthRT.anchorMin = new Vector2(0f, 0f);
        depthRT.anchorMax = new Vector2(1f, 0.18f);
        depthRT.offsetMin = new Vector2(4, 4);
        depthRT.offsetMax = new Vector2(-4, 0);
        var depthImg = depth.AddComponent<Image>();
        depthImg.sprite = LobbyVisuals.GetRoundedRect(Mathf.Max(1, corner - 4), 0, bottomCol, bottomCol);
        depthImg.type = Image.Type.Sliced;
        depthImg.color = Color.white;
        depthImg.raycastTarget = false;

        var sheen = new GameObject("Sheen", typeof(RectTransform));
        sheen.transform.SetParent(obj.transform, false);
        var sheenRT = (RectTransform)sheen.transform;
        sheenRT.anchorMin = new Vector2(0f, 0.55f);
        sheenRT.anchorMax = new Vector2(1f, 1f);
        sheenRT.offsetMin = new Vector2(4, 0);
        sheenRT.offsetMax = new Vector2(-4, -4);
        var sheenImg = sheen.AddComponent<Image>();
        sheenImg.sprite = LobbyVisuals.GetVerticalGradient(new Color(1f, 1f, 1f, 0.18f), new Color(1f, 1f, 1f, 0f));
        sheenImg.color = Color.white;
        sheenImg.raycastTarget = false;

        var glow = new GameObject("InnerGlow", typeof(RectTransform));
        glow.transform.SetParent(obj.transform, false);
        var glowRT = (RectTransform)glow.transform;
        glowRT.anchorMin = new Vector2(-0.10f, -0.20f);
        glowRT.anchorMax = new Vector2(1.10f, 1.20f);
        glowRT.offsetMin = Vector2.zero;
        glowRT.offsetMax = Vector2.zero;
        var glowImg = glow.AddComponent<Image>();
        glowImg.sprite = LobbyVisuals.GetRadialGlow(accentYellow);
        glowImg.color = new Color(1f, 1f, 1f, 0f);
        glowImg.raycastTarget = false;
        glow.transform.SetSiblingIndex(0);

        if (!string.IsNullOrEmpty(iconGlyph))
        {
            var icon = CreateText($"{name}_Icon", obj.transform, iconGlyph, big ? 28 : 22,
                Color.white, new Vector2(0.04f, 0.10f), new Vector2(0.18f, 0.90f), TextAlignmentOptions.Center);
            icon.fontStyle = FontStyles.Bold;
            var iconShadow = icon.gameObject.AddComponent<Shadow>();
            iconShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            iconShadow.effectDistance = new Vector2(1f, -1f);
        }

        var labelObj = CreateText($"{name}_Label", obj.transform, label, big ? 28 : 18, Color.white,
            new Vector2(0.20f, 0.10f), new Vector2(0.92f, 0.90f), TextAlignmentOptions.Center,
            new Vector2(0, 4), new Vector2(0, -4));
        labelObj.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        labelObj.characterSpacing = big ? 10f : 6f;
        var labelOutline = labelObj.gameObject.AddComponent<Outline>();
        labelOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        labelOutline.effectDistance = new Vector2(1.5f, -1.5f);

        var hover = obj.AddComponent<UIButtonHoverEffect>();
        hover.Bind(outline, glowImg);

        return btn;
    }

    private Button CreateCircularArrowButton(string name, Transform parent, string arrow,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rt = (RectTransform)obj.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = obj.AddComponent<Image>();
        img.sprite = LobbyVisuals.GetRoundedRect(64, 4, accentYellow, outlineDark);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;

        var shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(0f, -3f);

        var outline = obj.AddComponent<Outline>();
        Color outlineCol = Color.white;
        outlineCol.a = 0f;
        outline.effectColor = outlineCol;
        outline.effectDistance = new Vector2(2f, -2f);

        var btn = obj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.10f, 1.10f, 1.10f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        btn.colors = colors;
        btn.targetGraphic = img;

        var glow = new GameObject("InnerGlow", typeof(RectTransform));
        glow.transform.SetParent(obj.transform, false);
        var glowRT = (RectTransform)glow.transform;
        glowRT.anchorMin = new Vector2(-0.15f, -0.15f);
        glowRT.anchorMax = new Vector2(1.15f, 1.15f);
        glowRT.offsetMin = Vector2.zero;
        glowRT.offsetMax = Vector2.zero;
        var glowImg = glow.AddComponent<Image>();
        glowImg.sprite = LobbyVisuals.GetRadialGlow(accentYellow);
        glowImg.color = new Color(1f, 1f, 1f, 0f);
        glowImg.raycastTarget = false;
        glow.transform.SetSiblingIndex(0);

        var arrowText = CreateText($"{name}_Arrow", obj.transform, arrow, 56, outlineDark,
            Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
        arrowText.fontStyle = FontStyles.Bold;

        var hover = obj.AddComponent<UIButtonHoverEffect>();
        hover.Bind(outline, glowImg);

        return btn;
    }

    private void Track(GameObject go)
    {
        if (go == null) return;
        _generatedChildren.Add(go);
    }
}
