using System.Linq;
using _Scripts.Managers;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

public class PickUpObject : NetworkBehaviour
{
    [SerializeField] private AudioSource pickupSound;
    
    protected bool itemPickedUp = false;
    private void OnTriggerEnter(Collider other)
    {
        TryPickUp(other);
    }
    private void OnTriggerStay(Collider other)
    {
        TryPickUp(other);
    }

    private void TryPickUp(Collider other)
    {
        if (itemPickedUp)
            return;
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("PickupObject: player");
            RequestPickupServerRpc(other.gameObject);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void RequestPickupServerRpc(GameObject player)
    {
        // Don't allow pickup while respawning
        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        if (playerStats != null && playerStats.isRespawning.Value)
            return;

        if (itemPickedUp){ 
            // Debug.Log($"PickupObject: itemPickedUp: {itemPickedUp}");
            return;
        }
        itemPickedUp = true;
        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider != null)
        {
            RpcPlayPickupSound(playerStats.Owner);
            ItemPickUp(playerCollider);
        }
    }
    protected virtual void ItemPickUp(Collider other)
    {

    }
    
    [TargetRpc]
    private void RpcPlayPickupSound(NetworkConnection target)
    {
        PersistentAudioSourceManager.GetInstance().PlaySoundBasedOnRefencedSource(pickupSound);
    }
    
    protected void ResetPickupState()
    {
        itemPickedUp = false;
    }
}
