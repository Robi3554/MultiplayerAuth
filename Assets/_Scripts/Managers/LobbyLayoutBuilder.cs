using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a vibrant, game-y lobby layout procedurally at runtime.
///
/// Style direction is Fortnite-ish: saturated solid color blocks, chunky borders,
/// thick yellow accent strips, big bold uppercase typography, flat panels with
/// strong contrast — not the more subtle realistic/tech look.
///
/// Three columns sit over a saturated blue background with a faint diagonal
/// pattern overlay. Each panel has a chunky yellow band across its top.
/// Buttons have a solid color fill plus a darker bottom strip for a chunky 3D
/// depth, and a <see cref="UIButtonHoverEffect"/> that lerps scale and
/// outline/glow alphas on hover/press/select. The selected state pops with a
/// bright yellow outline and pulse.
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

    // ─── Palette (Fortnite-inspired) ──────────────────────────────────
    // Saturated blues form the base; bright yellow is the universal accent /
    // selection color; team/mode buttons keep their distinct hues but get the
    // chunky border treatment.
    private static readonly Color BgPrimary = new Color(0.10f, 0.16f, 0.40f, 1f);   // deep royal blue
    private static readonly Color BgSecondary = new Color(0.14f, 0.22f, 0.55f, 1f); // mid royal blue
    private static readonly Color PanelFill = new Color(0.13f, 0.21f, 0.52f, 1f);
    private static readonly Color PanelBottom = new Color(0.05f, 0.09f, 0.25f, 1f); // chunky bottom strip
    private static readonly Color AccentYellow = new Color(1.00f, 0.82f, 0.18f, 1f);
    private static readonly Color AccentYellowDim = new Color(0.78f, 0.62f, 0.10f, 1f);
    private static readonly Color AccentMint = new Color(0.32f, 0.86f, 0.68f, 1f);
    private static readonly Color TextWhite = new Color(0.99f, 1.00f, 1.00f, 1f);
    private static readonly Color TextDim = new Color(0.78f, 0.85f, 1.00f, 1f);
    private static readonly Color OutlineDark = new Color(0.02f, 0.05f, 0.14f, 1f);

    // Team / Mode tints — kept saturated, with a darker bottom strip per button.
    private static readonly Color RebelsCol = new Color(0.95f, 0.30f, 0.34f, 1f);
    private static readonly Color AICol = new Color(0.30f, 0.55f, 1.00f, 1f);
    private static readonly Color NoneCol = new Color(0.55f, 0.58f, 0.70f, 1f);
    private static readonly Color FFACol = new Color(1.00f, 0.65f, 0.18f, 1f);
    private static readonly Color TDMCol = new Color(0.32f, 0.86f, 0.55f, 1f);
    private static readonly Color ReadyCol = new Color(1.00f, 0.82f, 0.18f, 1f); // yellow CTA — Fortnite-y

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
    private TMP_Text characterTaglineText;

    private void Awake()
    {
        BuildLayout();
        WireReferences();
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

        BuildLeftPanel(lobbyContentRoot.transform);
        BuildCenterPanel(lobbyContentRoot.transform);
        BuildRightPanel(lobbyContentRoot.transform);
        BuildStatusBar(lobbyContentRoot.transform);
        BuildTopTitle(lobbyContentRoot.transform);
    }

    private static void BuildBackground(Transform parent)
    {
        // Solid saturated base: vertical step from BgPrimary (top) to BgSecondary (bottom).
        // Kept gentle so the background reads as a single saturated color, not a "moody" gradient.
        var bg = CreateAnchoredPanel("BG_Base", parent, Vector2.zero, Vector2.one, Color.white);
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = LobbyVisuals.GetVerticalGradient(BgPrimary, BgSecondary);
        bgImg.type = Image.Type.Simple;
        bgImg.raycastTarget = false;

        // Subtle diagonal scan-line pattern at low alpha so the BG isn't completely flat
        var pattern = CreateAnchoredPanel("BG_Pattern", parent, Vector2.zero, Vector2.one, Color.white);
        var patImg = pattern.GetComponent<Image>();
        patImg.sprite = LobbyVisuals.GetSubtlePattern(new Color(1f, 1f, 1f, 1f));
        patImg.type = Image.Type.Tiled;
        patImg.color = new Color(1f, 1f, 1f, 0.07f);
        patImg.raycastTarget = false;

        // Thick yellow accent stripe along the very top of the screen (Fortnite signature).
        var topStripe = CreateAnchoredPanel("BG_TopStripe", parent,
            new Vector2(0f, 0.985f), new Vector2(1f, 1f), AccentYellow);
        var stripeImg = topStripe.GetComponent<Image>();
        stripeImg.raycastTarget = false;
        // Hard shadow under the stripe for a chunky outline
        var topShadow = CreateAnchoredPanel("BG_TopStripeShadow", parent,
            new Vector2(0f, 0.978f), new Vector2(1f, 0.985f), new Color(0f, 0f, 0f, 0.55f));
        topShadow.GetComponent<Image>().raycastTarget = false;
    }

    private void BuildTopTitle(Transform parent)
    {
        var bar = CreateAnchoredPanel("TitleBar", parent,
            new Vector2(0f, 0.945f), new Vector2(1f, 0.985f), Color.clear);

        var title = CreateText("LobbyTitle", bar.transform, "MULTIPLAYER LOBBY", 38, AccentYellow,
            new Vector2(0f, 0f), new Vector2(1f, 1f), TextAlignmentOptions.Center,
            new Vector2(0, 0), new Vector2(0, 0));
        title.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        title.characterSpacing = 8f;

        // Chunky black outline + dark drop shadow for the punchy game-y look
        var titleOutline = title.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = OutlineDark;
        titleOutline.effectDistance = new Vector2(2.2f, -2.2f);
        var titleShadow = title.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        titleShadow.effectDistance = new Vector2(0f, -3f);
    }

    // ─── LEFT PANEL: Player List ──────────────────────────────────────

    private void BuildLeftPanel(Transform parent)
    {
        var panel = CreateChunkyPanel("LeftPanel", parent,
            new Vector2(0.013f, 0.075f),
            new Vector2(0.245f, 0.93f),
            PanelFill, PanelBottom);

        // Yellow header strip across the very top of the panel
        var headerStrip = CreateAnchoredPanel("HeaderStripe", panel.transform,
            new Vector2(0f, 0.92f), new Vector2(1f, 0.985f), AccentYellow);
        headerStrip.GetComponent<Image>().raycastTarget = false;

        // Header text on top of the strip
        var header = CreateText("PlayerListHeader", panel.transform, "PLAYERS", 24, OutlineDark,
            new Vector2(0f, 0.92f), new Vector2(1f, 0.985f), TextAlignmentOptions.Center,
            new Vector2(10, 0), new Vector2(-10, 0));
        header.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        header.characterSpacing = 10f;

        // Scrollable list area
        var scrollArea = CreateAnchoredPanel("PlayerScrollArea", panel.transform,
            new Vector2(0.025f, 0.04f), new Vector2(0.975f, 0.905f), Color.clear);

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

    // ─── CENTER PANEL: Character Preview (no frame, just stage) ───────

    private void BuildCenterPanel(Transform parent)
    {
        var panel = CreateAnchoredPanel("CenterPanel", parent,
            new Vector2(0.255f, 0.075f),
            new Vector2(0.745f, 0.93f),
            Color.clear);

        // Big radial glow behind the character — tinted by the active character's accent color.
        var glow = CreateAnchoredPanel("PreviewGlow", panel.transform,
            new Vector2(-0.10f, -0.05f), new Vector2(1.10f, 0.92f), Color.white);
        var glowImg = glow.GetComponent<Image>();
        glowImg.sprite = LobbyVisuals.GetRadialGlow(AccentYellow);
        glowImg.color = new Color(1f, 1f, 1f, 0.55f);
        glowImg.raycastTarget = false;

        // The RawImage stands on its own — no surrounding frame, just open to the background.
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

        // Yellow "stage" stripe under the character — gives the model a podium feel.
        var stageStripe = CreateAnchoredPanel("StageStripe", panel.transform,
            new Vector2(0.10f, 0.085f), new Vector2(0.90f, 0.110f), AccentYellow);
        stageStripe.GetComponent<Image>().raycastTarget = false;
        // Dark shadow underneath the stripe for chunkiness
        var stageShadow = CreateAnchoredPanel("StageStripeShadow", panel.transform,
            new Vector2(0.10f, 0.075f), new Vector2(0.90f, 0.088f), new Color(0f, 0f, 0f, 0.45f));
        stageShadow.GetComponent<Image>().raycastTarget = false;

        // BIG character name above
        var charNameText = CreateText("CharacterName", panel.transform, "Select Character", 48, AccentYellow,
            new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.965f), TextAlignmentOptions.Center);
        charNameText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        charNameText.characterSpacing = 6f;
        var nameOutline = charNameText.gameObject.AddComponent<Outline>();
        nameOutline.effectColor = OutlineDark;
        nameOutline.effectDistance = new Vector2(2.5f, -2.5f);
        var nameShadow = charNameText.gameObject.AddComponent<Shadow>();
        nameShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        nameShadow.effectDistance = new Vector2(0f, -4f);

        // Tagline below the name
        characterTaglineText = CreateText("CharacterTagline", panel.transform, "", 18, TextDim,
            new Vector2(0.10f, 0.835f), new Vector2(0.90f, 0.880f), TextAlignmentOptions.Center);
        characterTaglineText.fontStyle = FontStyles.Italic;

        // Circular yellow arrow buttons
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

            // Retint the radial glow + name highlight when the character changes.
            characterPreview.OnDefinitionChanged += def =>
            {
                if (def == null) return;
                glowImg.color = new Color(def.accentColor.r, def.accentColor.g, def.accentColor.b, 0.65f);
                charNameText.color = def.accentColor;
                // Keep the dark outline so the text remains punchy regardless of accent.
            };
        }
    }

    // ─── RIGHT PANEL: Team, Mode, Ready ───────────────────────────────

    private void BuildRightPanel(Transform parent)
    {
        var panel = CreateChunkyPanel("RightPanel", parent,
            new Vector2(0.755f, 0.075f),
            new Vector2(0.987f, 0.93f),
            PanelFill, PanelBottom);

        // Yellow header strip across the very top of the panel (matches LeftPanel style)
        var headerStrip = CreateAnchoredPanel("HeaderStripe", panel.transform,
            new Vector2(0f, 0.92f), new Vector2(1f, 0.985f), AccentYellow);
        headerStrip.GetComponent<Image>().raycastTarget = false;
        var loadout = CreateText("LoadoutHeader", panel.transform, "LOADOUT", 24, OutlineDark,
            new Vector2(0f, 0.92f), new Vector2(1f, 0.985f), TextAlignmentOptions.Center);
        loadout.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        loadout.characterSpacing = 12f;

        float yTop = 0.91f;
        float headerHeight = 0.045f;
        float sectionGap = 0.018f;
        float btnH = 0.075f;
        float btnGap = 0.014f;

        // ─── TEAM ───
        CreateSectionHeader(panel.transform, "TEAM", yTop - headerHeight, yTop);
        yTop -= headerHeight + sectionGap;

        rebelsButton = CreateLobbyButton("RebelsBtn", panel.transform, "REBELS", "✦",
            new Vector2(0.07f, yTop - btnH), new Vector2(0.93f, yTop), RebelsCol);
        yTop -= btnH + btnGap;

        aiButton = CreateLobbyButton("AIBtn", panel.transform, "A.I.", "◆",
            new Vector2(0.07f, yTop - btnH), new Vector2(0.93f, yTop), AICol);
        yTop -= btnH + btnGap;

        noTeamButton = CreateLobbyButton("NoTeamBtn", panel.transform, "NO TEAM", "—",
            new Vector2(0.07f, yTop - btnH), new Vector2(0.93f, yTop), NoneCol);
        yTop -= btnH + sectionGap * 2;

        // ─── GAME MODE ───
        CreateSectionHeader(panel.transform, "MODE", yTop - headerHeight, yTop);
        yTop -= headerHeight + sectionGap;

        ffaButton = CreateLobbyButton("FFABtn", panel.transform, "FREE FOR ALL", "●",
            new Vector2(0.07f, yTop - btnH), new Vector2(0.93f, yTop), FFACol);
        yTop -= btnH + btnGap;

        tdmButton = CreateLobbyButton("TDMBtn", panel.transform, "TEAM DEATHMATCH", "▲",
            new Vector2(0.07f, yTop - btnH), new Vector2(0.93f, yTop), TDMCol);
        yTop -= btnH + sectionGap;

        // Vote tally
        gameModeText = CreateText("GameModeVote", panel.transform, "", 14, TextDim,
            new Vector2(0.06f, yTop - 0.05f), new Vector2(0.94f, yTop), TextAlignmentOptions.Center);
        gameModeText.fontStyle = FontStyles.Italic;

        // ─── READY (huge yellow CTA at the bottom) ───
        float readyH = 0.11f;
        float readyY = 0.035f;
        readyButton = CreateLobbyButton("ReadyBtn", panel.transform, "READY UP", "✓",
            new Vector2(0.06f, readyY), new Vector2(0.94f, readyY + readyH), ReadyCol, big: true);
        readyButtonText = readyButton.GetComponentInChildren<TMP_Text>();
        readyButtonImage = readyButton.GetComponent<Image>();

        // Pulse when armed
        var readyHover = readyButton.GetComponent<UIButtonHoverEffect>();
        if (readyHover != null)
            readyHover.EnableSelectedPulse(speed: 2.6f, amplitude: 0.45f);
    }

    private void CreateSectionHeader(Transform parent, string text, float yMin, float yMax)
    {
        var header = CreateText($"Header_{text}", parent, text, 18, AccentYellow,
            new Vector2(0f, yMin), new Vector2(1f, yMax), TextAlignmentOptions.MidlineLeft,
            new Vector2(20, 0), new Vector2(-20, 0));
        header.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        header.characterSpacing = 10f;

        // Thick yellow underline bar under the header — flat, no gradient
        var bar = CreateAnchoredPanel($"HeaderBar_{text}", parent,
            new Vector2(0.07f, yMin - 0.004f), new Vector2(0.93f, yMin + 0.001f), AccentYellow);
        bar.GetComponent<Image>().raycastTarget = false;
    }

    // ─── STATUS BAR ────────────────────────────────────────────────────

    private void BuildStatusBar(Transform parent)
    {
        var bar = CreateChunkyPanel("StatusBar", parent,
            new Vector2(0.013f, 0.012f), new Vector2(0.987f, 0.062f),
            new Color(0.07f, 0.11f, 0.30f, 1f), new Color(0.03f, 0.05f, 0.16f, 1f));

        // Left: connection / counts
        statusText = CreateText("StatusText", bar.transform, "Connecting...", 18, TextWhite,
            new Vector2(0f, 0f), new Vector2(0.55f, 1f), TextAlignmentOptions.MidlineLeft,
            new Vector2(28, 0), new Vector2(-12, 0));
        statusText.fontStyle = FontStyles.Bold;

        // Right: tip
        var tip = CreateText("StatusTip", bar.transform, "PICK A TEAM, GAME MODE, AND READY UP", 14, AccentYellow,
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

    /// <summary>
    /// Chunky panel with a flat fill, a darker bottom strip for "depth", a thick top
    /// black outline for the game-y look, and a hard drop shadow underneath.
    /// </summary>
    private static GameObject CreateChunkyPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Color fill, Color bottomStrip, int cornerRadius = 10)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rt = (RectTransform)obj.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Root: rounded rect with a thick dark border
        var img = obj.AddComponent<Image>();
        img.sprite = LobbyVisuals.GetRoundedRect(cornerRadius, 3, fill, OutlineDark);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;

        // Drop shadow
        var shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(0f, -4f);

        // Bottom strip child for chunky depth
        var strip = new GameObject("BottomStrip", typeof(RectTransform));
        strip.transform.SetParent(obj.transform, false);
        var stripRT = (RectTransform)strip.transform;
        stripRT.anchorMin = new Vector2(0f, 0f);
        stripRT.anchorMax = new Vector2(1f, 0.04f);
        stripRT.offsetMin = new Vector2(3, 3);
        stripRT.offsetMax = new Vector2(-3, 0);
        var stripImg = strip.AddComponent<Image>();
        stripImg.sprite = LobbyVisuals.GetRoundedRect(cornerRadius - 4, 0, bottomStrip, bottomStrip);
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

    /// <summary>
    /// Solid color button with: a thick dark outline, a darker bottom strip for
    /// chunky 3D depth, an icon glyph on the left, big bold uppercase label, and
    /// a <see cref="UIButtonHoverEffect"/> hover/select polish that brightens an
    /// outline + soft glow. The selected state shows a thick yellow accent ring.
    /// </summary>
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

        // Solid color fill, thick black border
        Color bottomCol = tint * 0.55f; bottomCol.a = 1f;
        int corner = big ? 14 : 10;
        var img = obj.AddComponent<Image>();
        img.sprite = LobbyVisuals.GetRoundedRect(corner, big ? 4 : 3, tint, OutlineDark);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;

        // Hard drop shadow
        var shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(0f, big ? -4f : -3f);

        // Yellow selected-state outline (alpha is driven by hover effect)
        var outline = obj.AddComponent<Outline>();
        Color outlineCol = AccentYellow;
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

        // Chunky bottom strip for the 3D depth (sits inside the rounded rect)
        var depth = new GameObject("DepthStrip", typeof(RectTransform));
        depth.transform.SetParent(obj.transform, false);
        var depthRT = (RectTransform)depth.transform;
        depthRT.anchorMin = new Vector2(0f, 0f);
        depthRT.anchorMax = new Vector2(1f, 0.18f);
        depthRT.offsetMin = new Vector2(4, 4);
        depthRT.offsetMax = new Vector2(-4, 0);
        var depthImg = depth.AddComponent<Image>();
        depthImg.sprite = LobbyVisuals.GetRoundedRect(corner - 4, 0, bottomCol, bottomCol);
        depthImg.type = Image.Type.Sliced;
        depthImg.color = Color.white;
        depthImg.raycastTarget = false;

        // Subtle top sheen — kept minimal for the flat-color feel
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

        // Selected-state glow (driven by hover effect)
        var glow = new GameObject("InnerGlow", typeof(RectTransform));
        glow.transform.SetParent(obj.transform, false);
        var glowRT = (RectTransform)glow.transform;
        glowRT.anchorMin = new Vector2(-0.10f, -0.20f);
        glowRT.anchorMax = new Vector2(1.10f, 1.20f);
        glowRT.offsetMin = Vector2.zero;
        glowRT.offsetMax = Vector2.zero;
        var glowImg = glow.AddComponent<Image>();
        glowImg.sprite = LobbyVisuals.GetRadialGlow(AccentYellow);
        glowImg.color = new Color(1f, 1f, 1f, 0f);
        glowImg.raycastTarget = false;
        glow.transform.SetSiblingIndex(0);

        // Icon glyph (left-aligned)
        if (!string.IsNullOrEmpty(iconGlyph))
        {
            var icon = CreateText($"{name}_Icon", obj.transform, iconGlyph, big ? 28 : 22,
                Color.white, new Vector2(0.04f, 0.10f), new Vector2(0.18f, 0.90f), TextAlignmentOptions.Center);
            icon.fontStyle = FontStyles.Bold;
            var iconShadow = icon.gameObject.AddComponent<Shadow>();
            iconShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            iconShadow.effectDistance = new Vector2(1f, -1f);
        }

        // Big bold uppercase label
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

    /// <summary>
    /// Big bright yellow circular arrow button used by the character carousel.
    /// </summary>
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
        img.sprite = LobbyVisuals.GetRoundedRect(64, 4, AccentYellow, OutlineDark);
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

        // Inner glow
        var glow = new GameObject("InnerGlow", typeof(RectTransform));
        glow.transform.SetParent(obj.transform, false);
        var glowRT = (RectTransform)glow.transform;
        glowRT.anchorMin = new Vector2(-0.15f, -0.15f);
        glowRT.anchorMax = new Vector2(1.15f, 1.15f);
        glowRT.offsetMin = Vector2.zero;
        glowRT.offsetMax = Vector2.zero;
        var glowImg = glow.AddComponent<Image>();
        glowImg.sprite = LobbyVisuals.GetRadialGlow(AccentYellow);
        glowImg.color = new Color(1f, 1f, 1f, 0f);
        glowImg.raycastTarget = false;
        glow.transform.SetSiblingIndex(0);

        var arrowText = CreateText($"{name}_Arrow", obj.transform, arrow, 56, OutlineDark,
            Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
        arrowText.fontStyle = FontStyles.Bold;

        var hover = obj.AddComponent<UIButtonHoverEffect>();
        hover.Bind(outline, glowImg);

        return btn;
    }
}
