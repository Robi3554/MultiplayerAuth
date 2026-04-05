using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;

/// <summary>
/// Displayed in the game scene for late joiners (players who connect after the game has started).
/// Builds the entire UI at runtime to match the LobbyScene visual style.
/// Shows game mode, team player counts, lets the player pick a team, then spawns them in.
/// Attach this to any GameObject in SampleScene — the Canvas is created automatically.
/// </summary>
public class LateJoinUI : MonoBehaviour
{
    // ─── Lobby Color Palette ──────────────────────────────────────────
    private static readonly Color BgColor = new(0.1f, 0.1f, 0.14f, 1f);
    private static readonly Color TitleBarColor = new(0.08f, 0.08f, 0.12f, 1f);
    private static readonly Color PanelColor = new(0.14f, 0.14f, 0.2f, 0.95f);
    private static readonly Color DividerColor = new(1f, 1f, 1f, 0.08f);
    private static readonly Color RebelsColor = new(0.9f, 0.3f, 0.3f, 1f);
    private static readonly Color AIColor = new(0.3f, 0.5f, 0.9f, 1f);
    private static readonly Color NoneColor = new(0.45f, 0.45f, 0.5f, 1f);
    private static readonly Color JoinColor = new(0.25f, 0.8f, 0.35f, 1f);

    // ─── Runtime references ───────────────────────────────────────────
    private GameObject root;
    private TMP_Text statusText;
    private TMP_Text gameModeText;
    private TMP_Text teamCountsText;
    private Button rebelsButton;
    private Button aiButton;
    private Button noTeamButton;
    private Button joinButton;

    private LobbyManager lobbyManager;
    private Team selectedTeam = Team.None;
    private bool hasJoined;
    private bool uiBuilt;
    private int lastHash = -1;

    // ─── Lifecycle ────────────────────────────────────────────────────

    private void Update()
    {
        if (hasJoined)
        {
            if (root != null && root.activeSelf)
                root.SetActive(false);
            return;
        }

        if (lobbyManager == null)
        {
            lobbyManager = LobbyManager.Instance;
            if (lobbyManager == null || !lobbyManager.IsSpawned)
                return;
        }

        if (!lobbyManager.IsGameStarting.Value)
        {
            if (root != null) root.SetActive(false);
            return;
        }

        var clientConn = InstanceFinder.ClientManager?.Connection;
        if (clientConn == null)
            return;

        int localClientId = clientConn.ClientId;
        bool isPending = false;
        for (int i = 0; i < lobbyManager.Players.Count; i++)
        {
            if (lobbyManager.Players[i].ClientId == localClientId && !lobbyManager.Players[i].IsReady)
            {
                isPending = true;
                break;
            }
        }

        if (!isPending)
        {
            if (root != null) root.SetActive(false);
            return;
        }

        // Build UI once, only when needed
        if (!uiBuilt)
        {
            BuildUI();
            uiBuilt = true;
        }

        if (!root.activeSelf)
        {
            root.SetActive(true);
            if (LoadingManager.Instance != null)
                LoadingManager.Instance.Hide();
        }

        int hash = ComputeHash();
        if (hash != lastHash)
        {
            RefreshInfo();
            lastHash = hash;
        }
    }

    // ─── UI Construction ──────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas (overlay, on top of everything)
        var canvasGo = new GameObject("LateJoinCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        root = canvasGo;

        // Full-screen dark background
        var bg = CreatePanel(canvasGo.transform, "Background", BgColor);
        Stretch(bg);

        // Center container — fixed-width panel
        var container = CreatePanel(bg.transform, "Container", PanelColor);
        var containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(560, 0);
        containerRect.anchoredPosition = Vector2.zero;
        var containerLayout = container.AddComponent<VerticalLayoutGroup>();
        containerLayout.padding = new RectOffset(0, 0, 0, 0);
        containerLayout.spacing = 0;
        containerLayout.childControlWidth = true;
        containerLayout.childControlHeight = true;
        containerLayout.childForceExpandWidth = true;
        containerLayout.childForceExpandHeight = false;
        var containerFitter = container.AddComponent<ContentSizeFitter>();
        containerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Title Bar ──
        var titleBar = CreatePanel(container.transform, "TitleBar", TitleBarColor);
        var titleBarLayout = titleBar.AddComponent<HorizontalLayoutGroup>();
        titleBarLayout.padding = new RectOffset(24, 24, 14, 14);
        titleBarLayout.childAlignment = TextAnchor.MiddleCenter;
        titleBarLayout.childControlWidth = true;
        titleBarLayout.childControlHeight = true;

        CreateText(titleBar.transform, "TitleText", "JOINING GAME", 30, FontStyles.Bold);

        // ── Body ──
        var body = CreatePanel(container.transform, "Body", Color.clear);
        var bodyLayout = body.AddComponent<VerticalLayoutGroup>();
        bodyLayout.padding = new RectOffset(24, 24, 20, 20);
        bodyLayout.spacing = 14;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;

        // Status text
        statusText = CreateText(body.transform, "StatusText", "", 18, FontStyles.Normal);

        // Divider
        CreateDivider(body.transform, "Divider1");

        // Game mode
        CreateText(body.transform, "GameModeHeader", "GAME MODE", 22, FontStyles.Bold);
        gameModeText = CreateText(body.transform, "GameModeText", "", 18, FontStyles.Normal);

        // Divider
        CreateDivider(body.transform, "Divider2");

        // Team counts
        CreateText(body.transform, "PlayersHeader", "PLAYERS", 22, FontStyles.Bold);
        teamCountsText = CreateText(body.transform, "TeamCountsText", "", 18, FontStyles.Normal);

        // Divider
        CreateDivider(body.transform, "Divider3");

        // Team selection header
        CreateText(body.transform, "TeamHeader", "SELECT TEAM", 22, FontStyles.Bold);

        // Team button row
        var teamRow = new GameObject("TeamRow");
        teamRow.transform.SetParent(body.transform, false);
        var teamRowLayout = teamRow.AddComponent<HorizontalLayoutGroup>();
        teamRowLayout.spacing = 10;
        teamRowLayout.childControlWidth = true;
        teamRowLayout.childControlHeight = true;
        teamRowLayout.childForceExpandWidth = true;
        teamRowLayout.childForceExpandHeight = false;
        var teamRowLE = teamRow.AddComponent<LayoutElement>();
        teamRowLE.preferredHeight = 50;

        rebelsButton = CreateButton(teamRow.transform, "RebelsButton", "REBELS", RebelsColor);
        aiButton = CreateButton(teamRow.transform, "AIButton", "AI", AIColor);
        noTeamButton = CreateButton(teamRow.transform, "NoTeamButton", "NONE", NoneColor);

        rebelsButton.onClick.AddListener(() => SelectTeam(Team.Rebels));
        aiButton.onClick.AddListener(() => SelectTeam(Team.AI));
        noTeamButton.onClick.AddListener(() => SelectTeam(Team.None));

        // Spacer
        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(body.transform, false);
        var spacerLE = spacer.AddComponent<LayoutElement>();
        spacerLE.preferredHeight = 6;

        // Join button
        joinButton = CreateButton(body.transform, "JoinButton", "JOIN GAME", JoinColor, 26);
        var joinLE = joinButton.gameObject.AddComponent<LayoutElement>();
        joinLE.preferredHeight = 60;
        joinButton.onClick.AddListener(OnJoinClicked);

        // Initial highlight
        HighlightTeamButton(Team.None);
    }

    // ─── UI Factory Helpers ───────────────────────────────────────────

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static TMP_Text CreateText(Transform parent, string name, string content, float fontSize, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = false;
        return tmp;
    }

    private static void CreateDivider(Transform parent, string name)
    {
        var go = CreatePanel(parent, name, DividerColor);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 1;
        le.flexibleWidth = 1;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color, float fontSize = 20f)
    {
        var go = CreatePanel(parent, name, color);
        var btn = go.AddComponent<Button>();

        var colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.15f;
        colors.pressedColor = color * 0.75f;
        colors.selectedColor = color * 1.05f;
        colors.fadeDuration = 0.1f;
        btn.colors = colors;
        btn.targetGraphic = go.GetComponent<Image>();

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        Stretch(textGo);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    // ─── Logic ────────────────────────────────────────────────────────

    private int ComputeHash()
    {
        int hash = lobbyManager.Players.Count * 397;
        hash ^= (int)lobbyManager.ResolvedMode.Value * 31;
        for (int i = 0; i < lobbyManager.Players.Count; i++)
        {
            var p = lobbyManager.Players[i];
            hash ^= (p.ClientId * 17 + (int)p.Team * 7 + (p.IsReady ? 1 : 0)) << (i % 16);
        }
        return hash;
    }

    private void RefreshInfo()
    {
        GameMode mode = lobbyManager.ResolvedMode.Value;
        gameModeText.text = mode == GameMode.TeamDeathmatch ? "Team Deathmatch" : "Free For All";
        statusText.text = "<color=yellow>A game is already in progress</color>";

        int rebels = 0, ai = 0, none = 0;
        for (int i = 0; i < lobbyManager.Players.Count; i++)
        {
            var p = lobbyManager.Players[i];
            if (!p.IsReady) continue;
            switch (p.Team)
            {
                case Team.Rebels: rebels++; break;
                case Team.AI: ai++; break;
                default: none++; break;
            }
        }

        teamCountsText.text = $"Rebels: {rebels}   |   AI: {ai}   |   No Team: {none}";
    }

    private void SelectTeam(Team team)
    {
        selectedTeam = team;
        HighlightTeamButton(team);
    }

    private void HighlightTeamButton(Team team)
    {
        SetButtonColor(rebelsButton, team == Team.Rebels ? RebelsColor : RebelsColor * 0.5f);
        SetButtonColor(aiButton, team == Team.AI ? AIColor : AIColor * 0.5f);
        SetButtonColor(noTeamButton, team == Team.None ? NoneColor : NoneColor * 0.5f);
    }

    private void OnJoinClicked()
    {
        if (lobbyManager == null || !lobbyManager.IsSpawned) return;

        string username = string.IsNullOrEmpty(ConnectionInfo.username) ? "Player" : ConnectionInfo.username;
        lobbyManager.CmdLateJoin(username, selectedTeam);
        hasJoined = true;
        root.SetActive(false);
    }

    private static void SetButtonColor(Button button, Color color)
    {
        var img = button.GetComponent<Image>();
        if (img != null) img.color = color;

        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.15f;
        colors.pressedColor = color * 0.75f;
        colors.selectedColor = color * 1.05f;
        button.colors = colors;
    }
}
