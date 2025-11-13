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
    
    // private SyncList<GameObject> _spawnList = new SyncList<GameObject>();


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
    // private void _myCollection_OnChange(SyncListOperation op, int index,
    //     RespawnData oldItem, RespawnData newItem, bool asServer)
    // {
    //     switch (op)
    //     {
    //         /* An object was added to the list. Index
    //         * will be where it was added, which will be the end
    //         * of the list, while newItem is the value added. */
    //         case SyncListOperation.Add:

    //             Debug.Log($"Item added at index {index}");
    //             break;
    //         /* An object was removed from the list. Index
    //         * is from where the object was removed. oldItem
    //         * will contain the removed item. */
    //         case SyncListOperation.RemoveAt:
    //             Debug.Log($"Item removed from index {index}");
    //             break;
    //         /* An object was inserted into the list. Index
    //         * is where the obejct was inserted. newItem
    //         * contains the item inserted. */
    //         case SyncListOperation.Insert:
    //             break;
    //         /* An object replaced another. Index
    //         * is where the object was replaced. oldItem
    //         * is the item that was replaced, while
    //         * newItem is the item which now has it's place. */
    //         case SyncListOperation.Set:
    //             break;
    //         /* All objects have been cleared. Index, oldValue,
    //         * and newValue are default. */
    //         case SyncListOperation.Clear:
    //             break;
    //         /* When complete calls all changes have been
    //         * made to the collection. You may use this
    //         * to refresh information in relation to
    //         * the list changes, rather than doing so
    //         * after every entry change. Like Clear
    //         * Index, oldItem, and newItem are all default. */
    //         case SyncListOperation.Complete:
    //             break;
    //     }
    // }
    // private void Awake()
    // {
    //     /* Listening to SyncList callbacks are a
    //     * little different from SyncVars. */
    //     _respawnList.OnChange += _myCollection_OnChange;
    // }
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

        // RespawnChildObserver(data);

    }
    [ObserversRpc]
    private void RespawnChildObserver(RespawnData data)
    {

        // data.childNetworkObject.gameObject.SetActive(true);
        // aici ar trb sa dau spawn la locatia obiectului initial. trb sa fac reference la prefab cumva
    }
}
