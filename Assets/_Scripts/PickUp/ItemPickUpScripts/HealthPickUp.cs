using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.CodeGenerating;
using System;
using System.Collections;
using System.Globalization;

public class HealthPickUp : PickUpObject
{
    [SerializeField] private int healAmount = 50;
    [SerializeField] private float pickupDelay = 2f;
    [SerializeField] private string analyticsPickupId;
    
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
        }
    }

    public override void OnStartServer()
    {
        // always reset state on server start
        ResetPickupState(); // resets itemPickedUp
    }

    private void OnEnable()
    {
        if (IsServerInitialized)
        {
            ResetPickupState();
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
            AnalyticsManager.EnsureInstance().RecordHealthPickup(GetAnalyticsPickupId(), playerStats.Owner.ClientId);
            playerStats.HealPlayer(healAmount); // call the server-side HealPlayer method
        }
        // get the PickUpRespawn component from the parent
        PickUpRespawn parentRespawn = GetComponentInParent<PickUpRespawn>();
        if (parentRespawn != null)
        {
            parentRespawn.StartRespawnTimer(this.NetworkObject,initPosition.Value,initRotation.Value); // call the StartRespawnTimer method on the parent
        }
    }

    private string GetAnalyticsPickupId()
    {
        if (!string.IsNullOrWhiteSpace(analyticsPickupId))
            return analyticsPickupId;

        Vector3 position = transform.position;
        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : "UnknownScene";
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}/{1} @ x:{2:0.#} y:{3:0.#} z:{4:0.#}",
            sceneName,
            name,
            position.x,
            position.y,
            position.z);
    }
}
