using System.Globalization;
using UnityEngine;

public class AutomaticShot : RaycastShoot
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
