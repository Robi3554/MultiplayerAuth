using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

public class PickUpObject : NetworkBehaviour
{
    protected bool itemPickedUp = false;
    private void OnTriggerEnter(Collider other)
    {
        TryPickUp(other);
    }
    private void OnTriggerStay(Collider other)
    {
        Debug.Log($"PickupObject: IsServerInitialized:{IsServerInitialized}");
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
        Debug.Log("PickupObject: RequestPickupServerRpc");

        if (itemPickedUp){ 
            // Debug.Log($"PickupObject: itemPickedUp: {itemPickedUp}");
            return;
        }
        itemPickedUp = true;
        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider != null)
        {
            ItemPickUp(playerCollider);
        }
    }
    protected virtual void ItemPickUp(Collider other)
    {
        Debug.Log("PickupObject: Default item pickup does nothing");
    }
    
    protected void ResetPickupState()
    {
        Debug.Log("PickupObject: ResetPickupState");
        itemPickedUp = false;
    }
}
