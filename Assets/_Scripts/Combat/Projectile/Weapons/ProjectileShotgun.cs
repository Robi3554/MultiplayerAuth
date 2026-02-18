using FishNet.Object;
using UnityEngine;

public class ProjectileShotgun : ProjectileShooting
{
    [SerializeField]
    private int pelletCount;
    [SerializeField]
    private float spreadAngle;

    protected override void Shoot()
    {
        if (!canShoot) return;

        Vector3 origin = firePoint.position;

        var playerNet = GetComponentInParent<PlayerNetworkInitializer>();

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 dir = GetSpreadDirection(-firePoint.up, spreadAngle);

            Vector3 hitPosition = origin + dir * maxDistance;

            // Notify server to replicate the pellet to all observers
            playerNet.NotifyProjectileShotServer(projectilePrefab, origin, dir * speed, damage, maxDistance);

            if (canPlayShootSound)
            {
                playerNet.ProjectileWeaponSound();
            }
        }
        
        playerNet.NotifyMuzzleFlashServer();

        CurrentAmmo--;
    }


    private Vector3 GetSpreadDirection(Vector3 forward, float angle)
    {
        float spreadRadius = Mathf.Tan(spreadAngle * Mathf.Deg2Rad);

        Vector2 rPoint = Random.insideUnitCircle * spreadRadius;

        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;

        Vector3 up = Vector3.Cross(right, forward).normalized;

        Vector3 spread = (forward + right * rPoint.x + up * rPoint.y).normalized;

        return spread;
    }
}
