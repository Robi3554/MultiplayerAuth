using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour // Regular MonoBehaviour, not NetworkBehaviour
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
        // Listen for ESC key globally
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
            if (player.IsOwner) // Safe to use here because we're not in Start()
            {
                _playerMovement = player;
                _playerInput = player.GetComponent<PlayerInput>();
                Debug.Log("Local player found for pause menu!");
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