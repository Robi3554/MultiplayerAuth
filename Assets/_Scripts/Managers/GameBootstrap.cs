using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using FishNet.Transporting.Bayou;
using FishNet.Transporting.Multipass;
using System;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private string defaultAddress = "localhost";
    [SerializeField] private ushort defaultPort = 7777;
    [SerializeField] private ushort webSocketPort = 7770;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = FindFirstObjectByType<NetworkManager>();
    }

    private void Start()
    {
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        // If already connected (e.g. arriving from LobbyScene via FishNet scene management),
        // do NOT restart connections — the lobby already established them.
        if (networkManager.IsClientStarted || networkManager.IsServerStarted)
        {
            Debug.Log("[Bootstrap] Connections already active (from lobby). Skipping bootstrap.");
            return;
        }

        InitializeConnection();
    }

    private void InitializeConnection()
    {
        string address = string.IsNullOrWhiteSpace(ConnectionInfo.IpAddress) ? "localhost" : ConnectionInfo.IpAddress;

        // ── Detect available transports ──
        Multipass multipass = networkManager.GetComponent<Multipass>();
        Tugboat tugboat = networkManager.GetComponent<Tugboat>();
        Bayou bayou = networkManager.GetComponent<Bayou>();

        if (multipass != null)
        {
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

#if UNITY_WEBGL && !UNITY_EDITOR
            multipass.SetClientTransport<Bayou>();
            Debug.Log($"[Bootstrap] Multipass → WebGL client using Bayou (ws) on port {webSocketPort}");
#else
            if (tugboat != null)
                multipass.SetClientTransport<Tugboat>();
            else if (bayou != null)
                multipass.SetClientTransport<Bayou>();
            Debug.Log($"[Bootstrap] Multipass → Desktop client using Tugboat on port {defaultPort}");
#endif
        }
        else if (bayou != null)
        {
            bayou.SetClientAddress(address);
            bayou.SetPort(webSocketPort);
            Debug.Log($"[Bootstrap] Standalone Bayou, Address={address}, Port={webSocketPort}");
        }
        else if (tugboat != null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.LogError("[Bootstrap] WebGL build but only Tugboat (UDP) is available. " +
                           "Add Bayou (+ Multipass) to the NetworkManager for browser support.");
#endif
            tugboat.SetClientAddress(address);
            tugboat.SetPort(defaultPort);
            Debug.Log($"[Bootstrap] Standalone Tugboat, Address={address}, Port={defaultPort}");
        }
        else
        {
            Debug.LogError("[Bootstrap] No supported transport found on NetworkManager!");
            return;
        }

        Debug.Log($"[Bootstrap] Using Address={address}");

#if UNITY_EDITOR
        // --- EDITOR MODE (ParrelSync) ---
        if (ParrelSync.ClonesManager.IsClone())
        {
            Debug.Log("[ParrelSync] Starting as CLIENT (clone).");
            networkManager.ClientManager.StartConnection();
        }
        else
        {
            Debug.Log("[ParrelSync] Starting as HOST (original).");
            if (address == "localhost")
            {
                networkManager.ServerManager.StartConnection();
            }

            networkManager.ClientManager.StartConnection();
        }

#elif DEDICATED_SERVER
        // --- BUILD MODE: Dedicated Server ---
        if (tugboat != null)
            tugboat.SetServerBindAddress("0.0.0.0", IPAddressType.IPv4);
        Debug.Log($"[Bootstrap] Starting Dedicated Server (UDP:{defaultPort} WS:{webSocketPort})");
        networkManager.ServerManager.StartConnection();

#elif CLIENT
        // --- BUILD MODE: Client only ---
        Debug.Log($"[Bootstrap] Starting Client. Connecting to {address}");
        networkManager.ClientManager.StartConnection();

#else
        // --- BUILD MODE: Default Host (no defines) ---
        Console.WriteLine("-----------------------------------------");
        Console.WriteLine($"Enter the server IP address to bind to:");
        string serverAddress = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine($"Renter the server IP address to bind to:");
        }

        if (tugboat != null)
            tugboat.SetServerBindAddress(serverAddress, IPAddressType.IPv4);

        Debug.Log("[Bootstrap] Starting Dedicated Server.");
        networkManager.ServerManager.StartConnection();
        Debug.Log("[Bootstrap] Starting Host (default).");

#endif
    }
}
