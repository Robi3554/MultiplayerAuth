using System.Globalization;
using UnityEngine;

public class AutomaticShot : RaycastShoot
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
