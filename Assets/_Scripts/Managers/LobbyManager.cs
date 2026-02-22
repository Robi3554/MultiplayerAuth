using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using System.Collections;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    /// <summary>
    /// Data for a single player in the lobby. Synced to all clients via SyncList.
    /// </summary>
    public struct LobbyPlayerData
    {
        public int ClientId;
        public string Username;
        public Team Team;
        public GameMode PreferredMode;
        public bool IsReady;
    }

    public readonly SyncList<LobbyPlayerData> Players = new();
    public readonly SyncVar<bool> IsGameStarting = new(false);

    [SerializeField] private string gameSceneName = "SampleScene";

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"[Lobby] OnStartClient — IsSpawned={IsSpawned}, IsOwner={IsOwner}, ObjectId={ObjectId}");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        Debug.Log("[Lobby] Server started. Waiting for players...");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (NetworkManager != null)
            NetworkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            Players.Add(new LobbyPlayerData
            {
                ClientId = conn.ClientId,
                Username = "Connecting...",
                Team = Team.None,
                PreferredMode = GameMode.FreeForAll,
                IsReady = false
            });
            Debug.Log($"[Lobby] Player connected: ClientId={conn.ClientId}");
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            for (int i = Players.Count - 1; i >= 0; i--)
            {
                if (Players[i].ClientId == conn.ClientId)
                {
                    Players.RemoveAt(i);
                    break;
                }
            }
            Debug.Log($"[Lobby] Player disconnected: ClientId={conn.ClientId}");
        }
    }

    // ─── Client Commands ──────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    public void CmdJoinLobby(string username, NetworkConnection sender = null)
    {
        int clientId = sender.ClientId;
        UpdatePlayer(clientId, p =>
        {
            p.Username = username;
            return p;
        });
        Debug.Log($"[Lobby] {username} (ClientId={clientId}) joined lobby.");
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdSetTeam(Team team, NetworkConnection sender = null)
    {
        Debug.Log($"[Lobby] CmdSetTeam called: ClientId={sender.ClientId}, Team={team}");
        UpdatePlayer(sender.ClientId, p =>
        {
            p.Team = team;
            p.IsReady = false;
            return p;
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdSetGameMode(GameMode mode, NetworkConnection sender = null)
    {
        UpdatePlayer(sender.ClientId, p =>
        {
            p.PreferredMode = mode;
            p.IsReady = false;
            return p;
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdSetReady(bool ready, NetworkConnection sender = null)
    {
        UpdatePlayer(sender.ClientId, p =>
        {
            p.IsReady = ready;
            return p;
        });
        CheckAllReady();
    }

    // ─── Server Logic ─────────────────────────────────────────────────

    private void UpdatePlayer(int clientId, System.Func<LobbyPlayerData, LobbyPlayerData> modifier)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                Players[i] = modifier(Players[i]);
                Debug.Log($"[Lobby] UpdatePlayer: ClientId={clientId} updated. Team={Players[i].Team}, Ready={Players[i].IsReady}");
                return;
            }
        }
        Debug.LogWarning($"[Lobby] UpdatePlayer: ClientId={clientId} NOT FOUND in Players list (count={Players.Count})!");
    }

    [Server]
    private void CheckAllReady()
    {
        if (Players.Count == 0) return;
        if (IsGameStarting.Value) return;

        foreach (var p in Players)
        {
            if (!p.IsReady) return;
        }

        // All players are ready
        StartGame();
    }

    [Server]
    private void StartGame()
    {
        IsGameStarting.Value = true;

        // Resolve game mode by majority vote
        int ffaVotes = 0;
        int tdmVotes = 0;
        foreach (var p in Players)
        {
            if (p.PreferredMode == GameMode.FreeForAll) ffaVotes++;
            else tdmVotes++;
        }

        // TDM wins only with strict majority, otherwise FFA
        GameMode resolvedMode = tdmVotes > ffaVotes
            ? GameMode.TeamDeathmatch
            : GameMode.FreeForAll;

        // Store lobby data for the game scene to read
        LobbyData.Clear();
        LobbyData.ResolvedGameMode = resolvedMode;
        foreach (var p in Players)
        {
            LobbyData.PlayerTeams[p.ClientId] = p.Team;
        }

        Debug.Log($"[Lobby] Game starting! Mode={resolvedMode}, Players={Players.Count}");

        // Notify all clients
        RpcNotifyGameStarting(resolvedMode);

        // Load the game scene globally for all connected clients
        SceneLoadData sld = new SceneLoadData(gameSceneName);
        NetworkManager.SceneManager.LoadGlobalScenes(sld);

        // Wait a frame for the scene load to be queued, then despawn
        StartCoroutine(DespawnAfterDelay());
    }

    private IEnumerator DespawnAfterDelay()
    {
        // Wait for scene load to be fully processed before despawning
        yield return new WaitForSeconds(1f);
        if (IsServerInitialized)
            ServerManager.Despawn(gameObject);
    }

    [ObserversRpc]
    private void RpcNotifyGameStarting(GameMode mode)
    {
        Debug.Log($"[Lobby] Game starting with mode: {mode}");
    }
}
