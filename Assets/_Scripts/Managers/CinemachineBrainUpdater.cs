using UnityEngine;
using Unity.Cinemachine;

public class NetworkedCinemachineUpdater : MonoBehaviour
{
    private CinemachineBrain _brain;

    private void Awake()
    {
        _brain = GetComponent<CinemachineBrain>();
        if (_brain == null)
            Debug.LogError("CinemachineBrain not found on this GameObject.");
    }

    /// <summary>
    /// Manually updates Cinemachine to stay in sync with FishNet ticks.
    /// </summary>
    public void UpdateCineMachine()
    {
        if (_brain != null)
            _brain.ManualUpdate();
    }
}
