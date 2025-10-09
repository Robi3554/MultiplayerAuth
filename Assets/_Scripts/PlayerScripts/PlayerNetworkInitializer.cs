using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class PlayerNetworkInitializer : NetworkBehaviour
{
    [SerializeField]
    private GameObject playerHUD;

    [SerializeField]
    private List<GameObject> weapons;

    public override void OnStartServer()
    {
        base.OnStartServer();

        int clientId = (int)Owner.ClientId;
        var stats = GetComponent<PlayerStats>();

        PlayerManager.Instance.players[clientId] = new PlayerManager.Player
        {
            playerObject = this.gameObject,
            connection = Owner,
            stats = stats
        };
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            playerHUD.SetActive(false);

        foreach (var weapon in weapons)
        {
            var netObj = weapon.GetComponent<NetworkObject>();
            if (netObj != null)
                netObj.GiveOwnership(Owner);
        }
    }
}
