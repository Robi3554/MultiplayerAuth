using FishNet;
using FishNet.Object;
using GameKit.Dependencies.Utilities.ObjectPooling.Examples;
using UnityEngine;

public class PlayerNetworkInitializer : NetworkBehaviour
{
    [SerializeField] private GameObject playerHUD;

    [Header("Analytics")]
    [SerializeField] private float analyticsReportInterval = 10f;
    [SerializeField] private float latencySampleInterval = 1f;

    /// <summary>
    /// Exposes the player's own HUD so Weapon can resolve its ammo text from
    /// the correct hierarchy rather than using scene-global GameObject.Find.
    /// </summary>
    public GameObject PlayerHUD => playerHUD;

    private float analyticsReportTimer;
    private float latencySampleTimer;
    private float fpsSampleSum;
    private int fpsSampleCount;
    private long latencySampleSum;
    private int latencySampleCount;

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

        AnalyticsManager.EnsureInstance().RegisterPlayer(Owner, stats);
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

    private void Update()
    {
        if (!IsOwner || !IsClientStarted)
            return;

        SampleAnalytics();
    }

    public override void OnStopClient()
    {
        FlushPerformanceAnalytics();
        base.OnStopClient();
    }

    private void OnApplicationQuit()
    {
        FlushPerformanceAnalytics();
    }

    private void SampleAnalytics()
    {
        float deltaTime = Time.unscaledDeltaTime;
        if (deltaTime > 0f)
        {
            fpsSampleSum += 1f / deltaTime;
            fpsSampleCount++;
        }

        latencySampleTimer += deltaTime;
        if (latencySampleTimer >= latencySampleInterval)
        {
            latencySampleTimer = 0f;
            if (InstanceFinder.TimeManager != null)
            {
                latencySampleSum += InstanceFinder.TimeManager.RoundTripTime;
                latencySampleCount++;
            }
        }

        analyticsReportTimer += deltaTime;
        if (analyticsReportTimer >= analyticsReportInterval)
        {
            analyticsReportTimer = 0f;
            FlushPerformanceAnalytics();
        }
    }

    private void FlushPerformanceAnalytics()
    {
        if (!IsOwner || !IsClientStarted || (fpsSampleCount <= 0 && latencySampleCount <= 0))
            return;

        float averageFps = fpsSampleCount > 0 ? fpsSampleSum / fpsSampleCount : 0f;
        long averageLatency = latencySampleCount > 0 ? latencySampleSum / latencySampleCount : 0L;

        ReportPerformanceServerRpc(averageFps, fpsSampleCount, averageLatency, latencySampleCount);

        fpsSampleSum = 0f;
        fpsSampleCount = 0;
        latencySampleSum = 0L;
        latencySampleCount = 0;
    }

    [ServerRpc]
    private void ReportPerformanceServerRpc(float averageFps, int fpsSamples, long averageLatencyMs, int latencySamples)
    {
        var stats = GetComponent<PlayerStats>();
        AnalyticsManager.EnsureInstance().RegisterPlayer(Owner, stats);
        AnalyticsManager.Instance.RecordClientPerformance(Owner.ClientId, averageFps, fpsSamples, averageLatencyMs, latencySamples);
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
    public void ControlFootstepSoundsServer(PredictionMoving playerMovement, float speed, bool isGrounded)
    {
        playerMovement.ControlFootstepSounds(speed, isGrounded);
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
