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
    protected LineRenderer lineRenderer;

    [SerializeField]
    protected float maxDistance = 100f;
    [SerializeField]
    protected float fireRate;
    [SerializeField]
    protected int damage;

    protected bool canShoot = true;

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
            HitPlayer(hit.transform.GetComponent<NetworkObject>());
        }

        StartCoroutine(ShowShotLine(origin, hitPosition));

        StartCoroutine(CanShootUpdater());
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

        PlayerManager.Instance.DamagePlayer(targetId, damage, attackerId);
    }

    protected IEnumerator CanShootUpdater()
    {
        canShoot = false;

        yield return new WaitForSeconds(fireRate);

        canShoot = true;
    }

    protected IEnumerator ShowShotLine(Vector3 start, Vector3 end)
    {
        if (lineRenderer == null)
            yield break;

        LineRenderer tempLine = Instantiate(lineRenderer, lineRenderer.transform.parent);
        tempLine.enabled = true;
        tempLine.SetPosition(0, start);
        tempLine.SetPosition(1, end);

        yield return new WaitForSeconds(0.05f);

        Destroy(tempLine.gameObject);
    }

    protected abstract void HandleShootInput();
}
