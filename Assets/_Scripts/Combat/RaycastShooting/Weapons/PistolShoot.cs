using UnityEngine;

public class PistolShoot : RaycastShoot
{
    protected override void HandleShootInput()
    {
        Shoot();
    }
}
