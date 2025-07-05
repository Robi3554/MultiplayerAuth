using UnityEngine;

public class PistolShoot : RaycastShoot
{
    protected override void HandleShootInput()
    {
        if (Input.GetKeyDown(shootKey) && canShoot)
        {
            Shoot();
        }
    }
}
