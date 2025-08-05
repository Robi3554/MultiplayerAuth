using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class HealthPickUp : PickUpObject
{
    [SerializeField] private int healAmount = 50;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    public override void OnStartServer()
    {
        base.OnStartServer();
        originalPosition = transform.position;
        originalRotation = transform.rotation;
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
            parentRespawn.StartRespawnTimer(NetworkObject, originalPosition, originalRotation); // call the StartRespawnTimer method on the parent
        }
        else
        {
            Debug.LogWarning("PickUpRespawn component not found on parent!");
        }
        itemPickedUp = false;
        Despawn(); // despawn the health pickup
    }
}
