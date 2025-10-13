using UnityEngine;

public class SingleShot : RaycastShoot
{
    protected override void HandleShootInput()
    {
        if (Time.time >= nextShootTime && Input.GetKey(shootKey))
        {
            nextShootTime = Time.time + fireRate;
            Shoot();
        }
    }
}
