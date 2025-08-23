using UnityEngine;

public class SingleShot : RaycastShoot
{
    protected override void HandleShootInput()
    {
        if (Input.GetKeyDown(shootKey) && Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + fireRate;
            Shoot();
        }
    }
}
