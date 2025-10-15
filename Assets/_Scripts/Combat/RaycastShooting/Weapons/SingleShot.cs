using UnityEngine;

public class SingleShot : RaycastShoot
{
    protected override void HandleShootInput()
    {
        if (Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + fireRate;
            Shoot();
        }
    }
}
