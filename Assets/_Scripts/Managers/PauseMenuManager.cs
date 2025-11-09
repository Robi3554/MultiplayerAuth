using System;
using FishNet.Managing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [SerializeField] private Canvas _pauseMenuCanvas;
    [SerializeField] private string _menuSceneName = "WelcomeScreen";
    [SerializeField] private PlayerInput _playerInput;
    
    private PredictionMoving _playerMovement;

    private void Start()
    {
        _pauseMenuCanvas.gameObject.SetActive(false);
        
        StartCoroutine(FindLocalPlayerDelayed());
    }

    private System.Collections.IEnumerator FindLocalPlayerDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        
        PredictionMoving[] allPlayers = FindObjectsByType<PredictionMoving>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (!player.IsOwner) continue;
            
            _playerMovement = player;
            Debug.Log("Local player found for input switching!");
            break;
        }
        
        if (_playerMovement == null)
        {
            Debug.LogWarning("Local player not found! Input switching will not work.");
        }
    }
    
    private void OnEnable()
    {
        if (_playerMovement == null)
        {
            StartCoroutine(FindLocalPlayerDelayed());
        }
    }

    public void Pause()
    {
        _pauseMenuCanvas.gameObject.SetActive(true);
        _playerInput.SwitchCurrentActionMap("UI");
    }

    public void Resume()
    {
        _pauseMenuCanvas.gameObject.SetActive(false);
        _playerInput.SwitchCurrentActionMap("Gameplay");
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