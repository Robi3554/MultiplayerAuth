using UnityEngine;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using FishNet.Transporting;

/// <summary>
/// Handles network connection setup in the Lobby scene.
/// Spawns the LobbyManager at runtime so FishNet properly replicates it to clients.
/// </summary>
public class LobbyBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private ushort defaultPort = 7777;

    [Header("Lobby Manager (Prefab)")]
    [Tooltip("Drag the LobbyManager prefab here. It will be spawned on the server at runtime.")]
    [SerializeField] private NetworkObject lobbyManagerPrefab;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = FindFirstObjectByType<NetworkManager>();
    }

    private void Start()
    {
        // Clear any stale lobby data from a previous game session
        LobbyData.Clear();

        if (networkManager == null)
        {
            Debug.LogError("[LobbyBootstrap] NetworkManager not found!");
            return;
        }

        // If already connected (returning from game), skip
        if (networkManager.IsClientStarted || networkManager.IsServerStarted)
        {
            Debug.Log("[LobbyBootstrap] Already connected. Skipping.");
            return;
        }

        // Listen for server start so we can spawn the LobbyManager
        networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;

        // Listen for client connection state
        networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;

        InitializeConnection();
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
            networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }
    }

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // Server just started — spawn the LobbyManager
            if (lobbyManagerPrefab != null)
            {
                NetworkObject instance = Instantiate(lobbyManagerPrefab);
                instance.SetIsGlobal(true);
                networkManager.ServerManager.Spawn(instance);
                Debug.Log("[LobbyBootstrap] Spawned LobbyManager on server (global).");
            }
            else
            {
                Debug.LogError("[LobbyBootstrap] LobbyManager prefab is not assigned!");
            }
        }
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        Debug.Log($"[LobbyBootstrap] Client connection state: {args.ConnectionState}");
    }

    private void InitializeConnection()
    {
        Tugboat tugboat = networkManager.GetComponent<Tugboat>();
        if (tugboat == null)
        {
            Debug.LogError("[LobbyBootstrap] Tugboat transport not found!");
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.LogError("[LobbyBootstrap] WebGL build detected but current transport is Tugboat (UDP). Browsers cannot use UDP sockets. Configure a WebSocket/WebRTC transport for WebGL clients.");
#endif

        string address = string.IsNullOrWhiteSpace(ConnectionInfo.IpAddress) ? "localhost" : ConnectionInfo.IpAddress;
        tugboat.SetPort(defaultPort);

        Debug.Log($"[LobbyBootstrap] Address={address}, Port={defaultPort}");

#if UNITY_EDITOR
        tugboat.SetClientAddress(address);
        if (ParrelSync.ClonesManager.IsClone())
        {
            Debug.Log("[LobbyBootstrap] ParrelSync clone → CLIENT.");
            networkManager.ClientManager.StartConnection();
        }
        else
        {
            // If address is NOT localhost, connect as client only (remote dedicated server)
            if (address != "localhost")
            {
                Debug.Log($"[LobbyBootstrap] ParrelSync original → CLIENT (remote server: {address}).");
                networkManager.ClientManager.StartConnection();
            }
            else
            {
                Debug.Log("[LobbyBootstrap] ParrelSync original → HOST (localhost).");
                networkManager.ServerManager.StartConnection();
                networkManager.ClientManager.StartConnection();
            }
        }

#elif DEDICATED_SERVER
        // Bind to all interfaces so external clients can connect
        tugboat.SetServerBindAddress("0.0.0.0", IPAddressType.IPv4);
        Debug.Log("[LobbyBootstrap] Starting Dedicated Server on 0.0.0.0:" + defaultPort);
        networkManager.ServerManager.StartConnection();

#elif CLIENT
        tugboat.SetClientAddress(address);
        Debug.Log($"[LobbyBootstrap] Starting Client → {address}:{defaultPort}");
        networkManager.ClientManager.StartConnection();

#else
        tugboat.SetClientAddress(address);
        if (address == "localhost")
            networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection();
#endif
    }
}
