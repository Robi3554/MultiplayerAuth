using FishNet.Object;
using UnityEngine;

public class PlayerNetworkInitializer : NetworkBehaviour
{
    [SerializeField] private GameObject playerHUD;

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

    // Called by RaycastShoot when a hit happens
    [ServerRpc(RequireOwnership = false)]
    public void NotifyHitServer(NetworkObject targetPlayer, int damage)
    {
        if (targetPlayer == null)
            return;

        int targetId = (int)targetPlayer.Owner.ClientId;
        int attackerId = (int)Owner.ClientId;

        Debug.Log($"[Server] Player {attackerId} hit Player {targetId} for {damage} damage.");
        PlayerManager.Instance.DamagePlayer(targetId, damage, attackerId);
    }

    // Called when a player fires a bullet
    [ServerRpc(RequireOwnership = false)]
    public void NotifyShotServer(Vector3 start, Vector3 end)
    {
        // Tell all observers to show the shot
        ShowShotObserversRpc(start, end);
    }

    [ObserversRpc]
    private void ShowShotObserversRpc(Vector3 start, Vector3 end)
    {
        // Recreate the shot line on all clients
        var weapon = GetComponentInChildren<RaycastShoot>();
        if (weapon != null)
            weapon.CreateBulletEffect(start, end);
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyReloadServer(int newAmmo)
    {
        Debug.Log($"[Server] Player {Owner.ClientId} reloaded. Ammo reset to {newAmmo}");
    }
}
