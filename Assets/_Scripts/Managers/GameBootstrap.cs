using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using System;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private string defaultAddress = "localhost";
    [SerializeField] private ushort defaultPort = 7777;

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
        Multipass multipass = networkManager.GetComponent<Multipass>();
        Tugboat tugboat = networkManager.GetComponent<Tugboat>();

        if (multipass == null || tugboat == null)
        {
            Debug.LogError("[Bootstrap] Multipass or Tugboat transport not found on NetworkManager!");
            return;
        }

        string address = string.IsNullOrWhiteSpace(ConnectionInfo.IpAddress) ? "localhost" : ConnectionInfo.IpAddress;
        tugboat.SetClientAddress(address);
        tugboat.SetPort(defaultPort);

        // Multipass must be the active transport; set which child transport the client uses
        networkManager.TransportManager.Transport = multipass;
        multipass.SetClientTransport<Tugboat>();

        Debug.Log($"[Bootstrap] Using Address={address}, Port={defaultPort}");

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
        tugboat.SetServerBindAddress("0.0.0.0", IPAddressType.IPv4);
        Debug.Log("[Bootstrap] Starting Dedicated Server on 0.0.0.0:" + defaultPort);
        networkManager.ServerManager.StartConnection();

#elif CLIENT
        // --- BUILD MODE: Client only ---
        Debug.Log($"[Bootstrap] Starting Client. Connecting to {address}:{defaultPort}");
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

        tugboat.SetServerBindAddress(serverAddress, IPAddressType.IPv4);

        Debug.Log("[Bootstrap] Starting Dedicated Server.");
        networkManager.ServerManager.StartConnection();
        Debug.Log("[Bootstrap] Starting Host (default).");

#endif
    }
}
