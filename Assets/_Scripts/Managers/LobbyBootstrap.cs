using UnityEngine;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using FishNet.Transporting;
#if UNITY_WEBGL && !UNITY_EDITOR
using FishNet.Transporting.Bayou;
#else
using FishNet.Transporting.Multipass;
#endif

/// <summary>
/// Handles network connection setup in the Lobby scene.
/// Spawns the LobbyManager at runtime so FishNet properly replicates it to clients.
/// </summary>
public class LobbyBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private ushort defaultPort = 7777;
    [SerializeField] private ushort webGLPort = 7770;

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
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: use Bayou (WebSocket) transport
        Bayou bayou = networkManager.GetComponent<Bayou>();
        if (bayou == null)
        {
            Debug.LogError("[LobbyBootstrap] Bayou transport not found on NetworkManager! Add the Bayou component for WebGL builds.");
            return;
        }

        string address = string.IsNullOrWhiteSpace(ConnectionInfo.IpAddress) ? "localhost" : ConnectionInfo.IpAddress;
        bayou.SetClientAddress(address);
        bayou.SetPort(webGLPort);
        networkManager.TransportManager.Transport = bayou;

        Debug.Log($"[LobbyBootstrap] WebGL → Bayou client connecting to {address}:{webGLPort}");
        networkManager.ClientManager.StartConnection();
#else
        Tugboat tugboat = networkManager.GetComponent<Tugboat>();
        if (tugboat == null)
        {
            Debug.LogError("[LobbyBootstrap] Tugboat transport not found!");
            return;
        }

        string address = string.IsNullOrWhiteSpace(ConnectionInfo.IpAddress) ? "localhost" : ConnectionInfo.IpAddress;
        tugboat.SetPort(defaultPort);
        networkManager.TransportManager.Transport = tugboat;

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
                SetupMultipass(tugboat);
                networkManager.ServerManager.StartConnection();
                networkManager.ClientManager.StartConnection();
            }
        }

#elif DEDICATED_SERVER
        // Bind to all interfaces so external clients can connect
        tugboat.SetServerBindAddress("0.0.0.0", IPAddressType.IPv4);
        SetupMultipass(tugboat);
        Debug.Log("[LobbyBootstrap] Starting Dedicated Server on 0.0.0.0:" + defaultPort);
        networkManager.ServerManager.StartConnection();

#elif CLIENT
        tugboat.SetClientAddress(address);
        Debug.Log($"[LobbyBootstrap] Starting Client → {address}:{defaultPort}");
        networkManager.ClientManager.StartConnection();

#else
        tugboat.SetClientAddress(address);
        if (address == "localhost")
        {
            SetupMultipass(tugboat);
            networkManager.ServerManager.StartConnection();
        }
        networkManager.ClientManager.StartConnection();
#endif
#endif // !UNITY_WEBGL
    }

#if !(UNITY_WEBGL && !UNITY_EDITOR)
    /// <summary>
    /// Configures Multipass to wrap Tugboat + Bayou so the server accepts both UDP and WebSocket clients.
    /// Sets Tugboat as the client transport for non-WebGL builds.
    /// </summary>
    private void SetupMultipass(Tugboat tugboat)
    {
        var multipass = networkManager.GetComponent<Multipass>();
        if (multipass == null)
        {
            Debug.LogWarning("[LobbyBootstrap] Multipass not found on NetworkManager. Server will only accept Tugboat (UDP) connections.");
            return;
        }

        multipass.SetClientTransport<Tugboat>();
        networkManager.TransportManager.Transport = multipass;
        Debug.Log($"[LobbyBootstrap] Multipass enabled — Tugboat:{defaultPort} + Bayou:{webGLPort}");
    }
#endif
}
