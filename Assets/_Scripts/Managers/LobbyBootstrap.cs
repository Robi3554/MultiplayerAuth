using UnityEngine;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using FishNet.Transporting.Bayou;
using FishNet.Transporting.Multipass;

/// <summary>
/// Handles network connection setup in the Lobby scene.
/// Spawns the LobbyManager at runtime so FishNet properly replicates it to clients.
///
/// Transport strategy:
///   • Multipass (Tugboat + Bayou): server accepts both UDP and WebSocket clients.
///   • Standalone Tugboat: desktop-only builds.
///   • Standalone Bayou:   WebGL-only builds.
/// WebGL clients automatically select Bayou when Multipass is active.
/// </summary>
public class LobbyBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private ushort defaultPort = 7777;
    [SerializeField] private ushort webSocketPort = 7770;

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

    /// <summary>
    /// Configures transports and starts server/client connections based on build target.
    /// Supports Multipass (Tugboat + Bayou), standalone Tugboat, or standalone Bayou.
    /// </summary>
    private void InitializeConnection()
    {
        string address = string.IsNullOrWhiteSpace(ConnectionInfo.IpAddress) ? "localhost" : ConnectionInfo.IpAddress;

        // ── Detect available transports ──
        Multipass multipass = networkManager.GetComponent<Multipass>();
        Tugboat tugboat = networkManager.GetComponent<Tugboat>();
        Bayou bayou = networkManager.GetComponent<Bayou>();

        if (multipass != null)
        {
            // Multipass: configure each child transport independently
            if (tugboat != null)
            {
                tugboat.SetClientAddress(address);
                tugboat.SetPort(defaultPort);
            }
            if (bayou != null)
            {
                bayou.SetClientAddress(address);
                bayou.SetPort(webSocketPort);
            }

            // Tell Multipass which transport this client should use
#if UNITY_WEBGL && !UNITY_EDITOR
            multipass.SetClientTransport<Bayou>();
            Debug.Log($"[LobbyBootstrap] Multipass → WebGL client using Bayou (ws) on port {webSocketPort}");
#else
            if (tugboat != null)
                multipass.SetClientTransport<Tugboat>();
            else if (bayou != null)
                multipass.SetClientTransport<Bayou>();
            Debug.Log($"[LobbyBootstrap] Multipass → Desktop client using Tugboat on port {defaultPort}");
#endif
        }
        else if (bayou != null)
        {
            // Standalone Bayou (WebGL-only builds or WebSocket-only server)
            bayou.SetClientAddress(address);
            bayou.SetPort(webSocketPort);
            Debug.Log($"[LobbyBootstrap] Standalone Bayou, Address={address}, Port={webSocketPort}");
        }
        else if (tugboat != null)
        {
            // Standalone Tugboat (desktop-only, original behaviour)
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.LogError("[LobbyBootstrap] WebGL build but only Tugboat (UDP) is available. " +
                           "Add Bayou (+ Multipass) to the NetworkManager for browser support.");
#endif
            tugboat.SetClientAddress(address);
            tugboat.SetPort(defaultPort);
            Debug.Log($"[LobbyBootstrap] Standalone Tugboat, Address={address}, Port={defaultPort}");
        }
        else
        {
            Debug.LogError("[LobbyBootstrap] No supported transport found on NetworkManager!");
            return;
        }

        // ── Start connections based on build context ──
#if UNITY_EDITOR
        if (ParrelSync.ClonesManager.IsClone())
        {
            Debug.Log("[LobbyBootstrap] ParrelSync clone → CLIENT.");
            networkManager.ClientManager.StartConnection();
        }
        else
        {
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
        // Bind Tugboat to all interfaces; Bayou's SetServerBindAddress is a no-op (binds to all automatically)
        if (tugboat != null)
            tugboat.SetServerBindAddress("0.0.0.0", IPAddressType.IPv4);
        Debug.Log($"[LobbyBootstrap] Starting Dedicated Server (UDP:{defaultPort} WS:{webSocketPort})");
        networkManager.ServerManager.StartConnection();

#elif CLIENT
        Debug.Log($"[LobbyBootstrap] Starting Client → {address}");
        networkManager.ClientManager.StartConnection();

#else
        if (address == "localhost")
            networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection();
#endif
    }
}
