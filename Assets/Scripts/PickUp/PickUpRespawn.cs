using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;

public class PickUpRespawn : NetworkBehaviour
{
    [SerializeField] private float respawnTime = 10f;
    private List<RespawnData> respawnList = new List<RespawnData>();

    public struct RespawnData
    {
        public NetworkObject childNetworkObject;
        public Vector3 originalPosition;
        public Quaternion originalRotation;
        public float respawnTimer;
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
        respawnList.Add(newRespawn);
    }

    private void Update()
    {
        if (IsServerInitialized)
        {
            for (int i = 0; i < respawnList.Count; i++)
            {
                RespawnData data = respawnList[i];
                data.respawnTimer -= Time.deltaTime;

                if (data.respawnTimer <= 0)
                {
                    RespawnChild(data);
                    respawnList.RemoveAt(i);
                    i--; // adjust index after removing
                }
                else
                {
                    respawnList[i] = data; // update the timer
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
            data.childNetworkObject.RemoveOwnership(); // remove ownership
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
