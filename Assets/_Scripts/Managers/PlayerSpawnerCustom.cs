using FishNet.Connection;
using FishNet.Object;
using FishNet.Managing.Scened;
using UnityEngine;
using System.Collections.Generic; // Required for using Lists

public class PlayerSpawner : NetworkBehaviour
{
    [Header("Player Prefab")]
    [SerializeField]
    private NetworkObject _playerPrefab;

    [Header("Spawning")]
    [Tooltip("Points where players may spawn.")]
    [SerializeField]
    private List<Transform> _spawnPoints = new List<Transform>();

    [Tooltip("True to add the player to the default scene upon spawning.")]
    [SerializeField]
    private bool _addToDefaultScene = true;

    public override void OnStartServer()
    {
        base.OnStartServer();
        base.NetworkManager.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (base.NetworkManager != null)
            base.NetworkManager.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
    }

    private void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        // Only spawn a player object for the host's client portion.
        // This prevents a player from being spawned for a dedicated server.
        if (!conn.IsHost)
            return;

        // Check if a player prefab is assigned.
        if (_playerPrefab == null)
        {
            Debug.LogWarning("Player Prefab is not set in the PlayerSpawner.");
            return;
        }

        // Ensure there are spawn points available.
        if (_spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points configured in the PlayerSpawner.");
            return;
        }

        // --- Spawning Logic ---
        
        // 1. Choose a random spawn point.
        int spawnIndex = Random.Range(0, _spawnPoints.Count);
        Transform spawnTransform = _spawnPoints[spawnIndex];

        // 2. Spawn the player prefab.
        NetworkObject playerInstance = base.NetworkManager.GetPooledInstantiated(_playerPrefab, true);
        
        // 3. Set spawn position and rotation.
        playerInstance.transform.SetPositionAndRotation(spawnTransform.position, spawnTransform.rotation);
        
        // 4. Spawn the object on the network and give ownership to the client.
        base.ServerManager.Spawn(playerInstance, conn);

        // 5. Optionally, add the player to the DontDestroyOnLoad scene.
        if (_addToDefaultScene)
        {
            base.SceneManager.AddOwnerToDefaultScene(playerInstance);
        }
    }
}