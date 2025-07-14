using System.Collections;
using UnityEngine;
using FishNet.Object;

public abstract class RaycastShoot : NetworkBehaviour
{
    [SerializeField]
    protected KeyCode shootKey = KeyCode.Mouse0;
    [SerializeField]
    protected LayerMask playerLayer;
    [SerializeField]
    protected Transform firePoint;
    [SerializeField]
    protected GameObject shotLinePrefab;

    [SerializeField]
    protected float maxDistance = 100f;
    [SerializeField]
    protected float fireRate;
    [SerializeField]
    protected int damage;

    protected bool canShoot = true;

    protected float nextShootTime = 0f;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!base.IsOwner)
            return;
    }

    protected void Update()
    {
        if (!base.IsOwner)
            return;

        HandleShootInput();
    }

    protected virtual void Shoot()
    {
        Vector3 origin = firePoint.position;
        Vector3 direction = firePoint.right;

        Vector3 hitPosition = origin + direction * maxDistance;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, Mathf.Infinity, playerLayer))
        {
            hitPosition = hit.point;
            Debug.Log("Raycast hit at position: " + hitPosition);
            HitPlayer(hit.transform.GetComponent<NetworkObject>());
        }
        else
        {
            Debug.Log("Raycast did not hit anything. Drawing to max distance.");
        }

        StartCoroutine(ShowShotLine(origin, hitPosition));
    }

    [ServerRpc(RequireOwnership = false)]
    protected void HitPlayer(NetworkObject playerHit)
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

        Debug.Log($"Player {attackerId} hit Player {targetId} for {damage} damage.");
        PlayerManager.Instance.DamagePlayer(targetId, damage, attackerId);
    }

    protected IEnumerator ShowShotLine(Vector3 start, Vector3 end)
    {
        if (shotLinePrefab == null)
        {
            yield break;
        }

        GameObject tempGO = Instantiate(shotLinePrefab, firePoint.position, Quaternion.identity, firePoint.parent);

        LineRenderer tempLine = tempGO.GetComponent<LineRenderer>();

        if (tempLine == null)
        {
            Debug.LogWarning("Shot Line Prefab has no LineRenderer component!");
            Destroy(tempGO);
            yield break;
        }

        tempLine.enabled = true;
        tempLine.SetPosition(0, start);
        tempLine.SetPosition(1, end);

        yield return new WaitForSeconds(0.05f);

        Destroy(tempLine.gameObject);
    }

    protected abstract void HandleShootInput();
}
