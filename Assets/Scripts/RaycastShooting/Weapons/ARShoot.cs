using System.Globalization;
using UnityEngine;

public class ARShoot : RaycastShoot
{
    protected override void HandleShootInput()
    {
        if (Input.GetKey(shootKey) && canShoot)
        {
            Shoot();
        }
    }
}
