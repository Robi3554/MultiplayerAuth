using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using System;

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

        // Command line overrides
        (string cmdAddress, ushort? cmdPort) = ParseCommandLineArgs();
        string finalAddress = string.IsNullOrEmpty(cmdAddress) ? defaultAddress : cmdAddress;
        ushort finalPort = cmdPort ?? defaultPort;

        tugboat.SetClientAddress(finalAddress);
        tugboat.SetPort(finalPort);

        Debug.Log($"[Bootstrap] Using Address={finalAddress}, Port={finalPort}");

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
            if (defaultAddress == "localhost")
            {
                networkManager.ServerManager.StartConnection();    
            }
            
            networkManager.ClientManager.StartConnection();
        }

#elif DEDICATED_SERVER
        // --- BUILD MODE: Dedicated Server ---
        Debug.Log("[Bootstrap] Starting Dedicated Server.");
        networkManager.ServerManager.StartConnection();

#elif CLIENT
        // --- BUILD MODE: Client only ---
        Debug.Log($"[Bootstrap] Starting Client. Connecting to {finalAddress}:{finalPort}");
        networkManager.ClientManager.StartConnection();

#else
        // --- BUILD MODE: Default Host (no defines) ---
        Debug.Log("[Bootstrap] Starting Host (default).");
        networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection();

#endif
    }

    private (string, ushort?) ParseCommandLineArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        string address = null;
        ushort? port = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-address" && i + 1 < args.Length)
                address = args[i + 1];
            if (args[i] == "-port" && i + 1 < args.Length && ushort.TryParse(args[i + 1], out ushort p))
                port = p;
        }

        return (address, port);
    }
}
