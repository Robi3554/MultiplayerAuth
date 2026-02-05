using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Client-side lobby UI. Polls LobbyManager's SyncList for changes and refreshes the display.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Player List")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerEntryPrefab;

    [Header("Team Buttons")]
    [SerializeField] private Button rebelsButton;
    [SerializeField] private Button aiButton;
    [SerializeField] private Button noTeamButton;

    [Header("Game Mode Buttons")]
    [SerializeField] private Button ffaButton;
    [SerializeField] private Button tdmButton;

    [Header("Ready")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private Image readyButtonImage;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text gameModeText;

    private LobbyManager lobbyManager;
    private bool isReady;
    private bool hasJoined;
    private int lastPlayerHash = -1;
    private readonly List<GameObject> entryObjects = new();

    private static readonly Color RebelsColor = new Color(0.9f, 0.3f, 0.3f);
    private static readonly Color AIColor = new Color(0.3f, 0.5f, 0.9f);

    private void Start()
    {
        rebelsButton.onClick.AddListener(() => SelectTeam(Team.Rebels));
        aiButton.onClick.AddListener(() => SelectTeam(Team.AI));
        noTeamButton.onClick.AddListener(() => SelectTeam(Team.None));

        ffaButton.onClick.AddListener(() => SelectGameMode(GameMode.FreeForAll));
        tdmButton.onClick.AddListener(() => SelectGameMode(GameMode.TeamDeathmatch));

        readyButton.onClick.AddListener(ToggleReady);

        // Color the team buttons
        SetButtonColor(rebelsButton, RebelsColor);
        SetButtonColor(aiButton, AIColor);
    }

    private void Update()
    {
        // Wait for LobbyManager to be available (it's a scene NetworkObject, takes a frame to initialize)
        if (lobbyManager == null)
        {
            lobbyManager = LobbyManager.Instance;
            if (lobbyManager == null) return;
        }

        // Send username to server once after connecting
        if (!hasJoined)
        {
            string username = string.IsNullOrEmpty(ConnectionInfo.username)
                ? "Player"
                : ConnectionInfo.username;
            lobbyManager.CmdJoinLobby(username);
            hasJoined = true;
        }

        // Poll SyncList for changes via a simple hash comparison
        int currentHash = ComputePlayersHash();
        if (currentHash != lastPlayerHash)
        {
            RefreshUI();
            lastPlayerHash = currentHash;
        }

        // Show game starting status
        if (lobbyManager.IsGameStarting.Value)
        {
            statusText.text = "<color=yellow>Game starting...</color>";
        }
    }

    private int ComputePlayersHash()
    {
        int hash = lobbyManager.Players.Count * 397;
        for (int i = 0; i < lobbyManager.Players.Count; i++)
        {
            var p = lobbyManager.Players[i];
            hash ^= (p.ClientId * 31 + (int)p.Team * 7 + (int)p.PreferredMode * 3 + (p.IsReady ? 1 : 0)) << (i % 16);
            hash ^= (p.Username ?? "").GetHashCode();
        }
        return hash;
    }

    private void RefreshUI()
    {
        // Clear existing entries
        foreach (var obj in entryObjects)
            Destroy(obj);
        entryObjects.Clear();

        int readyCount = 0;
        int totalCount = lobbyManager.Players.Count;
        int ffaVotes = 0;
        int tdmVotes = 0;

        for (int i = 0; i < lobbyManager.Players.Count; i++)
        {
            var player = lobbyManager.Players[i];

            var go = Instantiate(playerEntryPrefab, playerListContent);
            var entry = go.GetComponent<LobbyPlayerEntry>();
            entry.Setup(player);
            entryObjects.Add(go);

            if (player.IsReady) readyCount++;
            if (player.PreferredMode == GameMode.FreeForAll) ffaVotes++;
            else tdmVotes++;
        }

        statusText.text = $"Players: {totalCount}  |  Ready: {readyCount}/{totalCount}";

        string leadingMode = tdmVotes > ffaVotes ? "Team Deathmatch" : "Free For All";
        gameModeText.text = $"Vote: {leadingMode}  (FFA: {ffaVotes} | TDM: {tdmVotes})";
    }

    // ─── Button Handlers ──────────────────────────────────────────────

    private void SelectTeam(Team team)
    {
        if (lobbyManager == null) return;
        lobbyManager.CmdSetTeam(team);
        isReady = false;
        UpdateReadyButton();
    }

    private void SelectGameMode(GameMode mode)
    {
        if (lobbyManager == null) return;
        lobbyManager.CmdSetGameMode(mode);
        isReady = false;
        UpdateReadyButton();
    }

    private void ToggleReady()
    {
        isReady = !isReady;
        lobbyManager?.CmdSetReady(isReady);
        UpdateReadyButton();
    }

    private void UpdateReadyButton()
    {
        readyButtonText.text = isReady ? "READY ✓" : "Ready Up";
        if (readyButtonImage != null)
            readyButtonImage.color = isReady ? Color.green : Color.white;
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    private static void SetButtonColor(Button button, Color color)
    {
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.1f;
        colors.pressedColor = color * 0.8f;
        button.colors = colors;
    }
}
