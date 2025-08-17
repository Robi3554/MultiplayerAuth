using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private string address = "127.0.0.1";
    [SerializeField] private ushort port = 7777;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = FindFirstObjectByType<NetworkManager>();
    }

    private void Start()
    {
#if UNITY_EDITOR
        // EDITOR MODE (ParrelSync)

        Tugboat tugboat = networkManager.GetComponent<Tugboat>();
        if (tugboat == null)
        {
            Debug.LogError("No Tugboat transport found on NetworkManager!");
            return;
        }

        if (ParrelSync.ClonesManager.IsClone())
        {
            Debug.Log("[ParrelSync] Starting as CLIENT (clone).");
            tugboat.StartConnection(false); // client
        }
        else
        {
            Debug.Log("[ParrelSync] Starting as HOST (original).");
            tugboat.StartConnection(true);  // server
            tugboat.StartConnection(false); // client
        }

#elif DEDICATED_SERVER
        // BUILD MODE: Dedicated Server

        Debug.Log("Starting in Dedicated Server mode");
        networkManager.ServerManager.StartConnection();


#elif CLIENT
        // BUILD MODE: Client Only

        Debug.Log($"Starting in Client mode. Connecting to {address}:{port}");
        networkManager.ClientManager.StartConnection(address, port);

#else
        // BUILD MODE: Default Host (if no defines set)

        Debug.Log("Starting in Host mode (no define symbols).");
        networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection(address, port);

#endif
    }
}
