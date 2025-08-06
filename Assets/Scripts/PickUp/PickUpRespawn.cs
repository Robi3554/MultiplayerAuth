using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;
using FishNet.Object.Synchronizing;

public class PickUpRespawn : NetworkBehaviour
{
    [SerializeField] private float respawnTime = 10f;
    private readonly SyncList<RespawnData> _respawnList = new SyncList<RespawnData>();
    
    [System.Serializable]
    private struct RespawnData
    {
        public NetworkObject childNetworkObject;
        public Vector3 originalPosition;
        public Quaternion originalRotation;
        public float respawnTimer;
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
    private void _myCollection_OnChange(SyncListOperation op, int index,
        RespawnData oldItem, RespawnData newItem, bool asServer)
    {
        switch (op)
        {
            /* An object was added to the list. Index
            * will be where it was added, which will be the end
            * of the list, while newItem is the value added. */
            case SyncListOperation.Add:
            
                Debug.Log($"Item added at index {index}. position:{newItem.originalPosition}, rotation:{newItem.originalRotation}");
                break;
            /* An object was removed from the list. Index
            * is from where the object was removed. oldItem
            * will contain the removed item. */
            case SyncListOperation.RemoveAt:
                Debug.Log($"Item removed from index {index}");
                break;
            /* An object was inserted into the list. Index
            * is where the obejct was inserted. newItem
            * contains the item inserted. */
            case SyncListOperation.Insert:
                break;
            /* An object replaced another. Index
            * is where the object was replaced. oldItem
            * is the item that was replaced, while
            * newItem is the item which now has it's place. */
            case SyncListOperation.Set:
                break;
            /* All objects have been cleared. Index, oldValue,
            * and newValue are default. */
            case SyncListOperation.Clear:
                break;
            /* When complete calls all changes have been
            * made to the collection. You may use this
            * to refresh information in relation to
            * the list changes, rather than doing so
            * after every entry change. Like Clear
            * Index, oldItem, and newItem are all default. */
            case SyncListOperation.Complete:
                break;
        }
    }
    private void Awake()
    {
        /* Listening to SyncList callbacks are a
        * little different from SyncVars. */
        _respawnList.OnChange += _myCollection_OnChange;
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    // call this method to start the respawn timer in the item
    [ServerRpc(RequireOwnership = false)]
    public void StartRespawnTimer(NetworkObject childNetObj, Vector3 originalPosition, Quaternion originalRotation)
    {
        Debug.Log("Starting respawn timer");
        RespawnData newRespawn = new RespawnData
        {
            childNetworkObject = childNetObj,
            originalPosition = originalPosition,
            originalRotation = originalRotation,
            respawnTimer = respawnTime
        };
        _respawnList.Add(newRespawn);
    }

    private void Update()
    {
        if (IsServerInitialized)
        {
            for (int i = 0; i < _respawnList.Count; i++)
            {
                RespawnData data = _respawnList[i];
                data.respawnTimer -= Time.deltaTime;

                if (data.respawnTimer <= 0)
                {
                    RespawnChild(data);
                    _respawnList.RemoveAt(i);
                    i--; // adjust index after removing
                }
                else
                {
                    ModifyRespawnData(i, data);
                }
            }
        }
    }

    private void RespawnChild(RespawnData data)
    {
        Debug.Log("Respawning child");
        if (data.childNetworkObject != null)
        {
            // reset the object's state
            data.childNetworkObject.transform.position = data.originalPosition; // reset to original position
            data.childNetworkObject.transform.rotation = data.originalRotation; // reset rotation

            // deinitialize the NetworkObject before spawning (had errors in console for this lol)
            data.childNetworkObject.gameObject.SetActive(false); // disable the object (needs to be disabled while reseting)
            data.childNetworkObject.ResetState(true); // reset the NetworkObject as server
            // data.childNetworkObject.RemoveOwnership(); // remove ownership
            data.childNetworkObject.gameObject.SetActive(true); // enable the object

            // spawn the NetworkObject
            ServerManager.Spawn(data.childNetworkObject, Owner);
        }
        else
        {
            Debug.LogWarning("child to respawn is null!");
        }
    }
}
