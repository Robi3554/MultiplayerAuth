using System.Collections;
using FishNet.Object;
using GameKit.Dependencies.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

public class BurstShot : RaycastShoot
{
    [SerializeField]
    private float timeBetweenShots;
    [SerializeField]
    private int burstCount;

    private bool _canHandleShoot = true;

    protected override void HandleShootInput(InputAction.CallbackContext context)
    {
        if (CurrentAmmo > 0 && _canHandleShoot)
        {
            StartCoroutine(Burst(context.action));
        }
    }

    private IEnumerator Burst(InputAction action)
    {
        _canHandleShoot = false;
        
        for (int i = 0; i < burstCount; i++)
        {
            Shoot();

            yield return new WaitForSeconds(timeBetweenShots);
            
            if (CurrentAmmo <= 0)
            {
                _canHandleShoot = true;
                yield break;
            }
            
            var colliders = Physics.OverlapBox(wallCheckCollider.bounds.center,
                wallCheckCollider.size.Multiply(wallCheckCollider.transform.lossyScale) / 2,
                wallCheckCollider.transform.rotation, wallCheckCollider.includeLayers);
            if (colliders.Length > 0) yield break;
        }
        
        yield return new WaitForSeconds(1/fireRate);

        if (CurrentAmmo > 0)
        {

            var held = action.ReadValue<float>();
            if (held > 0)
            {
                StartCoroutine(Burst(action));
                yield break;
            }
        }

        _canHandleShoot = true;
    }
}
