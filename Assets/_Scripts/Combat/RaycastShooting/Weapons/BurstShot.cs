using System.Collections;
using FishNet.Object;
using UnityEngine;

public class BurstShot : RaycastShoot
{
    [SerializeField]
    private float timeBetweenShots;
    [SerializeField]
    private int burstCount;

    protected override void HandleShootInput()
    {
        if (Input.GetKeyDown(shootKey) && Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + fireRate;
            StartCoroutine(Burst());
        }
    }

    private IEnumerator Burst()
    {
        for(int i = 0; i < burstCount; i++)
        {
            ShootObserverRpc();

            yield return new WaitForSeconds(timeBetweenShots);
        }
    }
}
