using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;

/// <summary>
/// Displayed in the game scene for late joiners (players who connect after the game has started).
/// Shows game mode, team player counts, and lets the player pick a team before spawning in.
/// </summary>
public class LateJoinUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject lateJoinPanel;

    [Header("Info")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text gameModeText;
    [SerializeField] private TMP_Text teamCountsText;

    [Header("Team Buttons")]
    [SerializeField] private Button rebelsButton;
    [SerializeField] private Button aiButton;
    [SerializeField] private Button noTeamButton;

    [Header("Join")]
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text joinButtonText;

    private LobbyManager lobbyManager;
    private Team selectedTeam = Team.None;
    private bool hasJoined;
    private int lastHash = -1;

    private static readonly Color RebelsColor = new Color(0.9f, 0.3f, 0.3f);
    private static readonly Color AIColor = new Color(0.3f, 0.5f, 0.9f);
    private static readonly Color NoneColor = new Color(0.45f, 0.45f, 0.5f);

    private void Start()
    {
        lateJoinPanel.SetActive(false);

        rebelsButton.onClick.AddListener(() => SelectTeam(Team.Rebels));
        aiButton.onClick.AddListener(() => SelectTeam(Team.AI));
        noTeamButton.onClick.AddListener(() => SelectTeam(Team.None));
        joinButton.onClick.AddListener(OnJoinClicked);

        SetButtonColor(rebelsButton, RebelsColor * 0.5f);
        SetButtonColor(aiButton, AIColor * 0.5f);
        SetButtonColor(noTeamButton, NoneColor);

        HighlightTeamButton(Team.None);
    }

    private void Update()
    {
        if (hasJoined)
        {
            if (lateJoinPanel.activeSelf)
                lateJoinPanel.SetActive(false);
            return;
        }

        if (lobbyManager == null)
        {
            lobbyManager = LobbyManager.Instance;
            if (lobbyManager == null || !lobbyManager.IsSpawned)
                return;
        }

        // Only relevant when a game is in progress
        if (!lobbyManager.IsGameStarting.Value)
        {
            lateJoinPanel.SetActive(false);
            return;
        }

        // Check if the local client is a pending late joiner
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
            lateJoinPanel.SetActive(false);
            return;
        }

        // Show the late-join panel
        if (!lateJoinPanel.activeSelf)
        {
            lateJoinPanel.SetActive(true);
            if (LoadingManager.Instance != null)
                LoadingManager.Instance.Hide();
        }

        // Refresh info when SyncList changes
        int hash = ComputeHash();
        if (hash != lastHash)
        {
            RefreshInfo();
            lastHash = hash;
        }
    }

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
        gameModeText.text = $"Game Mode: {(mode == GameMode.TeamDeathmatch ? "Team Deathmatch" : "Free For All")}";
        statusText.text = "<color=yellow>A game is already in progress</color>";

        int rebels = 0, ai = 0, none = 0;
        for (int i = 0; i < lobbyManager.Players.Count; i++)
        {
            var p = lobbyManager.Players[i];
            if (!p.IsReady) continue; // Don't count other pending late joiners
            switch (p.Team)
            {
                case Team.Rebels: rebels++; break;
                case Team.AI: ai++; break;
                default: none++; break;
            }
        }

        teamCountsText.text = $"Rebels: {rebels}  |  AI: {ai}  |  No Team: {none}";
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
        lateJoinPanel.SetActive(false);
    }

    private static void SetButtonColor(Button button, Color color)
    {
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.1f;
        colors.pressedColor = color * 0.8f;
        button.colors = colors;
    }
}
