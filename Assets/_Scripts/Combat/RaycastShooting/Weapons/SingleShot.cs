using UnityEngine;
using UnityEngine.InputSystem;

public class SingleShot : RaycastShoot
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
