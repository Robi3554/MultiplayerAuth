using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _addressInput;
    [SerializeField] private string _gameSceneName = "SampleScene";

    public void OnConnectClicked()
    {
        // Save the IP address to our persistent data holder.
        ConnectionInfo.IpAddress = _addressInput.text;

        // Load the game scene.
        SceneManager.LoadScene(_gameSceneName);
    }
}