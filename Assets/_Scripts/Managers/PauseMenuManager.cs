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

    private void Start()
    {
        _pauseMenuCanvas.gameObject.SetActive(false);
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

    public void LeaveMatch()
    {
        SceneManager.LoadScene(_menuSceneName);
    }
    
    public void QuitGame()
    {
        # if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        # else
            Application.Quit();
        # endif
    }
}
