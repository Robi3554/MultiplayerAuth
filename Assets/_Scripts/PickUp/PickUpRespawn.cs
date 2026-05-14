using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using UnityEngine.UIElements;

public class PickUpRespawn : NetworkBehaviour
{
    [SerializeField] private GameObject spawnableObjPrefab;
    [SerializeField] private float respawnTime = 10f;

    private readonly List<RespawnData> _respawnList = new List<RespawnData>();

    [System.Serializable]
    private struct RespawnData
    {
        public NetworkObject pickedNetObj;
        public float respawnTimer;
        public Vector3 position;
        public Quaternion rotation;
    }

    [Server]
    private void ModifyRespawnData(int index, RespawnData respawnData)
    {
        if (index >= 0 && index < _respawnList.Count)
        {
            _respawnList[index] = respawnData;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    // call this method to start the respawn timer in the item
    public void StartRespawnTimer(NetworkObject pickedNetworkObj, Vector3 initPosition, Quaternion initRotation)
    {
        if (pickedNetworkObj == null || !IsServerStarted) return;

        // Hide the pickup instead of despawning it so the scene NetworkObject
        // stays properly tracked by FishNet across scene reloads.
        pickedNetworkObj.gameObject.SetActive(false);
        RpcSetPickupActive(pickedNetworkObj.gameObject, false);

        RespawnData newRespawn = new RespawnData
        {
            pickedNetObj = pickedNetworkObj,
            respawnTimer = respawnTime,
            position = initPosition,
            rotation = initRotation
        };
        _respawnList.Add(newRespawn);
    }

    private void Update()
    {
        if (!IsServerInitialized) return;

        for (int i = 0; i < _respawnList.Count; i++)
        {
            RespawnData data = _respawnList[i];
            data.respawnTimer -= Time.deltaTime;
            // Debug.Log($"PickUpRespawn: Item {i} timer: {data.respawnTimer}");

            if (data.respawnTimer <= 0f)
            {
                RespawnChildServer(data);
                _respawnList.RemoveAt(i);
                i--; // adjust index after removing
            }
            else
            {
                ModifyRespawnData(i, data);
            }
        }
    }
    private void RespawnChildServer(RespawnData data)
    {
        // If the object was destroyed (e.g. scene transition), skip it.
        if (data.pickedNetObj == null) return;

        // Restore position and re-show the original pickup.
        data.pickedNetObj.transform.SetPositionAndRotation(data.position, data.rotation);
        data.pickedNetObj.gameObject.SetActive(true);
        RpcSetPickupActive(data.pickedNetObj.gameObject, true);
    }

    [ObserversRpc]
    private void RpcSetPickupActive(GameObject obj, bool active)
    {
        if (obj != null)
            obj.SetActive(active);
    }
}
