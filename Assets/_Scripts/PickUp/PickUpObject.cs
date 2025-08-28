using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

public class PickUpObject : NetworkBehaviour
{
    protected bool itemPickedUp = false;
    [SerializeField] protected NetworkObject itemPrefab;
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"PickupObject: IsServerInitialized:{IsServerInitialized}");
        if (!IsServerInitialized) return;

        TryPickUp(other);
    }
    private void OnTriggerStay(Collider other)
    {
        if (!IsServerInitialized) return;

        TryPickUp(other);
    }

    private void TryPickUp(Collider other)
    {
        Debug.Log($"PickupObject: itemPickedUp: {itemPickedUp}");
        if (itemPickedUp)
            return;
        Debug.Log("PickupObject: item collided with");
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("PickupObject: player");
            itemPickedUp = true;
            ItemPickUp(other);
        }
    }
    protected virtual void ItemPickUp(Collider other)
    {
        Debug.Log("Default item pickup does nothing");
    }
    
    protected void ResetPickupState()
    {
        itemPickedUp = false;
    }
}
