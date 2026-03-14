using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using FishNet;
using FishNet.Managing;

public class PauseMenuManager : MonoBehaviour 
{
    [SerializeField] private Canvas _pauseMenuCanvas;
    [SerializeField] private string _menuSceneName = "WelcomeScreen";
    
    private PredictionMoving _playerMovement;
    private PlayerInput _playerInput;
    private bool _isPaused = false;

    private void Start()
    {
        if (_pauseMenuCanvas != null)
            _pauseMenuCanvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isPaused)
                Resume();
            else
                Pause();
        }
    }

    private IEnumerator FindLocalPlayerDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        
        PredictionMoving[] allPlayers = FindObjectsByType<PredictionMoving>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player.IsOwner) 
            {
                _playerMovement = player;
                _playerInput = player.GetComponent<PlayerInput>();
                break;
            }
        }
        
        if (_playerMovement == null)
        {
            Debug.LogWarning("Local player not found!");
        }
    }

    public void Pause()
    {
        // Find player if not found yet
        if (_playerMovement == null || _playerInput == null)
        {
            StartCoroutine(FindLocalPlayerAndPause());
            return;
        }

        _isPaused = true;
        _pauseMenuCanvas.gameObject.SetActive(true);
        _playerInput.SwitchCurrentActionMap("UI");
        Time.timeScale = 0f; // Optional: freeze game
    }

    private IEnumerator FindLocalPlayerAndPause()
    {
        yield return FindLocalPlayerDelayed();
        
        if (_playerMovement != null && _playerInput != null)
        {
            Pause();
        }
    }

    public void Resume()
    {
        if (_playerInput == null) return;

        _isPaused = false;
        _pauseMenuCanvas.gameObject.SetActive(false);
        _playerInput.SwitchCurrentActionMap("Gameplay");
        Time.timeScale = 1f; // Optional: unfreeze game
    }

    public void ChangeInput()
    {
        if (_playerMovement != null)
        {
            _playerMovement.ToggleInputMode();
            Debug.Log($"Input mode changed to: {(_playerMovement.IsJoystickMode ? "Joystick" : "Mouse & Keyboard")}");
        }
        else
        {
            Debug.LogWarning("Cannot change input: Local player not found!");
            StartCoroutine(FindLocalPlayerDelayed());
        }
    }

    public void LeaveMatch()
    {
        Time.timeScale = 1f; // Reset time scale before leaving
        
        NetworkManager networkManager = InstanceFinder.NetworkManager;
        
        if (networkManager != null)
        {
            // Stop server if we're hosting
            if (networkManager.IsServerStarted)
            {
                networkManager.ServerManager.StopConnection(true);
            }
            
            // Stop client connection
            if (networkManager.IsClientStarted)
            {
                networkManager.ClientManager.StopConnection();
            }
            
            // Destroy the persistent NetworkManager so a fresh one is created when rejoining
            Destroy(networkManager.gameObject);
        }
        
        // Load scene immediately - the NetworkManager destruction will complete
        SceneManager.LoadScene(_menuSceneName);
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}