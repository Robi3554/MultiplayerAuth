using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;
using FishNet.Managing.Scened;
using System.Collections;
using FishNet.Object;
using UnityEngine.Serialization;

/// <summary>
/// Client-side lobby UI. Polls LobbyManager's SyncList for changes and refreshes the display.
/// Layout: Left = Player list | Center = 3D character preview with arrows | Right = Team + Mode + Ready.
/// Hides all lobby content until the LobbyManager is confirmed available and the game is NOT
/// already in progress. If a game is already underway (reconnect scenario), shows a
/// "Joining game..." overlay and lets FishNet handle the scene transition.
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

    [Header("Character Preview")]
    [SerializeField] private CharacterPreviewUI characterPreview;

    [Header("Ready")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private Image readyButtonImage;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text gameModeText;

    [Header("Lobby Content Root (Optional)")]
    [Tooltip("Optional: a CHILD panel that contains lobby panels (player list, buttons, etc.). " +
             "If assigned, it will be hidden while connecting and during reconnect. " +
             "Must NOT be the same GameObject that LobbyUI lives on.")]
    [SerializeField] private GameObject lobbyContentRoot;

    [Header("Connection")]
    [SerializeField] private float managerWaitTimeoutSeconds = 10f;

    private LobbyManager lobbyManager;
    private bool isReady;
    private bool hasJoined;
    private bool lobbyRevealed;
    private bool connectionProblemReported;
    private float managerWaitElapsed;
    private int lastPlayerHash = -1;
    private readonly List<GameObject> entryObjects = new();

    // Cached hover-effect components for the polished selected/hover state.
    private UIButtonHoverEffect rebelsHover, aiHover, noTeamHover;
    private UIButtonHoverEffect ffaHover, tdmHover;
    private UIButtonHoverEffect readyHover;

    // Local UI state of "what does the user have actively selected"
    private Team selectedTeam = Team.None;
    private GameMode selectedMode = GameMode.FreeForAll;

    /// <summary>
    /// Called by LobbyLayoutBuilder to wire all UI references at runtime.
    /// When used, the serialized fields are overridden by the builder-created elements.
    /// </summary>
    public void SetupLayoutReferences(
        Transform playerListContent,
        GameObject playerEntryPrefab,
        Button rebelsButton,
        Button aiButton,
        Button noTeamButton,
        Button ffaButton,
        Button tdmButton,
        CharacterPreviewUI characterPreview,
        Button readyButton,
        TMP_Text readyButtonText,
        Image readyButtonImage,
        TMP_Text statusText,
        TMP_Text gameModeText,
        GameObject lobbyContentRoot)
    {
        this.playerListContent = playerListContent;
        this.playerEntryPrefab = playerEntryPrefab;
        this.rebelsButton = rebelsButton;
        this.aiButton = aiButton;
        this.noTeamButton = noTeamButton;
        this.ffaButton = ffaButton;
        this.tdmButton = tdmButton;
        this.characterPreview = characterPreview;
        this.readyButton = readyButton;
        this.readyButtonText = readyButtonText;
        this.readyButtonImage = readyButtonImage;
        this.statusText = statusText;
        this.gameModeText = gameModeText;
        this.lobbyContentRoot = lobbyContentRoot;
    }

    private void Start()
    {
        Debug.Log($"[LobbyUI] Start() — rebels={rebelsButton != null}, ai={aiButton != null}, noTeam={noTeamButton != null}, ffa={ffaButton != null}, tdm={tdmButton != null}, ready={readyButton != null}");

        // Safety: never let lobbyContentRoot point at our own GameObject (would kill Update)
        if (lobbyContentRoot != null && lobbyContentRoot == gameObject)
        {
            Debug.LogError("[LobbyUI] lobbyContentRoot must NOT be the same GameObject as LobbyUI! Ignoring.");
            lobbyContentRoot = null;
        }

        // Hide optional content root while we wait for the lobby to be ready
        if (lobbyContentRoot != null)
            lobbyContentRoot.SetActive(false);

        rebelsButton.onClick.AddListener(() => { Debug.Log("[LobbyUI] Rebels clicked"); SelectTeam(Team.Rebels); });
        aiButton.onClick.AddListener(() => { Debug.Log("[LobbyUI] AI clicked"); SelectTeam(Team.AI); });
        noTeamButton.onClick.AddListener(() => { Debug.Log("[LobbyUI] NoTeam clicked"); SelectTeam(Team.None); });

        ffaButton.onClick.AddListener(() => { Debug.Log("[LobbyUI] FFA clicked"); SelectGameMode(GameMode.FreeForAll); });
        tdmButton.onClick.AddListener(() => { Debug.Log("[LobbyUI] TDM clicked"); SelectGameMode(GameMode.TeamDeathmatch); });

        // Subscribe to character preview changes (arrow-based cycling)
        if (characterPreview != null)
            characterPreview.OnCharacterChanged += OnCharacterPreviewChanged;

        readyButton.onClick.AddListener(ToggleReady);

        // Cache the hover-effect components from the new procedural buttons (LobbyLayoutBuilder
        // attaches a UIButtonHoverEffect to every CreateLobbyButton output).
        rebelsHover = rebelsButton.GetComponent<UIButtonHoverEffect>();
        aiHover = aiButton.GetComponent<UIButtonHoverEffect>();
        noTeamHover = noTeamButton.GetComponent<UIButtonHoverEffect>();
        ffaHover = ffaButton.GetComponent<UIButtonHoverEffect>();
        tdmHover = tdmButton.GetComponent<UIButtonHoverEffect>();
        readyHover = readyButton.GetComponent<UIButtonHoverEffect>();

        HighlightTeamButton(selectedTeam);
        HighlightModeButton(selectedMode);
        UpdateReadyButton();
    }

    private void OnDestroy()
    {
        if (characterPreview != null)
            characterPreview.OnCharacterChanged -= OnCharacterPreviewChanged;
    }

    private void OnCharacterPreviewChanged(NetworkObject character)
    {
        if (lobbyManager == null) return;
        Debug.Log($"[LobbyUI] Character changed to '{character.name}'");
        lobbyManager.CmdSetCharacter(character);
        isReady = false;
        UpdateReadyButton();
    }

    private void Update()
    {
        // Wait for LobbyManager to be available (it's a global NetworkObject spawned at runtime)
        if (lobbyManager == null)
        {
            lobbyManager = LobbyManager.Instance;
            if (lobbyManager == null)
            {
                managerWaitElapsed += Time.unscaledDeltaTime;

                if (statusText != null)
                    statusText.text = "Connecting to lobby server...";

                if (managerWaitElapsed >= managerWaitTimeoutSeconds)
                {
                    // If content was hidden during connect, reveal it so users can see status text.
                    if (lobbyContentRoot != null)
                        lobbyContentRoot.SetActive(true);

#if UNITY_WEBGL && !UNITY_EDITOR
                    string errorText = "<color=red>Could not connect.</color> Check that the server is running and the WebSocket port is accessible.";
#else
                    string errorText = "<color=red>Could not connect to lobby server.</color> Check server address/port and server status.";
#endif

                    if (statusText != null)
                        statusText.text = errorText;
                    if (gameModeText != null)
                        gameModeText.text = "";

                    if (!connectionProblemReported)
                    {
                        Debug.LogError("[LobbyUI] Timed out waiting for LobbyManager.Instance. Client likely failed to connect or transport is incompatible.");
                        connectionProblemReported = true;
                    }
                }

                if (Time.frameCount % 120 == 0)
                    Debug.LogWarning($"[LobbyUI] Waiting for LobbyManager.Instance... (frame {Time.frameCount})");
                return;
            }

            managerWaitElapsed = 0f;
            Debug.Log("[LobbyUI] Found LobbyManager!");
        }

        // If the game is already starting / in progress (reconnect scenario), keep lobby
        // hidden and show a status message. FishNet will transition us to the game scene.
        if (lobbyManager.IsGameStarting.Value)
        {
            LoadingManager.Instance.Show();

            if (lobbyContentRoot != null)
                lobbyContentRoot.SetActive(false);
            if (statusText != null)
                statusText.text = "<color=yellow>Joining game in progress...</color>";
            return;
        }

        // Reveal the lobby UI once we know the lobby is active
        //if (!lobbyRevealed)
        //{
        //    if (lobbyContentRoot != null)
        //        lobbyContentRoot.SetActive(true);
        //    lobbyRevealed = true;
        //}

        // Send username to server once after connecting
        if (!hasJoined)
        {
            // Must wait until LobbyManager is spawned (networked) before calling ServerRpcs
            if (!lobbyManager.IsSpawned)
            {
                if (Time.frameCount % 120 == 0)
                    Debug.LogWarning($"[LobbyUI] LobbyManager found but NOT spawned yet (IsSpawned=false, frame {Time.frameCount})");
                return;
            }

            string username = string.IsNullOrEmpty(ConnectionInfo.username)
                ? "Player"
                : ConnectionInfo.username;
            Debug.Log($"[LobbyUI] Calling CmdJoinLobby('{username}'), IsSpawned={lobbyManager.IsSpawned}, Players.Count={lobbyManager.Players.Count}");
            lobbyManager.CmdJoinLobby(username);
            hasJoined = true;

            // The default character (index 0) is shown in CharacterPreviewUI.Start() before
            // lobbyManager is available, so OnCharacterPreviewChanged silently returns without
            // sending CmdSetCharacter. Push the current selection now that we're joined.
            if (characterPreview != null && characterPreview.CurrentCharacter != null)
            {
                Debug.Log($"[LobbyUI] Syncing default character '{characterPreview.CurrentCharacter.name}' to server.");
                lobbyManager.CmdSetCharacter(characterPreview.CurrentCharacter);
            }
        }

        // Poll SyncList for changes via a simple hash comparison
        int currentHash = ComputePlayersHash();
        if (currentHash != lastPlayerHash)
        {
            RefreshUI();
            lastPlayerHash = currentHash;
        }

        if (!lobbyRevealed && IsLobbyReady())
        {
            StartCoroutine(ShowLobby());
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────

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
            go.SetActive(true); // Prefab may be inactive — force active
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
        selectedTeam = team;
        lobbyManager.CmdSetTeam(team);
        isReady = false;
        HighlightTeamButton(team);
        UpdateReadyButton();
    }

    private void SelectGameMode(GameMode mode)
    {
        if (lobbyManager == null) return;
        selectedMode = mode;
        lobbyManager.CmdSetGameMode(mode);
        isReady = false;
        HighlightModeButton(mode);
        UpdateReadyButton();
    }

    private void HighlightTeamButton(Team team)
    {
        SetSelected(rebelsHover, team == Team.Rebels);
        SetSelected(aiHover, team == Team.AI);
        SetSelected(noTeamHover, team == Team.None);
    }

    private void HighlightModeButton(GameMode mode)
    {
        SetSelected(ffaHover, mode == GameMode.FreeForAll);
        SetSelected(tdmHover, mode == GameMode.TeamDeathmatch);
    }

    private void ToggleReady()
    {
        isReady = !isReady;
        lobbyManager?.CmdSetReady(isReady);
        UpdateReadyButton();
    }

    private void UpdateReadyButton()
    {
        if (readyButtonText != null)
            readyButtonText.text = isReady ? "READY!" : "READY UP";
        // The button image is the rounded sliced sprite; we don't recolor it directly anymore.
        // Instead, the hover effect drives the outline + glow alpha for a clean selected state.
        SetSelected(readyHover, isReady);
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    private static void SetSelected(UIButtonHoverEffect hover, bool selected)
    {
        if (hover != null) hover.SetSelected(selected);
    }

    private bool IsLobbyReady()
    {
        if (lobbyManager == null) return false;
        if (!lobbyManager.IsSpawned) return false;
        if (!hasJoined) return false;
        if (lobbyManager.Players.Count == 0) return false;

        return true;
    }

    private IEnumerator ShowLobby()
    {
        lobbyRevealed = true;

        // Let UI populate first
        yield return null;
        yield return null;

        if (lobbyContentRoot != null)
            lobbyContentRoot.SetActive(true);

        LoadingManager.Instance.Hide();
    }
}
