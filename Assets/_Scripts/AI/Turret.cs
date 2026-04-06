using FishNet.Object;
using UnityEngine;

public class Turret : NetworkBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private LayerMask playerMask;

    [Header("Targeting")]
    [SerializeField] private Transform rotatingPart;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask obstaclesMask;

    [Header("Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float projectileSpeed = 30f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float maxProjectileDistance = 50f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootClip;

    private Transform currentTarget;
    private float nextFireTime;
    private readonly Collider[] detectedColliders = new Collider[10];

    // Turret uses -1 as attacker ID (non-player source)
    private const int TURRET_ATTACKER_ID = -1;

    private void Update()
    {
        if (!IsServerInitialized)
            return;

        FindTarget();

        if (currentTarget != null)
        {
            RotateTowardsTarget();

            if (CanShoot())
            {
                Shoot();
            }
        }
    }

    private void FindTarget()
    {
        currentTarget = null;
        float closestDistance = float.MaxValue;

        int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, detectedColliders, playerMask);

        for (int i = 0; i < count; i++)
        {
            Collider col = detectedColliders[i];
            if (col == null) continue;

            // Skip dead or respawning players
            if (col.TryGetComponent(out PlayerStats stats) && (stats.health.Value <= 0 || stats.isRespawning.Value))
                continue;

            Vector3 targetPosition = col.transform.position;
            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance < closestDistance)
            {
                if (requireLineOfSight && !HasLineOfSight(targetPosition))
                    continue;

                closestDistance = distance;
                currentTarget = col.transform;
            }
        }
    }

    private bool HasLineOfSight(Vector3 targetPosition)
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 direction = (targetPosition - origin).normalized;
        float distance = Vector3.Distance(origin, targetPosition);

        // Check if there's an obstacle between turret and target
        return !Physics.Raycast(origin, direction, distance, obstaclesMask);
    }

    private void RotateTowardsTarget()
    {
        if (rotatingPart == null || currentTarget == null)
            return;

        Vector3 direction = (currentTarget.position - rotatingPart.position).normalized;
        direction.y = 0f; // Keep rotation on horizontal plane

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rotatingPart.rotation = Quaternion.Slerp(rotatingPart.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private bool CanShoot()
    {
        if (currentTarget == null)
            return false;

        if (Time.time < nextFireTime)
            return false;

        // Check if turret is roughly facing the target
        if (rotatingPart != null)
        {
            Vector3 toTarget = (currentTarget.position - rotatingPart.position).normalized;
            toTarget.y = 0f;
            float angle = Vector3.Angle(rotatingPart.forward, toTarget);

            if (angle > 15f)
                return false;
        }

        return true;
    }

    private void Shoot()
    {
        nextFireTime = Time.time + (1f / fireRate);

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 direction = (currentTarget.position - origin).normalized;
        Vector3 velocity = direction * projectileSpeed;

        SpawnProjectile(origin, velocity);
        PlayShootEffectsRpc();
    }

    private void SpawnProjectile(Vector3 origin, Vector3 velocity)
    {
        if (projectilePrefab == null)
        {
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(velocity));
        ServerManager.Spawn(projectile);

        if (projectile.TryGetComponent(out ProjectileScript p))
        {
            p.ServerInitialize(velocity, damage, maxProjectileDistance, TURRET_ATTACKER_ID);
        }
    }

    [ObserversRpc]
    private void PlayShootEffectsRpc()
    {
        if (audioSource != null && shootClip != null)
        {
            audioSource.PlayOneShot(shootClip);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Fire point
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(firePoint.position, 0.1f);
            Gizmos.DrawRay(firePoint.position, firePoint.forward * 2f);
        }
    }
}
