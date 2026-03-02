using UnityEngine;
using UnityEngine.InputSystem;

public class ProjectileShooting : Weapon
{
    [SerializeField]
    protected GameObject projectilePrefab;
    [SerializeField] protected AudioSource jamAudioSource;
    protected Vector3 direction;

    protected override void HandleShootInput(InputAction.CallbackContext context)
    {
        if (Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + 1 / fireRate;
            Shoot();
        }
        else
        {
            if (jamAudioSource)
            {
                jamAudioSource.Play();
            }
        }
    }

    protected override void Shoot()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        playerNet.NotifyProjectileShotServer(projectilePrefab, firePoint.position, -firePoint.up * speed, Damage, maxDistance);
        playerNet.NotifyMuzzleFlashServer();
        
        if (canPlayShootSound)
        {
            playerNet.ProjectileWeaponSound();
        }

        CurrentAmmo--;
    }

    public void PlaySound()
    {
        shootAudioSource.PlayOneShot(shootAudioClip);
    }

}
