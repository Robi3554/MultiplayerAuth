using System.Collections;
using UnityEngine;

public class RailGunShoot : RaycastShoot
{
    private PredictionMoving pm;

    [SerializeField]
    private float
        stopTime,
        shootTime;

    protected override void HandleShootInput()
    {
        if (Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + fireRate;
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

        yield return new WaitForSeconds(shootTime);

        Shoot();

        yield return new WaitForSeconds(stopTime);

        pm.canMove = true;
    }
}
