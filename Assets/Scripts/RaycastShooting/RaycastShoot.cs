using System.Collections;
using UnityEngine;
using FishNet.Object;
using System;

public class RaycastShoot : NetworkBehaviour
{
    [SerializeField] 
    private int damage;
    [SerializeField] 
    private float fireRate;
    [SerializeField] 
    private KeyCode shootKey = KeyCode.Mouse0;
    [SerializeField] 
    private LayerMask playerLayer;
    [SerializeField] 
    private Transform firePoint;
    [SerializeField]
    private LineRenderer lineRenderer;

    bool canShoot = true;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!base.IsOwner)
            return;
    }

    private void Update()
    {
        if (!base.IsOwner)
            return;

        if (Input.GetKey(shootKey) && canShoot)
            Shoot();
    }

    void Shoot()
    {
        Vector3 origin = firePoint.position;
        Vector3 direction = firePoint.right;
        float maxDistance = 100f;

        Vector3 hitPosition = origin + direction * maxDistance;

        if (Physics.Raycast(firePoint.transform.position, firePoint.transform.right, out RaycastHit hit, Mathf.Infinity, playerLayer))
        {
            HitPlayer(hit.transform.GetComponent<NetworkObject>());
        }

        StartCoroutine(ShowShotLine(origin, hitPosition));

        StartCoroutine(CanShootUpdater());
    }

    [ServerRpc(RequireOwnership = false)]
    void HitPlayer(NetworkObject playerHit)
    {
        NetworkObject attackerNetObj = GetComponentInParent<NetworkObject>();

        Debug.Log("Attacker NetworkObject: " + attackerNetObj);
        Debug.Log("Target NetworkObject: " + playerHit);

        if (playerHit == null || attackerNetObj == null)
        {
            Debug.LogWarning("One of the NetworkObjects is null!");
            return;
        }

        int targetId = playerHit.ObjectId;
        int attackerId = attackerNetObj.ObjectId;

        PlayerManager.Instance.DamagePlayer(targetId, damage, attackerId);
    }

    IEnumerator CanShootUpdater()
    {
        canShoot = false;

        float waitTime = 1f / Mathf.Max(fireRate, 0.0001f);
        yield return new WaitForSeconds(waitTime);

        canShoot = true;
    }

    IEnumerator ShowShotLine(Vector3 start, Vector3 end)
    {
        if (lineRenderer == null)
            yield break;

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        yield return new WaitForSeconds(0.05f);
        lineRenderer.enabled = false;
    }
}
