#if CLIENT || UNITY_EDITOR

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Text.RegularExpressions;

public class MainMenuController : MonoBehaviour
{
    private const int MaxUsernameLength = 20;
    
    [SerializeField] private TMP_InputField _addressInput;
    [SerializeField] private TMP_InputField _usernameInput;
    [SerializeField] private string _gameSceneName = "LobbyScene";
    [SerializeField] private string _defaultAddress = "193.226.15.26";

    private void Start()
    {
        // Set character limit on the input field
        if (_usernameInput != null)
        {
            _usernameInput.characterLimit = MaxUsernameLength;
            _usernameInput.onValueChanged.AddListener(OnUsernameInputChanged);
        }
    }

    private void OnDestroy()
    {
        if (_usernameInput != null)
        {
            _usernameInput.onValueChanged.RemoveListener(OnUsernameInputChanged);
        }
    }

    private void OnUsernameInputChanged(string newValue)
    {
        // Sanitize in real-time as the user types
        string sanitized = SanitizeUsername(newValue);
        if (sanitized != newValue)
        {
            _usernameInput.text = sanitized;
        }
    }

    /// <summary>
    /// Sanitizes a username to only allow alphanumeric characters, max 20 chars.
    /// </summary>
    public static string SanitizeUsername(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove all non-alphanumeric characters (letters and digits only)
        string sanitized = Regex.Replace(input, @"[^a-zA-Z0-9]", "");

        // Limit to max length
        if (sanitized.Length > MaxUsernameLength)
            sanitized = sanitized.Substring(0, MaxUsernameLength);

        return sanitized;
    }

    public void OnConnectClicked()
    {
        // Save the IP address to our persistent data holder.
        string enteredAddress = _addressInput != null ? _addressInput.text.Trim() : string.Empty;
        ConnectionInfo.IpAddress = string.IsNullOrWhiteSpace(enteredAddress)
            ? _defaultAddress
            : enteredAddress;
        ConnectionInfo.username = SanitizeUsername(_usernameInput.text);

        // Load the game scene.
        SceneManager.LoadScene(_gameSceneName);
    }

    public void OnLocalhostClicked()
    {
        ConnectionInfo.IpAddress = "localhost";
        ConnectionInfo.username = SanitizeUsername(_usernameInput.text);
        
        SceneManager.LoadScene(_gameSceneName);
    }
}

#endif