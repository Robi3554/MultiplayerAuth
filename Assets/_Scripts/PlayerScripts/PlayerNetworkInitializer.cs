using FishNet.Object;
using GameKit.Dependencies.Utilities.ObjectPooling.Examples;
using UnityEngine;

public class PlayerNetworkInitializer : NetworkBehaviour
{
    [SerializeField] private GameObject playerHUD;

    /// <summary>
    /// Exposes the player's own HUD so Weapon can resolve its ammo text from
    /// the correct hierarchy rather than using scene-global GameObject.Find.
    /// </summary>
    public GameObject PlayerHUD => playerHUD;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (PlayerManager.Instance == null)
        {
            return;
        }

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
        {
            playerHUD.SetActive(false);
            return;
        }

        // Configure mobile input mode for the local player
        var movement = GetComponent<PredictionMoving>();
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.ConfigureLocalPlayer(movement);
        }
        else if (Application.isMobilePlatform)
        {
            // Fallback: MobileInputManager not in scene, set joystick mode directly
            movement.SetInputMode(true);
        }
    }

    // Called by RaycastShoot when a hit happens
    [ServerRpc]
    public void NotifyHitServer(NetworkObject target, int damage)
    {
        if (target == null)
            return;

        // Turret hit
        if (target.TryGetComponent(out Turret turret))
        {
            turret.TakeDamage(damage);
            return;
        }

        int targetId = (int)target.Owner.ClientId;
        int attackerId = (int)Owner.ClientId;

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

        if (go.TryGetComponent(out ProjectileScript p))
        {
            p.ServerInitialize(velocity, damage, maxDistance, attackerId);
        }
    }

    [ServerRpc]
    public void ProjectileWeaponSound()
    {
        PlayProjectileWeaponSoundObserversRpc();
    }

    [ObserversRpc]
    private void PlayProjectileWeaponSoundObserversRpc()
    {
        var weapon = GetComponentInChildren<ProjectileShooting>();
        if (weapon != null)
            weapon.PlaySound();
    }

    [ServerRpc]
    public void NotifyMuzzleFlashServer()
    {
        ShowMuzzleFlashObserversRpc();
    }

    [ObserversRpc]
    private void ShowMuzzleFlashObserversRpc()
    {
        var weapon = GetComponentInChildren<Weapon>();
        if (weapon != null)
            weapon.PlayMuzzleFlash();
    }

    [ServerRpc]
    public void ControlFootstepSoundsServer(PredictionMoving playerMovement, float speed)
    {
        playerMovement.ControlFootstepSounds(speed);
    }

    [ServerRpc]
    public void PlayDashSoundServer(PredictionMoving playerMovement)
    {
        playerMovement.PlayDashSound();
    }

    [ServerRpc]
    public void PlayJumpSoundServer(PredictionMoving playerMovement)
    {
        playerMovement.PlayJumpSound();
    }
}
