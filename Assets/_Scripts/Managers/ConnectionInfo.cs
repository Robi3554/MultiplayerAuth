using UnityEngine;

public class ConnectionInfo : MonoBehaviour
{
    // A static variable can be accessed from any other script.
    public static string IpAddress;

    // Use a singleton pattern to ensure only one instance exists.
    public static ConnectionInfo Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}