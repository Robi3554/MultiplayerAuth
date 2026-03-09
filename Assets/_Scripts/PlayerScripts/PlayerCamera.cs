using UnityEngine;
using FishNet.Object;
using Unity.Cinemachine;
using System.Collections;

public class PlayerCameraSetter : NetworkBehaviour
{
    private CinemachineBrain m_Brain;
    private CinemachineCamera m_Camera;
    private Coroutine _setupCoroutine;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            _setupCoroutine = StartCoroutine(SetupCameraRoutine());
        }
    }

    private IEnumerator SetupCameraRoutine()
    {
        // Wait until both CinemachineBrain and CinemachineCamera are available in the scene.
        // During FishNet scene transitions they may not exist on the first frame.
        float timeout = 5f;
        float elapsed = 0f;

        while (m_Brain == null || m_Camera == null)
        {
            if (m_Brain == null)
                m_Brain = FindFirstObjectByType<CinemachineBrain>();
            if (m_Camera == null)
                m_Camera = FindFirstObjectByType<CinemachineCamera>();

            if (m_Brain != null && m_Camera != null)
                break;

            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
            {
                Debug.LogWarning("[PlayerCamera] Timed out waiting for Cinemachine objects. Camera will not follow player.");
                yield break;
            }

            yield return null;
        }

        // Hook the brain to tick-based updates
        if (m_Brain != null)
            TimeManager.OnPostTick += m_Brain.ManualUpdate;

        // Assign follow/look-at targets
        m_Camera.Follow = transform;
        m_Camera.LookAt = transform;

        Debug.Log("[PlayerCamera] Camera setup complete.");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (_setupCoroutine != null)
        {
            StopCoroutine(_setupCoroutine);
            _setupCoroutine = null;
        }

        if (m_Brain != null)
            TimeManager.OnPostTick -= m_Brain.ManualUpdate;
    }
}