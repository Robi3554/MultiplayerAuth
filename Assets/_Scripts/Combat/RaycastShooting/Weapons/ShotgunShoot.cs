using FishNet.Object;
using UnityEngine;

public class ShotgunShoot : SingleShot
{
    [SerializeField]
    private int pelletCount;
    [SerializeField]
    private float spreadAngle;

    [ServerRpc]
    protected override void Shoot()
    {
        if (!canShoot) return;

        Vector3 origin = firePoint.position;

        for(int i = 0; i < pelletCount; i++)
        {
            Vector3 dir = GetSpreadDirection(-firePoint.up, spreadAngle);
            if (Physics.Raycast(origin, dir, out RaycastHit hit, Mathf.Infinity, combinedLayer))
            {
                HitPlayer(hit.transform.GetComponent<NetworkObject>());
                ShowShotLineObserversRpc(origin, hit.point);
            }
            else
            {
                ShowShotLineObserversRpc(origin, origin + dir * maxDistance);
            }
        }
        if (shootAudioSource && shootSound)
        {
            shootAudioSource.PlayOneShot(shootSound);
        }

        currentAmmo--;
    }
    private Vector3 GetSpreadDirection(Vector3 forward, float angle)
    {
        float spreadRadius = Mathf.Tan(spreadAngle * Mathf.Deg2Rad);

        Vector2 rPoint = Random.insideUnitCircle * spreadRadius;

        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;

        Vector3 up = Vector3.Cross(right, forward).normalized;

        Vector3 spread = (forward + right * rPoint.x +  up * rPoint.y).normalized;

        return spread;
    }
}
