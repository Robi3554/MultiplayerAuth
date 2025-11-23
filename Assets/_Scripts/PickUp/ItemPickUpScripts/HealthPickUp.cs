 using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.CodeGenerating;
using System;
using System.Collections;

public class HealthPickUp : PickUpObject
{
    [SerializeField] private int healAmount = 50;
    [SerializeField] private float pickupDelay = 2f;
    
    [AllowMutableSyncType]
    private SyncVar<Vector3> initPosition = new SyncVar<Vector3>();
    [AllowMutableSyncType]
    private SyncVar<Quaternion> initRotation = new SyncVar<Quaternion>();

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        if (IsServerStarted)
        {
            // cache initial position and rotation on server
            initPosition.Value = transform.position;
            initRotation.Value = transform.rotation;
            Debug.Log($"HealthPickUp: Cached initial position at {initPosition.Value}");
        }
    }

    public override void OnStartServer()
    {
        // always reset state on server start
        ResetPickupState(); // resets itemPickedUp
    }

    private void OnEnable()
    {
        Debug.Log($"HealthPickUp: OnEnable IsServerInitialized:{IsServerInitialized}, IsServerStarted:{IsServerStarted}, IsClientInitialized:{IsClientInitialized}");
        if (IsServerInitialized)
        {
            Debug.Log("HealthPickUp: Enable on server");
            ResetPickupState();
        }
        if (IsClientInitialized)
        {
            Debug.Log("HealthPickUp: Enable on client");
        }
    }

    [Server]
    override protected void ItemPickUp(Collider other)
    {
        // Debug.Log($"HealthPickUp::ItemPickUp : readyForPickUp.Value:{readyForPickUp.Value}");
        Debug.Log("HealthPickUp: Health pack picked up");
        // get the PlayerStats component from the player
        PlayerStats playerStats = other.GetComponentInParent<PlayerStats>();
        if (playerStats != null)
        {
            Debug.Log("HealthPickUp: Healing player");
            playerStats.HealPlayer(healAmount); // call the server-side HealPlayer method
        }
        else
        {
            Debug.LogWarning("HealthPickUp: PlayerStats component not found");
        }
        // get the PickUpRespawn component from the parent
        PickUpRespawn parentRespawn = GetComponentInParent<PickUpRespawn>();
        if (parentRespawn != null)
        {
            parentRespawn.StartRespawnTimer(this.NetworkObject,initPosition.Value,initRotation.Value); // call the StartRespawnTimer method on the parent
        }
        else
        {
            Debug.LogWarning("HealthPickUp: PickUpRespawn component not found on parent!");
        }
    }
}
