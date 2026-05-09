using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Required for using Lists
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class PlayerSpawnerCustom : NetworkBehaviour
{
    public static PlayerSpawnerCustom Instance { get; private set; }

    [FormerlySerializedAs("_playerPrefab")]
    [Header("Player Prefab")]
    [SerializeField]
    private NetworkObject _defaultPlayerPrefab;

    [Header("Spawning - FFA / Default")]
    [Tooltip("Points where players may spawn in FFA mode.")]
    [SerializeField]
    private Transform ffaSpawnParent;
    private readonly List<Transform> _ffaSpawnPoints = new();

    [Header("Spawning - Team Deathmatch")]
    [SerializeField]
    private Transform tdmRebelsParent;
    private readonly List<Transform> _rebelsSpawnPoints = new();
    [SerializeField]
    private Transform tdmAiParent;
    private readonly List<Transform> _tdmAiSpawnPoints = new();

    [Tooltip("True to add the player to the default scene upon spawning.")]
    [SerializeField]
    private bool _addToDefaultScene = true;

    private void Awake()
    {
        Instance = this;

        foreach (Transform child in ffaSpawnParent)
        {
            _ffaSpawnPoints.Add(child);
        }
        
        foreach (Transform child in tdmRebelsParent)
        {
            _rebelsSpawnPoints.Add(child);
        }
        
        foreach (Transform child in tdmAiParent)
        {
            _tdmAiSpawnPoints.Add(child);
        }
    }

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

        // Hide the loading screen for the client(s)
        // Only hide for non-server clients
        foreach (var kvp in ServerManager.Clients)
        {
            NetworkConnection conn = kvp.Value;
            if (!conn.IsLocalClient) continue; // Only hide for the local client
            HideLoadingObservers();
        }
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
        if (_defaultPlayerPrefab == null)
        {
            Debug.LogWarning("Default Player Prefab is not set in the PlayerSpawner.");
            return;
        }

        // Late joiner: wait for team selection before spawning
        if (LobbyManager.Instance != null && LobbyManager.Instance.IsPendingLateJoiner(conn.ClientId))
        {
            Debug.Log($"[Spawner] ClientId={conn.ClientId} is a pending late joiner. Waiting for team selection.");
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

        NetworkObject playerPrefab;
        if (LobbyData.PlayerCharacters.TryGetValue(conn.ClientId, out NetworkObject customPrefab) && customPrefab != null)
        {
            playerPrefab = customPrefab;
            Debug.Log($"[Spawner] ClientId={conn.ClientId} using selected player prefab.");
        }
        else
        {
            playerPrefab = _defaultPlayerPrefab;
            Debug.Log($"[Spawner] ClientId={conn.ClientId} using default player prefab (custom was {(LobbyData.PlayerCharacters.ContainsKey(conn.ClientId) ? "null" : "not set")}).");
        }
        
        // Spawn the player
        NetworkObject playerInstance = base.NetworkManager.GetPooledInstantiated(playerPrefab, true);
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
    }

    private Transform GetSpawnPoint(Team team)
    {
        List<Transform> availableSpawnPoints;

        if (LobbyData.ResolvedGameMode == GameMode.TeamDeathmatch)
        {
            if (team == Team.Rebels && _rebelsSpawnPoints.Count > 0)
                availableSpawnPoints = _rebelsSpawnPoints;
            else if (team == Team.AI && _tdmAiSpawnPoints.Count > 0)
                availableSpawnPoints = _tdmAiSpawnPoints;
            else
                availableSpawnPoints = _ffaSpawnPoints; // Fallback
        }
        else
        {
            availableSpawnPoints = _ffaSpawnPoints;
        }

        if (availableSpawnPoints == null || availableSpawnPoints.Count == 0)
        {
            return transform;
        }
        
        Transform spawnPoint = null;
        var players = PlayerManager.Instance != null ? PlayerManager.Instance.players : new Dictionary<int, PlayerManager.Player>();
        while (availableSpawnPoints.Count > 0)
        {
            var randomIndex = Random.Range(0, availableSpawnPoints.Count);
            spawnPoint = availableSpawnPoints[randomIndex];
            try
            {
                var occupied = players.Values.ToList()
                    .Where(p => !p.stats.isRespawning.Value)
                    .Any(p =>
                    {
                        var distance = Vector3.Distance(p.playerObject.transform.position, spawnPoint.position);
                        return distance < 2f;
                    });

                if (!occupied)
                {
                    break; // Found an unoccupied spawn point
                }

                availableSpawnPoints.RemoveAt(randomIndex); // Remove occupied spawn point and try again
            }
            catch (Exception e)
            {
                break; // If there's an error checking occupancy, just use this spawn point
            }
        }

        return spawnPoint;
    }

    [ObserversRpc]
    private void HideLoadingObservers()
    {
        StartCoroutine(HideLoadingScreenWithDelay());
    }

    private IEnumerator HideLoadingScreenWithDelay()
    {
        yield return new WaitForSeconds(0.5f);

        if (LoadingManager.Instance != null)
            LoadingManager.Instance.Hide();
    }

    [Server]
    public void SpawnSinglePlayer(NetworkConnection conn)
    {
        if (HasSpawnedPlayer(conn))
            return;
        SpawnPlayer(conn);
    }
}