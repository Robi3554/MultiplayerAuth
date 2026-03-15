using FishNet.Object;
using UnityEngine;

public class PlayerNuke : PlayerAbility
{
    [SerializeField]
    private NetworkObject bomb;

    protected override void ActivateAbilty()
    {
        base.ActivateAbilty();

        if (!IsOwner) return;

        SpawnNuke((int)Owner.ClientId);
    }

    [ServerRpc]
    private void SpawnNuke(int attackerId)
    {
        NetworkObject bombNet = Instantiate(bomb, new Vector3(105f, 16f, -25f), Quaternion.identity);
        Spawn(bombNet);

        Nuke script = bombNet.GetComponent<Nuke>();
        script.ServerInitialize(attackerId);
    }
}
