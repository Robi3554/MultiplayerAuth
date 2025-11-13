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
        else
        {
            Debug.LogWarning($"Index {index} is out of range for _respawnList (Count: {_respawnList.Count})");
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    // call this method to start the respawn timer in the item
    public void StartRespawnTimer(NetworkObject pickedNetworkObj, Vector3 initPosition, Quaternion initRotation)
    {
        Debug.Log("PickUpRespawn: Starting respawn timer");
        if (pickedNetworkObj == null || !IsServerStarted) return;
        RespawnData newRespawn = new RespawnData
        {
            pickedNetObj = pickedNetworkObj,
            respawnTimer = respawnTime,
            position = initPosition,
            rotation = initRotation
        };
        ServerManager.Despawn(pickedNetworkObj.gameObject);
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
        Debug.Log("PickUpRespawn: Respawning child on server");
        if (spawnableObjPrefab == null)
        {
            Debug.LogError("PickUpRespawn: spawnableObjPrefab is null; cannot respawn.");
            return;
        }
        
        GameObject spawnedObj = Instantiate(spawnableObjPrefab, data.position, data.rotation);
        spawnedObj.transform.SetParent(this.transform, true);
        ServerManager.Spawn(spawnedObj);
    }
}
