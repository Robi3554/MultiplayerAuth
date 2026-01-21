using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using System;
using FishNet.Transporting;

public class NetworkBootstrap : MonoBehaviour
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

        Console.WriteLine("-----------------------------------------");
        Console.WriteLine($"This is the dedicated server. The IP will be: 193.226.15.26");
        string serverAddress = "193.226.15.26";

        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine($"Renter the server IP address to bind to:");
        }

        tugboat.SetServerBindAddress(serverAddress, IPAddressType.IPv4);

        Debug.Log("[Bootstrap] Starting Dedicated Server.");
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
