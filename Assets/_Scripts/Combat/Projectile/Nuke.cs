using FishNet.Object;
using UnityEngine;

public class Nuke : NetworkBehaviour
{
    private int attackerId;

    [SerializeField]
    private int damage = 999;

    public void ServerInitialize(int attackerId)
    {
        this.attackerId = attackerId;

        InitializeObserversRpc(attackerId);
    }

    [ObserversRpc(BufferLast = true)]
    private void InitializeObserversRpc(int attackerId)
    {
        if (!IsServerInitialized)
        {
            this.attackerId = attackerId;
        }
    }

    private void OnCollisionEnter(Collision col)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach(GameObject player in players)
        {
            NetworkObject netObj = player.GetComponent<NetworkObject>();

            if (netObj == null || netObj.Owner == null)
                continue;

            int targetId = (int)netObj.Owner.ClientId;

            PlayerManager.Instance.DamagePlayer(targetId, damage, attackerId);
        }

        DespawnNuke();
    }

    private void DespawnNuke()
    {
        if (IsServerInitialized)
        {
            ServerManager.Despawn(gameObject);
        }
    }
}
