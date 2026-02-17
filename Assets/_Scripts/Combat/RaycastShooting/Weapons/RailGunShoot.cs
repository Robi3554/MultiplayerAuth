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
    protected AudioSource loadAudioSource;
    [SerializeField]
    protected AudioClip loadAudioClip;

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
    }

    private IEnumerator StopAndShoot()
    {
        pm.canMove = false;
        pm.canDash = false;
        isShooting = true;
        
        pm.SetRunAnimFalse();

        loadAudioSource.PlayOneShot(loadAudioClip);
        yield return new WaitForSeconds(shootTime);

        Shoot();

        yield return new WaitForSeconds(stopTime);

        pm.canMove = true;
        pm.canDash = true;
        isShooting = false;
    }
}
