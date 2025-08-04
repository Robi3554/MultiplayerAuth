using UnityEngine;
using FishNet.Object;

public class HealthPickUp : PickUpObject
{
    [SerializeField] private int healAmount = 50;

    override protected void ItemPickUp(Collision other)
    {
        Debug.Log("Health pack picked up");
        CallHealPlayer(healAmount, other);
        Debug.Log("Healing is technically done");
        base.Despawn();
    }

    private void CallHealPlayer(int healAmount, Collision other) // calling a ServerRpc method directly from the client doesn`t work
    {                                                            // in this case, we call it from the OnColliderEnter, a client method
        ServerHealPlayer(healAmount, other); // Call the ServerRpc
    }

    [ServerRpc(RequireOwnership = false)]
    protected void ServerHealPlayer(int healAmount, Collision playerCollider)
    {
        Debug.Log("Trying to heal player");
        NetworkObject playerNetObj = playerCollider.gameObject.GetComponentInParent<NetworkObject>();
        if (playerNetObj == null)
        {
            Debug.Log("Player network object does not exist");
            return;
        }
        var playerId = playerNetObj.Owner.ClientId;
        Debug.Log("Healing player");
        PlayerManager.Instance.HealPlayer(playerId, healAmount);
    }
}
