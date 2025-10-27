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
    private Vector3 initPosition;
    private Quaternion initRotation;

    private Collider _col;
    private void Awake()
    {
        _col = GetComponent<Collider>();
    }
    private void Start()
    {
        initPosition = transform.position;
        initRotation = transform.rotation;
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
        if (IsServerStarted)
        {
            Debug.Log("HealthPickUp: Enable on server");
            transform.position = initPosition;
            transform.rotation = initRotation;
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
            // Vector3 FloorPosition = new Vector3(transform.position.x, yBasePosition, transform.position.z);
            parentRespawn.StartRespawnTimer(NetworkObject); // call the StartRespawnTimer method on the parent
        }
        else
        {
            Debug.LogWarning("HealthPickUp: PickUpRespawn component not found on parent!");
        }
        ResetPickup();
        ItemPickUpObserver();
        // ServerManager.Despawn(gameObject, DespawnType.Pool); // despawn the health pickup
    }

    [ObserversRpc]
    private void ItemPickUpObserver()
    {
        gameObject.SetActive(false);
    }
}
