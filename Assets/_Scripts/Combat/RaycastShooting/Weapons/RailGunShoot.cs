using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RailGunShoot : RaycastShoot
{
    private PredictionMoving pm;

    [SerializeField]
    private float
        stopTime,
        shootTime;

    [SerializeField]
    protected AudioSource aimAudioSource;
    [SerializeField]
    protected AudioClip aimAudioClip;

    private bool isShooting;

    private Coroutine stopAndShootRoutine;

    protected override void HandleShootInput(InputAction.CallbackContext context)
    {
        if (Time.time >= nextShootTime && !isShooting)
        {
            nextShootTime = Time.time + 1/fireRate;
            stopAndShootRoutine = StartCoroutine(StopAndShoot());
        }
    }

    private void Awake()
    {
        pm = GetComponentInParent<PredictionMoving>();
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

    public override void OnDeathReload()
    {
        base.OnDeathReload();


        if (stopAndShootRoutine != null)
        {
            StopCoroutine(stopAndShootRoutine);
            stopAndShootRoutine = null;
        }

        pm.canMove = true;
        isShooting = false;
        cw.canChange = true;
    }
}
