using FishNet.Object;
using GameKit.Dependencies.Utilities.ObjectPooling.Examples;
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
    [ServerRpc]
    public void NotifyHitServer(NetworkObject targetPlayer, int damage)
    {
        if (targetPlayer == null)
            return;

        int targetId = (int)targetPlayer.Owner.ClientId;
        int attackerId = (int)Owner.ClientId;

        Debug.Log($"[Server] Player {attackerId} hit Player {targetId} for {damage} damage.");
        PlayerManager.Instance.DamagePlayer(targetId, damage, attackerId);
    }

    // Called when a player fires a raycast bullet
    [ServerRpc]
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

    // Called when a player fires a rigidbody bullet
    [ServerRpc]
    public void NotifyProjectileShotServer(GameObject projectilePrefab, Vector3 origin, Vector3 velocity, int damage, float maxDistance)
    {
        GameObject go = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(velocity));
        ServerManager.Spawn(go);

        int attackerId = (int)Owner.ClientId;
        Debug.Log("Attacker ID: " +  attackerId);

        if (go.TryGetComponent(out ProjectileScript p))
        {
            p.ServerInitialize(velocity, damage, maxDistance, attackerId);
        }
    }

    [ServerRpc]
    public void ProjectileWeaponSound()
    {
        var weapon = GetComponentInChildren<ProjectileShooting>();
        if (weapon != null)
            weapon.PlaySound();
    }
}
