using UnityEngine;
using FishNet.Object;
using Unity.Cinemachine;

public class PlayerCameraSetter : NetworkBehaviour
{
    private CinemachineBrain m_Brain;
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            m_Brain = FindFirstObjectByType<CinemachineBrain>();
            if (m_Brain != null)
                TimeManager.OnPostTick += m_Brain.ManualUpdate;
            
            CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
            cam.Follow = transform;
            cam.LookAt = transform;
        }
    }
    
    public override void OnStopClient()
    {
        base.OnStopClient();
        if (m_Brain != null)
            TimeManager.OnPostTick -= m_Brain.ManualUpdate;
    }

}