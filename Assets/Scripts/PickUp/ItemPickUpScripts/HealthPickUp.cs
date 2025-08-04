using UnityEngine;
using FishNet.Object;

public class HealthPickUp : PickUpObject
{
    [SerializeField] private int healAmount = 50;

    override protected void ItemPickUp(Collider other)
    {
        base.Despawn(); 
        Debug.Log("Health pack picked up");
        NetworkObject playerNetObj = other.gameObject.GetComponentInParent<NetworkObject>();
        if (playerNetObj == null)
        {
            Debug.Log("Player network object does not exist");
            return;
        }
        var playerId = playerNetObj.Owner.ClientId;
        Debug.Log("Healing player");
        PlayerManager.Instance.HealPlayer(playerId, healAmount);
        Debug.Log("Healing is technically done");
    }

}
