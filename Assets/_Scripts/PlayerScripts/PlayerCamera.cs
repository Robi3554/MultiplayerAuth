using UnityEngine;
using FishNet.Object;
using Unity.Cinemachine;

public class PlayerCameraSetter : NetworkBehaviour
{
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
            cam.Follow = transform;
            cam.LookAt = transform;
        }
    }
}