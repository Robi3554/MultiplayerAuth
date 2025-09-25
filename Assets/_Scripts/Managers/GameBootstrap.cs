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
/*
 * using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using TMPro;
using UnityEngine.UI;

public class NetworkBootstrap : MonoBehaviour
{
    private NetworkManager _networkManager;
    private Tugboat _tugboat;

    [Header("UI References")]
    [SerializeField] private GameObject _connectionPanel;
    [SerializeField] private TMP_InputField _addressInput;
    [SerializeField] private Button _connectButton;

    private void Awake()
    {
        _networkManager = FindFirstObjectByType<NetworkManager>();
        _tugboat = _networkManager.GetComponent<Tugboat>();
    }

    private void Start()
    {
        #if UNITY_EDITOR
            if (ParrelSync.ClonesManager.IsClone())
                _networkManager.ClientManager.StartConnection();
            else
            {
                _networkManager.ServerManager.StartConnection();
                _networkManager.ClientManager.StartConnection();
            }
        #elif DEDICATED_SERVER
            _networkManager.ServerManager.StartConnection();
        #elif CLIENT
            // Subscribe to the correct connection events
            _networkManager.ClientManager.OnAuthenticated += OnAuthenticated; // Corrected event
            _networkManager.ClientManager.OnClientStopped += OnClientStopped;

            _connectionPanel.SetActive(true);
            _connectButton.onClick.AddListener(OnConnectClicked);
        #else
            _networkManager.ServerManager.StartConnection();
            _networkManager.ClientManager.StartConnection();
        #endif
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (_networkManager != null)
        {
            _networkManager.ClientManager.OnAuthenticated -= OnAuthenticated; // Corrected event
            _networkManager.ClientManager.OnClientTimeOut -= OnClientTimeOut;
        }
    }

    // Called by the UI button click
    public void OnConnectClicked()
    {
        string address = string.IsNullOrWhiteSpace(_addressInput.text) ? "localhost" : _addressInput.text;
        _tugboat.SetClientAddress(address);
        _networkManager.ClientManager.StartConnection();
    }

    // This event runs when the client successfully connects and is authenticated
    private void OnAuthenticated()
    {
        Debug.Log("Client successfully authenticated.");
        _connectionPanel.SetActive(false);
    }

    // This event runs if the client disconnects or fails to connect
    private void OnClientTimeOut()
    {
        Debug.Log("Client disconnected or failed to connect.");
        if (_connectionPanel != null)
        {
            _connectionPanel.SetActive(true);
        }
    }
}
*/