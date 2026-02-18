using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RailGunShoot : RaycastShoot
{
    private PredictionMoving pm;
    private ChangeWeapons cw;

    [SerializeField]
    private float
        stopTime,
        shootTime;

    [SerializeField]
    protected AudioSource aimAudioSource;
    [SerializeField]
    protected AudioClip aimAudioClip;

    private bool isShooting;

    protected override void HandleShootInput(InputAction.CallbackContext context)
    {
        if (Time.time >= nextShootTime && !isShooting)
        {
            nextShootTime = Time.time + 1/fireRate;
            StartCoroutine(StopAndShoot());
        }
    }

    private void Awake()
    {
        pm = GetComponentInParent<PredictionMoving>();
        cw = GetComponentInParent<ChangeWeapons>();
    }

    private IEnumerator StopAndShoot()
    {
        pm.canMove = false;
        isShooting = true;
        cw.canChange = false;
        
        pm.SetRunAnimFalse();

        aimAudioSource.PlayOneShot(aimAudioClip);
        yield return new WaitForSeconds(shootTime);

        Shoot();

        yield return new WaitForSeconds(stopTime);

        pm.canMove = true;
        isShooting = false;
        cw.canChange = true;
    }
}
