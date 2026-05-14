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
        if (player == null) return;

        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        if (playerStats == null) return;

        // Don't allow pickup while respawning
        if (playerStats.isRespawning.Value)
            return;

        if (itemPickedUp)
            return;

        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider == null) return;

        // Only mark as picked up after all validations pass
        itemPickedUp = true;
        RpcPlayPickupSound(playerStats.Owner);
        ItemPickUp(playerCollider);
    }
    protected virtual void ItemPickUp(Collider other)
    {

    }

    [TargetRpc]
    private void RpcPlayPickupSound(NetworkConnection target)
    {
        var audioManager = PersistentAudioSourceManager.GetInstance();
        if (audioManager != null && pickupSound != null)
            audioManager.PlaySoundBasedOnRefencedSource(pickupSound);
    }

    protected void ResetPickupState()
    {
        itemPickedUp = false;
    }
}
