using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.CodeGenerating;

public class HealthPickUp : PickUpObject
{
    [SerializeField] private int healAmount = 50;

    [AllowMutableSyncType]
    private SyncVar<Vector3> originalPosition;
    [AllowMutableSyncType]
    private SyncVar<Quaternion> originalRotation;

    // private SyncVar<bool> pickedUp = new SyncVar<bool>(false);

    private void Awake()
    {
        originalPosition.Value = transform.position;
        originalRotation.Value = transform.rotation;
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    override protected void ItemPickUp(Collider other)
    {
        Debug.Log("Health pack picked up");
        // get the PlayerStats component from the player
        PlayerStats playerStats = other.GetComponentInParent<PlayerStats>();
        if (playerStats != null)
        {
            Debug.Log("Healing player");
            playerStats.HealPlayer(healAmount); // call the server-side HealPlayer method
        }
        else
        {
            Debug.Log("PlayerStats component not found");
        }
        // get the PickUpRespawn component from the parent
        PickUpRespawn parentRespawn = GetComponentInParent<PickUpRespawn>();
        if (parentRespawn != null)
        {
            Debug.Log("Calling StartRespawnTimer on parent");
            parentRespawn.StartRespawnTimer(NetworkObject, originalPosition.Value, originalRotation.Value); // call the StartRespawnTimer method on the parent
        }
        else
        {
            Debug.LogWarning("PickUpRespawn component not found on parent!");
        }
        Despawn(); // despawn the health pickup
    }

    [Server]
    void SpawnCollider()
    {
        gameObject.GetComponent<Collider>().enabled = true;
    }
}
