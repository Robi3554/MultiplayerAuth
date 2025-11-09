#if CLIENT || UNITY_EDITOR

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _addressInput;
    [SerializeField] private TMP_InputField _usernameInput;
    [SerializeField] private string _gameSceneName = "GameScene";

    public void OnConnectClicked()
    {
        // Save the IP address to our persistent data holder.
        ConnectionInfo.IpAddress = _addressInput.text;
        ConnectionInfo.username = _usernameInput.text;

        // Load the game scene.
        SceneManager.LoadScene(_gameSceneName);
    }
}

#endif