using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RaycastShoot : Weapon
{
    [SerializeField] 
    protected LayerMask playerLayer;
    [SerializeField] 
    protected LayerMask wallLayer;
    [SerializeField] 
    protected GameObject shotLinePrefab;

    protected override void Start()
    {
        base.Start();
        combinedLayer = playerLayer | wallLayer;       
    }

    private void OnDisable()
    {
        ammoText = null;
    }

    protected override void Shoot()
    {
        if (!canShoot) return;

        Vector3 origin = firePoint.position;
        Vector3 direction = -firePoint.up;
        Vector3 hitPosition = origin + direction * maxDistance;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, combinedLayer))
        {
            hitPosition = hit.point;

            var hitNetObj = hit.transform.GetComponent<FishNet.Object.NetworkObject>();
            if (hitNetObj != null)
                playerNet?.NotifyHitServer(hitNetObj, damage);
        }

        // tell the server to show the tracer for others
        playerNet?.NotifyShotServer(origin, hitPosition);

        CurrentAmmo--;
    }


    public void CreateBulletEffect(Vector3 start, Vector3 end)
    {
        if (shotLinePrefab == null)
            return;

        if (canPlayShootSound)
        {
            shootAudioSource.PlayOneShot(shootAudioClip);
        }
        GameObject tempGO = Instantiate(shotLinePrefab, firePoint.position, Quaternion.identity);
        tempGO.GetComponent<LineProjectile>().Initialize(speed, start, end);
    }

    protected override void HandleShootInput(InputAction.CallbackContext context)
    {
        if (Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + 1 / fireRate;
            Shoot();
        }
    }
}
