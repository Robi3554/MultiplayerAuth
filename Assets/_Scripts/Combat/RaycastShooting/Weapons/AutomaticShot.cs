using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

public class AutomaticShot : RaycastShoot
{
    protected override void HandleShootInput(InputAction.CallbackContext context)
    {
        if (Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + 1/fireRate;
            Shoot();
        }
    }
}
