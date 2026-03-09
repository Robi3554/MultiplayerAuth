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
    
    private bool _canCheckInput = true;

    protected override void OnEnable()
    {
        base.OnEnable();
        _canCheckInput = true;
    }
    protected override void HandleShootInput(InputAction.CallbackContext context)
    {
        if (_canCheckInput)
        {
            _canCheckInput = false;
            StartCoroutine(Burst(context.action));
        }
    }

    private IEnumerator Burst(InputAction action)
    {
        cw.canChange = false;

        for (int i = 0; i < burstCount; i++)
        {
            Shoot();
            yield return new WaitForSeconds(timeBetweenShots);
            
            var colliders = Physics.OverlapBox(wallCheckCollider.bounds.center,
                wallCheckCollider.size.Multiply(wallCheckCollider.transform.lossyScale) / 2,
                wallCheckCollider.transform.rotation, wallCheckCollider.includeLayers);
            if (colliders.Length > 0 || currentAmmo <= 0)
            {
                _canCheckInput = true;
                yield break;
            }
        }

        cw.canChange = true;

        yield return new WaitForSeconds(1/fireRate);

        if (currentAmmo > 0)
        {

            var held = action.ReadValue<float>();
            if (held > 0)
            {
                StartCoroutine(Burst(action));
                yield break;
            }
        }
        
        _canCheckInput = true;
    }
}
