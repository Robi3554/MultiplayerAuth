using FishNet.Connection;
using FishNet.Object;
using FishNet.Managing.Scened;
using UnityEngine;
using System.Collections.Generic; // Required for using Lists

public class PlayerSpawnerCustom : NetworkBehaviour
{
    [Header("Player Prefab")]
    [SerializeField]
    private NetworkObject _playerPrefab;

    [Header("Spawning - FFA / Default")]
    [Tooltip("Points where players may spawn in FFA mode.")]
    [SerializeField]
    private List<Transform> _spawnPoints = new List<Transform>();

    [Header("Spawning - Team Deathmatch")]
    [SerializeField]
    private List<Transform> _rebelsSpawnPoints = new List<Transform>();
    [SerializeField]
    private List<Transform> _aiSpawnPoints = new List<Transform>();

    [Tooltip("True to add the player to the default scene upon spawning.")]
    [SerializeField]
    private bool _addToDefaultScene = true;

    public override void OnStartServer()
    {
        base.OnStartServer();
        base.NetworkManager.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
        base.NetworkManager.SceneManager.OnLoadEnd += SceneManager_OnLoadEnd;

        // If players are already connected (lobby → game transition),
        // spawn them now since OnLoadEnd/OnClientLoadedStartScenes won't fire again.
        SpawnAllConnectedPlayers();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (base.NetworkManager != null)
        {
            base.NetworkManager.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
            base.NetworkManager.SceneManager.OnLoadEnd -= SceneManager_OnLoadEnd;
        }
    }

    /// <summary>
    /// Called when FishNet finishes loading a scene (e.g. lobby → game transition).
    /// Spawns players for all connected clients who don't already have a player object.
    /// </summary>
    private void SceneManager_OnLoadEnd(SceneLoadEndEventArgs args)
    {
        if (!args.QueueData.AsServer)
            return;

        SpawnAllConnectedPlayers();
    }

    /// <summary>
    /// Iterates all connected clients and spawns a player for anyone who doesn't have one yet.
    /// </summary>
    private void SpawnAllConnectedPlayers()
    {
        if (ServerManager == null || ServerManager.Clients == null)
            return;

        Debug.Log($"[Spawner] SpawnAllConnectedPlayers: {ServerManager.Clients.Count} clients connected.");
        foreach (var kvp in ServerManager.Clients)
        {
            NetworkConnection conn = kvp.Value;
            // Skip only if this client already has a spawned player object.
            // Connections may own other objects (eg. temporary/lobby/network objects),
            // which should not prevent spawning their gameplay player.
            if (HasSpawnedPlayer(conn))
                continue;

            SpawnPlayer(conn);
        }
    }

    private void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        // Only run spawning logic on the server side of the callback.
        if (!asServer)
            return;

        if (HasSpawnedPlayer(conn))
            return;

        SpawnPlayer(conn);
    }

    private static bool HasSpawnedPlayer(NetworkConnection conn)
    {
        if (conn == null || conn.Objects == null)
            return false;

        foreach (NetworkObject nobj in conn.Objects)
        {
            if (nobj == null)
                continue;

            if (nobj.GetComponent<PlayerStats>() != null)
                return true;
        }

        return false;
    }

    private void SpawnPlayer(NetworkConnection conn)
    {
        // Check if a player prefab is assigned.
        if (_playerPrefab == null)
        {
            Debug.LogWarning("Player Prefab is not set in the PlayerSpawner.");
            return;
        }

        // Determine this player's team
        Team playerTeam = Team.None;
        if (LobbyData.PlayerTeams.TryGetValue(conn.ClientId, out Team lobbyTeam))
        {
            playerTeam = lobbyTeam;
        }
        else if (LobbyData.ResolvedGameMode == GameMode.TeamDeathmatch)
        {
            // Late joiner in TDM: auto-assign to the team with fewer players
            playerTeam = LobbyData.GetAutoAssignTeam();
            LobbyData.PlayerTeams[conn.ClientId] = playerTeam;
            Debug.Log($"[Spawner] Late joiner ClientId={conn.ClientId} auto-assigned to {playerTeam}");
        }

        // Choose spawn point based on game mode and team
        Transform spawnTransform = GetSpawnPoint(playerTeam);

        // Spawn the player
        NetworkObject playerInstance = base.NetworkManager.GetPooledInstantiated(_playerPrefab, true);
        playerInstance.transform.SetPositionAndRotation(spawnTransform.position, spawnTransform.rotation);
        base.ServerManager.Spawn(playerInstance, conn);

        // Set the player's team on their PlayerStats
        var stats = playerInstance.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.team.Value = playerTeam;
        }

        if (_addToDefaultScene)
        {
            base.SceneManager.AddOwnerToDefaultScene(playerInstance);
        }

        Debug.Log($"[Spawner] Spawned player ClientId={conn.ClientId}, Team={playerTeam}, Mode={LobbyData.ResolvedGameMode}");
    }

    private Transform GetSpawnPoint(Team team)
    {
        List<Transform> points;

        if (LobbyData.ResolvedGameMode == GameMode.TeamDeathmatch)
        {
            if (team == Team.Rebels && _rebelsSpawnPoints.Count > 0)
                points = _rebelsSpawnPoints;
            else if (team == Team.AI && _aiSpawnPoints.Count > 0)
                points = _aiSpawnPoints;
            else
                points = _spawnPoints; // Fallback
        }
        else
        {
            points = _spawnPoints;
        }

        if (points == null || points.Count == 0)
        {
            Debug.LogWarning("[Spawner] No spawn points available! Using origin.");
            return transform;
        }

        return points[Random.Range(0, points.Count)];
    }
}