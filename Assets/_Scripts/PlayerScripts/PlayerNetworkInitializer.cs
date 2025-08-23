using FishNet.Object;
using UnityEngine;

public class PlayerNetworkInitializer : NetworkBehaviour
{
    public override void OnStartServer()
    {
        base.OnStartServer();

        int clientId = (int)Owner.ClientId;
        var stats = GetComponent<PlayerStats>();

        Debug.Log($"[OnStartServer] Registering player with ClientId: {clientId}");

        PlayerManager.Instance.players[clientId] = new PlayerManager.Player
        {
            playerObject = this.gameObject,
            connection = Owner,
            stats = stats
        };
    }
}
