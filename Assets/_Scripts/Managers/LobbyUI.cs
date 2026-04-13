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
    
    [Header("Character Buttons")]
    [SerializeField] private GameObject characterButtonsParent;
    [SerializeField] private Button characterButtonTemplate;
    [SerializeField] private List<NetworkObject> characterOptions;

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

    private static readonly Color RebelsColor = new Color(0.9f, 0.3f, 0.3f);
    private static readonly Color AIColor = new Color(0.3f, 0.5f, 0.9f);

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

        InitializeCharacterButtons();

        readyButton.onClick.AddListener(ToggleReady);

        // Color the team buttons
        SetButtonColor(rebelsButton, RebelsColor);
        SetButtonColor(aiButton, AIColor);
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
                    string errorText = "<color=red>Could not connect.</color> Web builds require a browser-compatible transport (WebSocket/WebRTC).";
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
    
    private void InitializeCharacterButtons()
    {
        // Create a button for each character option
        foreach (var characterPrefab in characterOptions)
        {
            var go = Instantiate(characterButtonTemplate.gameObject, characterButtonsParent.transform);
            var button = go.GetComponent<Button>();
            var charName = characterPrefab.name;
            button.GetComponentInChildren<TMP_Text>().text = charName;
            button.onClick.AddListener(() =>
            {
                Debug.Log($"[LobbyUI] Character '{charName}' selected");
                lobbyManager.CmdSetCharacter(characterPrefab);
                isReady = false;
                UpdateReadyButton();
                HighlightCharacterButton(charName);
            });
        }
        
        Destroy(characterButtonTemplate.gameObject);
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
        lobbyManager.CmdSetTeam(team);
        isReady = false;
        UpdateReadyButton();
        HighlightTeamButton(team);
    }

    private void SelectGameMode(GameMode mode)
    {
        if (lobbyManager == null) return;
        lobbyManager.CmdSetGameMode(mode);
        isReady = false;
        UpdateReadyButton();
        HighlightModeButton(mode);
    }

    private void HighlightTeamButton(Team team)
    {
        SetButtonColor(rebelsButton, team == Team.Rebels ? RebelsColor : RebelsColor * 0.5f);
        SetButtonColor(aiButton, team == Team.AI ? AIColor : AIColor * 0.5f);
        SetButtonColor(noTeamButton, team == Team.None ? new Color(0.45f, 0.45f, 0.5f) : new Color(0.25f, 0.25f, 0.3f));
    }

    private void HighlightModeButton(GameMode mode)
    {
        Color ffaCol = new Color(0.9f, 0.65f, 0.2f);
        Color tdmCol = new Color(0.2f, 0.75f, 0.5f);
        SetButtonColor(ffaButton, mode == GameMode.FreeForAll ? ffaCol : ffaCol * 0.5f);
        SetButtonColor(tdmButton, mode == GameMode.TeamDeathmatch ? tdmCol : tdmCol * 0.5f);
    }
    
    private void HighlightCharacterButton(string characterName)
    {
        var buttonColour = new Color(0.8f, 0.8f, 0.8f);
        
        foreach (Transform child in characterButtonsParent.transform)
        {
            var button = child.GetComponent<Button>();
            if (button == null) continue;

            var text = button.GetComponentInChildren<TMP_Text>();
            if (text == null) continue;

            bool isSelected = text.text == characterName;
            SetButtonColor(button, isSelected ? buttonColour : buttonColour * 0.5f);
        }
    }

    private void ToggleReady()
    {
        isReady = !isReady;
        lobbyManager?.CmdSetReady(isReady);
        UpdateReadyButton();
    }

    private void UpdateReadyButton()
    {
        readyButtonText.text = isReady ? "READY!" : "Ready Up";
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
