using FishNet.Object;
using UnityEngine;

public class PlayerNetworkInitializer : NetworkBehaviour
{
    [SerializeField]
    private GameObject playerHUD;

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
    }
}
