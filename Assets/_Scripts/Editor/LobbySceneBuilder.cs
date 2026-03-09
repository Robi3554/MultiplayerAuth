using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Editor utility that (re)builds the entire LobbyScene UI from code.
/// Run via  Tools ▸ Build Lobby Scene UI.
/// </summary>
public static class LobbySceneBuilder
{
    // ─── Colours ──────────────────────────────────────────────────
    private static readonly Color BgDark      = new Color(0.10f, 0.10f, 0.14f, 1f);
    private static readonly Color PanelBg     = new Color(0.14f, 0.14f, 0.20f, 0.95f);
    private static readonly Color HeaderBg    = new Color(0.08f, 0.08f, 0.12f, 1f);
    private static readonly Color RebelsCol   = new Color(0.9f, 0.3f, 0.3f);
    private static readonly Color AICol       = new Color(0.3f, 0.5f, 0.9f);
    private static readonly Color NoneCol     = new Color(0.45f, 0.45f, 0.5f);
    private static readonly Color FfaCol      = new Color(0.9f, 0.65f, 0.2f);
    private static readonly Color TdmCol      = new Color(0.2f, 0.75f, 0.5f);
    private static readonly Color ReadyCol    = new Color(0.25f, 0.8f, 0.35f);
    private static readonly Color BtnDefault  = new Color(0.22f, 0.22f, 0.28f);
    private static readonly Color TextPrimary = Color.white;
    private static readonly Color TextMuted   = new Color(0.65f, 0.65f, 0.7f);

    [MenuItem("Tools/Build Lobby Scene UI")]
    public static void BuildLobbyUI()
    {
        // Make sure we're in LobbyScene
        Scene active = SceneManager.GetActiveScene();
        if (active.name != "LobbyScene")
        {
            if (!EditorUtility.DisplayDialog("Build Lobby UI",
                    $"Active scene is '{active.name}'. Open LobbyScene first, or continue anyway?",
                    "Continue", "Cancel"))
                return;
        }

        // Remove old LobbyCanvas if it exists
        var oldCanvas = GameObject.Find("LobbyCanvas");
        if (oldCanvas != null)
        {
            Undo.DestroyObjectImmediate(oldCanvas);
            Debug.Log("[LobbySceneBuilder] Removed old LobbyCanvas.");
        }

        // ── Root Canvas ──────────────────────────────────────────
        var canvasGo = new GameObject("LobbyCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create LobbyCanvas");

        var canvas  = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Full-screen dark background ──────────────────────────
        var bgGo = CreatePanel(canvasGo.transform, "Background", BgDark);
        Stretch(bgGo);

        // ══════════════════════════════════════════════════════════
        // MAIN LAYOUT  — Vertical:  Title  |  Body (H: Left + Right)  |  Bottom bar
        // ══════════════════════════════════════════════════════════
        var mainVert = CreateChild(bgGo.transform, "MainLayout");
        Stretch(mainVert);
        var mainVlg = mainVert.AddComponent<VerticalLayoutGroup>();
        mainVlg.padding = new RectOffset(40, 40, 20, 20);
        mainVlg.spacing = 12;
        mainVlg.childControlWidth = true;
        mainVlg.childControlHeight = true;
        mainVlg.childForceExpandWidth = true;
        mainVlg.childForceExpandHeight = false;

        // ─── 1. TITLE BAR ────────────────────────────────────────
        var titleBar = CreatePanel(mainVert.transform, "TitleBar", HeaderBg);
        SetLayoutElement(titleBar, preferredHeight: 70, flexibleWidth: 1);
        var titleHlg = titleBar.AddComponent<HorizontalLayoutGroup>();
        titleHlg.padding = new RectOffset(24, 24, 10, 10);
        titleHlg.childAlignment = TextAnchor.MiddleCenter;
        titleHlg.childControlWidth = true;
        titleHlg.childControlHeight = true;
        titleHlg.childForceExpandWidth = true;

        var titleTxt = CreateTMP(titleBar.transform, "TitleText", "GAME LOBBY", 36, TextPrimary, TextAlignmentOptions.Center);
        titleTxt.fontStyle = FontStyles.Bold | FontStyles.UpperCase;

        // ─── 2. BODY ─────────────────────────────────────────────
        var body = CreateChild(mainVert.transform, "Body");
        SetLayoutElement(body, flexibleHeight: 1, flexibleWidth: 1);
        var bodyHlg = body.AddComponent<HorizontalLayoutGroup>();
        bodyHlg.spacing = 16;
        bodyHlg.childControlWidth = true;
        bodyHlg.childControlHeight = true;
        bodyHlg.childForceExpandWidth = false;
        bodyHlg.childForceExpandHeight = true;

        // ─── 2a. LEFT PANEL (Player List) ────────────────────────
        var leftPanel = CreatePanel(body.transform, "LeftPanel", PanelBg);
        SetLayoutElement(leftPanel, flexibleWidth: 3, flexibleHeight: 1);
        var leftVlg = leftPanel.AddComponent<VerticalLayoutGroup>();
        leftVlg.padding = new RectOffset(16, 16, 12, 12);
        leftVlg.spacing = 8;
        leftVlg.childControlWidth = true;
        leftVlg.childControlHeight = false;
        leftVlg.childForceExpandWidth = true;
        leftVlg.childForceExpandHeight = false;

        // Player list header
        var plHeader = CreateTMP(leftPanel.transform, "PlayerListHeader", "PLAYERS", 22, TextMuted, TextAlignmentOptions.Left);
        plHeader.fontStyle = FontStyles.Bold;
        SetLayoutElement(plHeader.gameObject, preferredHeight: 34);

        // Divider
        var divider1 = CreatePanel(leftPanel.transform, "Divider", TextMuted);
        SetLayoutElement(divider1, preferredHeight: 2, flexibleWidth: 1);

        // Scroll area for player entries
        var scrollGo = CreateChild(leftPanel.transform, "PlayerScroll");
        SetLayoutElement(scrollGo, flexibleHeight: 1, flexibleWidth: 1);
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        var viewport = CreateChild(scrollGo.transform, "Viewport");
        Stretch(viewport);
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        // Content (this is what LobbyUI.playerListContent should reference)
        var content = CreateChild(viewport.transform, "Content");
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);
        var contentVlg = content.AddComponent<VerticalLayoutGroup>();
        contentVlg.padding = new RectOffset(4, 4, 4, 4);
        contentVlg.spacing = 6;
        contentVlg.childControlWidth = true;
        contentVlg.childControlHeight = false;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // Status text (bottom of left panel)
        var statusTxt = CreateTMP(leftPanel.transform, "StatusText", "Players: 0  |  Ready: 0/0", 18, TextMuted, TextAlignmentOptions.Left);
        SetLayoutElement(statusTxt.gameObject, preferredHeight: 28);

        // ─── 2b. RIGHT PANEL (Controls) ──────────────────────────
        var rightPanel = CreatePanel(body.transform, "RightPanel", PanelBg);
        SetLayoutElement(rightPanel, flexibleWidth: 2, flexibleHeight: 1);
        var rightVlg = rightPanel.AddComponent<VerticalLayoutGroup>();
        rightVlg.padding = new RectOffset(20, 20, 16, 16);
        rightVlg.spacing = 14;
        rightVlg.childControlWidth = true;
        rightVlg.childControlHeight = false;
        rightVlg.childForceExpandWidth = true;
        rightVlg.childForceExpandHeight = false;
        rightVlg.childAlignment = TextAnchor.UpperCenter;

        // — TEAM SELECTION SECTION —
        var teamHeader = CreateTMP(rightPanel.transform, "TeamHeader", "SELECT TEAM", 22, TextMuted, TextAlignmentOptions.Center);
        teamHeader.fontStyle = FontStyles.Bold;
        SetLayoutElement(teamHeader.gameObject, preferredHeight: 34);

        var teamRow = CreateChild(rightPanel.transform, "TeamRow");
        SetLayoutElement(teamRow, preferredHeight: 50, flexibleWidth: 1);
        var teamHlg = teamRow.AddComponent<HorizontalLayoutGroup>();
        teamHlg.spacing = 10;
        teamHlg.childControlWidth = true;
        teamHlg.childControlHeight = true;
        teamHlg.childForceExpandWidth = true;
        teamHlg.childForceExpandHeight = true;

        var rebelsBtn  = CreateButton(teamRow.transform, "RebelsButton",  "REBELS",  RebelsCol,  Color.white, 20);
        var aiBtn      = CreateButton(teamRow.transform, "AIButton",      "AI",      AICol,      Color.white, 20);
        var noTeamBtn  = CreateButton(teamRow.transform, "NoTeamButton",  "NONE",    NoneCol,    Color.white, 20);

        // Divider
        var divider2 = CreatePanel(rightPanel.transform, "Divider2", new Color(1, 1, 1, 0.08f));
        SetLayoutElement(divider2, preferredHeight: 2, flexibleWidth: 1);

        // — GAME MODE SECTION —
        var modeHeader = CreateTMP(rightPanel.transform, "ModeHeader", "VOTE GAME MODE", 22, TextMuted, TextAlignmentOptions.Center);
        modeHeader.fontStyle = FontStyles.Bold;
        SetLayoutElement(modeHeader.gameObject, preferredHeight: 34);

        var modeRow = CreateChild(rightPanel.transform, "ModeRow");
        SetLayoutElement(modeRow, preferredHeight: 50, flexibleWidth: 1);
        var modeHlg = modeRow.AddComponent<HorizontalLayoutGroup>();
        modeHlg.spacing = 10;
        modeHlg.childControlWidth = true;
        modeHlg.childControlHeight = true;
        modeHlg.childForceExpandWidth = true;
        modeHlg.childForceExpandHeight = true;

        var ffaBtn = CreateButton(modeRow.transform, "FFAButton", "FREE FOR ALL", FfaCol, Color.white, 18);
        var tdmBtn = CreateButton(modeRow.transform, "TDMButton", "TEAM DM",      TdmCol, Color.white, 18);

        // Vote tally text
        var gameModeTxt = CreateTMP(rightPanel.transform, "GameModeText", "Vote: Free For All  (FFA: 0 | TDM: 0)", 17, TextMuted, TextAlignmentOptions.Center);
        SetLayoutElement(gameModeTxt.gameObject, preferredHeight: 28);

        // Divider
        var divider3 = CreatePanel(rightPanel.transform, "Divider3", new Color(1, 1, 1, 0.08f));
        SetLayoutElement(divider3, preferredHeight: 2, flexibleWidth: 1);

        // — SPACER to push Ready button to the bottom —
        var spacer = CreateChild(rightPanel.transform, "Spacer");
        SetLayoutElement(spacer, flexibleHeight: 1);

        // — READY BUTTON —
        var readyBtn = CreateButton(rightPanel.transform, "ReadyButton", "READY UP", ReadyCol, Color.white, 26);
        SetLayoutElement(readyBtn, preferredHeight: 65, flexibleWidth: 1);
        var readyBtnTxt = readyBtn.GetComponentInChildren<TMP_Text>();
        readyBtnTxt.fontStyle = FontStyles.Bold;

        // ══════════════════════════════════════════════════════════
        // PLAYER ENTRY PREFAB  (created in scene, user will make it a prefab)
        // ══════════════════════════════════════════════════════════
        var entryGo = CreatePanel(canvasGo.transform, "LobbyPlayerEntry", new Color(0.18f, 0.18f, 0.24f, 0.9f));
        var entryRT = entryGo.GetComponent<RectTransform>();
        entryRT.sizeDelta = new Vector2(0, 56);
        SetLayoutElement(entryGo, preferredHeight: 56, flexibleWidth: 1);

        var entryHlg = entryGo.AddComponent<HorizontalLayoutGroup>();
        entryHlg.padding = new RectOffset(10, 10, 4, 4);
        entryHlg.spacing = 8;
        entryHlg.childControlWidth = true;
        entryHlg.childControlHeight = true;
        entryHlg.childForceExpandWidth = false;
        entryHlg.childForceExpandHeight = true;
        entryHlg.childAlignment = TextAnchor.MiddleLeft;

        // Team color bar (narrow strip on the left)
        var colorBarGo = CreatePanel(entryGo.transform, "TeamColorBar", NoneCol);
        SetLayoutElement(colorBarGo, preferredWidth: 6, flexibleHeight: 1);
        var colorBarImg = colorBarGo.GetComponent<Image>();

        // Username
        var usernameTxt = CreateTMP(entryGo.transform, "UsernameText", "PlayerName", 20, TextPrimary, TextAlignmentOptions.Left);
        SetLayoutElement(usernameTxt.gameObject, flexibleWidth: 3);

        // Team label
        var teamLabelTxt = CreateTMP(entryGo.transform, "TeamText", "-", 18, TextMuted, TextAlignmentOptions.Center);
        SetLayoutElement(teamLabelTxt.gameObject, preferredWidth: 100);

        // Ready checkmark
        var readyCheck = CreateChild(entryGo.transform, "ReadyCheckmark");
        SetLayoutElement(readyCheck, preferredWidth: 40);
        var checkTxt = readyCheck.AddComponent<TextMeshProUGUI>();
        checkTxt.text = "✓";
        checkTxt.fontSize = 28;
        checkTxt.color = ReadyCol;
        checkTxt.alignment = TextAlignmentOptions.Center;
        readyCheck.SetActive(false);

        // Add the LobbyPlayerEntry component
        var entryComp = entryGo.AddComponent<LobbyPlayerEntry>();
        // Wire serialized fields via SerializedObject
        var entrySO = new SerializedObject(entryComp);
        entrySO.FindProperty("usernameText").objectReferenceValue  = usernameTxt;
        entrySO.FindProperty("teamText").objectReferenceValue      = teamLabelTxt;
        entrySO.FindProperty("teamColorBar").objectReferenceValue  = colorBarImg;
        entrySO.FindProperty("readyCheckmark").objectReferenceValue = readyCheck;
        entrySO.ApplyModifiedPropertiesWithoutUndo();

        // Move entry off-canvas so it's invisible until user makes it a prefab
        entryGo.SetActive(false);

        // ══════════════════════════════════════════════════════════
        // WIRE UP LobbyUI COMPONENT
        // ══════════════════════════════════════════════════════════
        // Find or create the LobbyUI holder
        var lobbyUIGo = GameObject.Find("LobbyUI");
        if (lobbyUIGo == null)
        {
            lobbyUIGo = new GameObject("LobbyUI");
            Undo.RegisterCreatedObjectUndo(lobbyUIGo, "Create LobbyUI");
        }
        var lobbyUI = lobbyUIGo.GetComponent<LobbyUI>();
        if (lobbyUI == null)
            lobbyUI = lobbyUIGo.AddComponent<LobbyUI>();

        var lobbyUISO = new SerializedObject(lobbyUI);
        lobbyUISO.FindProperty("playerListContent").objectReferenceValue = content.transform;
        lobbyUISO.FindProperty("playerEntryPrefab").objectReferenceValue = entryGo;
        lobbyUISO.FindProperty("rebelsButton").objectReferenceValue      = rebelsBtn.GetComponent<Button>();
        lobbyUISO.FindProperty("aiButton").objectReferenceValue          = aiBtn.GetComponent<Button>();
        lobbyUISO.FindProperty("noTeamButton").objectReferenceValue      = noTeamBtn.GetComponent<Button>();
        lobbyUISO.FindProperty("ffaButton").objectReferenceValue         = ffaBtn.GetComponent<Button>();
        lobbyUISO.FindProperty("tdmButton").objectReferenceValue         = tdmBtn.GetComponent<Button>();
        lobbyUISO.FindProperty("readyButton").objectReferenceValue       = readyBtn.GetComponent<Button>();
        lobbyUISO.FindProperty("readyButtonText").objectReferenceValue   = readyBtnTxt;
        lobbyUISO.FindProperty("readyButtonImage").objectReferenceValue  = readyBtn.GetComponent<Image>();
        lobbyUISO.FindProperty("statusText").objectReferenceValue        = statusTxt;
        lobbyUISO.FindProperty("gameModeText").objectReferenceValue      = gameModeTxt;
        lobbyUISO.ApplyModifiedPropertiesWithoutUndo();

        // ── Mark scene dirty ─────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("<color=cyan>[LobbySceneBuilder]</color> Lobby UI built successfully! Don't forget to save the scene (Ctrl+S).");

        // Select the canvas so user can see it
        Selection.activeGameObject = canvasGo;
    }

    // ══════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ══════════════════════════════════════════════════════════════

    private static GameObject CreateChild(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static TMP_Text CreateTMP(Transform parent, string name, string text, float fontSize, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Color bgColor, Color textColor, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = bgColor;
        colors.highlightedColor = bgColor * 1.15f;
        colors.pressedColor     = bgColor * 0.75f;
        colors.selectedColor    = bgColor * 1.05f;
        btn.colors = colors;

        // Text child
        var txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        Stretch(txtGo);
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        return go;
    }

    private static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetLayoutElement(GameObject go, float preferredWidth = -1, float preferredHeight = -1, float flexibleWidth = -1, float flexibleHeight = -1)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (preferredWidth >= 0) le.preferredWidth = preferredWidth;
        if (preferredHeight >= 0) le.preferredHeight = preferredHeight;
        if (flexibleWidth >= 0) le.flexibleWidth = flexibleWidth;
        if (flexibleHeight >= 0) le.flexibleHeight = flexibleHeight;
    }
}
