using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using FishNet.Transporting;

/// <summary>
/// Handles network connection setup in the Lobby scene.
/// Mirrors the logic from GameBootstrap but for the lobby flow.
/// </summary>
public class LobbyBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
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
            Debug.LogError("[LobbyBootstrap] NetworkManager not found!");
            return;
        }

        // If already connected (returning from game), skip
        if (networkManager.IsClientStarted || networkManager.IsServerStarted)
        {
            Debug.Log("[LobbyBootstrap] Already connected. Skipping.");
            return;
        }

        InitializeConnection();
    }

    private void InitializeConnection()
    {
        Tugboat tugboat = networkManager.GetComponent<Tugboat>();
        if (tugboat == null)
        {
            Debug.LogError("[LobbyBootstrap] Tugboat transport not found!");
            return;
        }

        string address = string.IsNullOrWhiteSpace(ConnectionInfo.IpAddress) ? "localhost" : ConnectionInfo.IpAddress;
        tugboat.SetClientAddress(address);
        tugboat.SetPort(defaultPort);

        Debug.Log($"[LobbyBootstrap] Address={address}, Port={defaultPort}");

#if UNITY_EDITOR
        if (ParrelSync.ClonesManager.IsClone())
        {
            Debug.Log("[LobbyBootstrap] ParrelSync clone → CLIENT.");
            networkManager.ClientManager.StartConnection();
        }
        else
        {
            Debug.Log("[LobbyBootstrap] ParrelSync original → HOST.");
            if (address == "localhost")
                networkManager.ServerManager.StartConnection();
            networkManager.ClientManager.StartConnection();
        }

#elif DEDICATED_SERVER
        tugboat.SetServerBindAddress("193.226.15.26", IPAddressType.IPv4);
        Debug.Log("[LobbyBootstrap] Starting Dedicated Server.");
        networkManager.ServerManager.StartConnection();

#elif CLIENT
        Debug.Log($"[LobbyBootstrap] Starting Client → {address}:{defaultPort}");
        networkManager.ClientManager.StartConnection();

#else
        if (address == "localhost")
            networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection();
#endif
    }
}
