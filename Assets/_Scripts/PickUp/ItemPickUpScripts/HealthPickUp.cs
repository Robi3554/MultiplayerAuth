using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.CodeGenerating;
using System;
using System.Collections;
using System.Threading;
using OpenCover.Framework.Model;

public class HealthPickUp : PickUpObject
{
    [SerializeField] private int healAmount = 50;
    [Tooltip("Change to the height of the plane + a bit(e.g 50.0f + 2.5f = 52.5) to keep it grounded. It can float otherwise 😭")]
    [SerializeField] private float yBasePosition = 157.0f; // change to the height of the planne
    [AllowMutableSyncType]
    private SyncVar<bool> readyForPickUp = new SyncVar<bool>(false);
    [SerializeField] private float pickupDelay = 2f; 

    private Collider _col;
   private void Awake()
    {
        _col = GetComponent<Collider>();
    }
    public override void OnStartServer()
    {
        if (!IsServerInitialized) return;

        // always reset state on server start
        ResetPickupState(); // resets itemPickedUp
        ResetPickup();      // disables collider + SyncVar
        StartCoroutine(EnablePickupAfterDelay(pickupDelay)); // wait before enabling
    }
    private void OnEnable()
    {
        if (!IsServerInitialized) return;

        ResetPickupState();
        ResetPickup(); // always reset collider off
        StartCoroutine(EnablePickupAfterDelay(pickupDelay)); // always delay before ready
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
        Debug.Log($"HealthPickUp::ItemPickUp : readyForPickUp.Value:{readyForPickUp.Value}");
        if (!readyForPickUp.Value) return;

        Debug.Log("Healing: Health pack picked up");
        // get the PlayerStats component from the player
        PlayerStats playerStats = other.GetComponentInParent<PlayerStats>();
        if (playerStats != null)
        {
            Debug.Log("Healing: Healing player");
            playerStats.HealPlayer(healAmount); // call the server-side HealPlayer method
        }
        else
        {
            Debug.LogWarning("Healing: PlayerStats component not found");
        }
        // get the PickUpRespawn component from the parent
        PickUpRespawn parentRespawn = GetComponentInParent<PickUpRespawn>();
        if (parentRespawn != null)
        {
            Debug.Log("healing: Calling StartRespawnTimer on parent");
            // Vector3 FloorPosition = new Vector3(transform.position.x, yBasePosition, transform.position.z);
            parentRespawn.StartRespawnTimer(NetworkObject, transform.position, transform.rotation); // call the StartRespawnTimer method on the parent
        }
        else
        {
            Debug.LogWarning("Healing: PickUpRespawn component not found on parent!");
        }
        ResetPickup();
        ServerManager.Despawn(gameObject, DespawnType.Pool); // despawn the health pickup
    }
}
