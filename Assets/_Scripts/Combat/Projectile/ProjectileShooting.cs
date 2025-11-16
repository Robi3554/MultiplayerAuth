using UnityEngine;
using UnityEngine.InputSystem;

public class ProjectileShooting : RaycastShoot
{
    [SerializeField]
    private GameObject projectilePrefab;

    protected override void HandleShootInput(InputAction.CallbackContext context)
    {
        if (Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + 1 / fireRate;
            Shoot();
        }
    }

    protected override void Shoot()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        Vector3 origin = firePoint.position;
        Vector3 direction = -firePoint.up;

        playerNet.NotifyProjectileShotServer(projectilePrefab, origin, direction * speed, damage, maxDistance);

        if (canPlayShootSound)
        {
            shootAudioSource.PlayOneShot(shootAudioClip);
        }

        CurrentAmmo--;
    }

}
