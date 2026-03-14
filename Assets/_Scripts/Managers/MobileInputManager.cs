using UnityEngine;

/// <summary>
/// Auto-detects mobile platform and toggles the mobile controls canvas.
/// Place on a persistent scene object (e.g. alongside PauseMenuManager).
/// After the local player spawns, call ConfigureLocalPlayer().
/// </summary>
public class MobileInputManager : MonoBehaviour
{
    [SerializeField] private GameObject mobileControlsCanvas;

    public static MobileInputManager Instance { get; private set; }
    public static bool IsMobile { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

#if UNITY_EDITOR
        // In editor, default to PC unless you override via Inspector toggle
        IsMobile = false;
#else
        IsMobile = Application.isMobilePlatform;
#endif

        if (mobileControlsCanvas != null)
            mobileControlsCanvas.SetActive(IsMobile);
    }

    /// <summary>
    /// Called once after the local player prefab is spawned and ready.
    /// Sets input mode to joystick on mobile so the character faces movement direction.
    /// </summary>
    public void ConfigureLocalPlayer(PredictionMoving playerMovement)
    {
        if (playerMovement == null) return;

        playerMovement.SetInputMode(IsMobile);

        if (mobileControlsCanvas != null)
            mobileControlsCanvas.SetActive(IsMobile);
    }

    /// <summary>
    /// Runtime toggle (e.g. from pause menu) so devs/testers can switch on PC.
    /// </summary>
    public void SetMobileMode(bool mobile)
    {
        IsMobile = mobile;

        if (mobileControlsCanvas != null)
            mobileControlsCanvas.SetActive(mobile);
    }
}
