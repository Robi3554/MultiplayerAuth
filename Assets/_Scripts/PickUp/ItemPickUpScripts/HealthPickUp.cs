using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.CodeGenerating;
using System;
using System.Collections;

public class HealthPickUp : PickUpObject
{
    [SerializeField] private int healAmount = 50;
    [AllowMutableSyncType]
    private SyncVar<bool> readyForPickUp = new SyncVar<bool>(false);
    [SerializeField] private float pickupDelay = 2f;
    
    [AllowMutableSyncType]
    private SyncVar<Vector3> initPosition = new SyncVar<Vector3>();
    [AllowMutableSyncType]
    private SyncVar<Quaternion> initRotation = new SyncVar<Quaternion>();

    private Collider _col;

    private void Awake()
    {
        _col = GetComponent<Collider>();
    }

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
        ResetPickup();      // disables collider + SyncVar
        StartCoroutine(EnablePickupAfterDelay(pickupDelay)); // wait before enabling
    }

    private void OnEnable()
    {
        Debug.Log($"HealthPickUp: OnEnable IsServerInitialized:{IsServerInitialized}, IsServerStarted:{IsServerStarted}, IsClientInitialized:{IsClientInitialized}");

        if (!IsServerInitialized)
        {
            StartCoroutine(EnablePickupAfterDelay(pickupDelay)); // goes to sleep for x seconds and continues after
        }

        if (IsServerInitialized)
        {
            Debug.Log("HealthPickUp: Enable on server");
            ResetPickupState();
            ResetPickup(); // always reset collider off
            StartCoroutine(EnablePickupAfterDelay(pickupDelay)); // always delay before ready
        }
        if (IsClientInitialized)
        {
            Debug.Log("HealthPickUp: Enable on client");
            // client logic: could play a respawn VFX, glow effect, etc.
        }
    }
    private void ResetPickup()
    {
        readyForPickUp.Value = false;
        if (_col != null)
            _col.enabled = false;
        Debug.Log($"HealthPickUp: _col.enabled:{_col.enabled}, readyForPickUp.Value:{readyForPickUp.Value}");
    }

    private IEnumerator EnablePickupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log($"HealthPickUp: EnablePickupAfterDelay IsServerInitialized:{IsServerInitialized}");

        if (this != null && IsServerInitialized)
        {
            _col.enabled = true;
            readyForPickUp.Value = true;
        }
        Debug.Log($"HealthPickUp: _col.enabled:{_col.enabled}, readyForPickUp.Value:{readyForPickUp.Value}");
    }
    [Server]
    override protected void ItemPickUp(Collider other)
    {
        // Debug.Log($"HealthPickUp::ItemPickUp : readyForPickUp.Value:{readyForPickUp.Value}");
        if (!readyForPickUp.Value) return;
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
        ResetPickup();
    }
}
