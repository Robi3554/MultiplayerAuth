using UnityEngine;
using FishNet.Managing;
using System.Diagnostics;

[RequireComponent(typeof(NetworkManager))]
public class NetworkCommandLineArgs : MonoBehaviour
{
    private NetworkManager networkManager;

    private void Start()
    {
        networkManager = GetComponent<NetworkManager>();

        ushort? port = GetPortFromCommandLine();
        if (port.HasValue)
        {
            networkManager.TransportManager.Transport.SetPort(port.Value);
            networkManager.Log($"Port set to {port} via command line.");
        }
        else
        {
            networkManager.Log("No valid port found in command line args. Using default.");
        }

        StartDedicatedServer();
    }

    [Conditional("UNITY_SERVER")]
    private void StartDedicatedServer()
    {
        networkManager.ServerManager.StartConnection();
    }

    private ushort? GetPortFromCommandLine()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "-port") && i + 1 < args.Length)
            {
                if (ushort.TryParse(args[i + 1], out ushort port))
                    return port;
            }
        }
        return null;
    }
}