using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using System;
using System.Collections;
using FishNet.Transporting;

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
        // Check if there's already a NetworkManager from a previous session (shouldn't happen now, but safety check)
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        // If arriving from lobby (already connected via FishNet scene transition), skip connection setup
        if (networkManager.IsClientStarted || networkManager.IsServerStarted)
        {
            Debug.Log("[Bootstrap] Already connected (from lobby). Skipping connection setup.");
            return;
        }
        
        // If already connected (stale state), stop connections first
        if (networkManager.IsClientStarted || networkManager.IsServerStarted)
        {
            Debug.LogWarning("[Bootstrap] Found stale connections. Restarting...");
            StartCoroutine(RestartConnectionsCoroutine());
            return;
        }
        
        InitializeConnection();
    }

    private IEnumerator RestartConnectionsCoroutine()
    {
        if (networkManager.IsClientStarted)
            networkManager.ClientManager.StopConnection();
        if (networkManager.IsServerStarted)
            networkManager.ServerManager.StopConnection(true);
            
        // Wait for cleanup
        yield return new WaitForSeconds(0.3f);
        
        InitializeConnection();
    }

    private void InitializeConnection()
    {
        // Transport
        Tugboat tugboat = networkManager.GetComponent<Tugboat>();
        if (tugboat == null)
        {
            Debug.LogError("No Tugboat transport found on NetworkManager!");
            return;
        }
        
        string address = string.IsNullOrWhiteSpace(ConnectionInfo.IpAddress) ? "localhost" : ConnectionInfo.IpAddress;

        tugboat.SetClientAddress(address);
        tugboat.SetPort(defaultPort);

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
