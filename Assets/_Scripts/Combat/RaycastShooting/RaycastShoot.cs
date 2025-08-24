using System.Collections;
using FishNet.Object;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public abstract class RaycastShoot : NetworkBehaviour
{
    private TMP_Text ammoText;

    [SerializeField]
    protected KeyCode shootKey = KeyCode.Mouse0;
    [SerializeField]
    protected LayerMask playerLayer;
    [SerializeField]
    protected LayerMask wallLayer;
    [SerializeField]
    protected Transform firePoint;
    [SerializeField]
    protected GameObject shotLinePrefab;

    [SerializeField]
    protected float speed = 100f;
    [SerializeField]
    protected float maxDistance = 100f;
    [SerializeField]
    protected float fireRate;
    [SerializeField]
    protected int damage;
    [SerializeField]
    protected int maxAmmo;
    [SerializeField]
    protected int currentAmmo;
    [SerializeField]
    protected float reloadTime;

    private bool isReloading = false;

    protected float nextShootTime = 0f;

    protected LayerMask combinedLayer;

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    private void Start()
    {
        currentAmmo = maxAmmo;

        combinedLayer = playerLayer | wallLayer;
    }

    protected void OnEnable()
    {
        ammoText = GameObject.Find("PlayerHUD").transform.Find("Ammo Text").GetComponent<TMP_Text>();

        isReloading = false;
        nextShootTime = 0f;

        if (IsClientInitialized && base.IsOwner == false && NetworkObject != null)
        {
            RequestWeaponOwnershipServerRpc(NetworkObject);
        }
    }

    private void OnDisable()
    {
        ammoText = null;
    }

    protected void Update()
    {
        if (!base.IsOwner)
            return;

        ammoText.text = currentAmmo.ToString() + '/' + maxAmmo.ToString();

        if (isReloading)
            return;

        if (currentAmmo <= 0f || Input.GetKeyDown(KeyCode.R))
            StartCoroutine(Reload());
        else if(currentAmmo > 0)
            HandleShootInput();
    }

    protected virtual void Shoot()
    {
        Vector3 origin = firePoint.position;
        Vector3 direction = -firePoint.up;

        Vector3 hitPosition = origin + direction * maxDistance;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, Mathf.Infinity, combinedLayer))
        {
            hitPosition = hit.point;
            Debug.Log("Raycast hit at position: " + hitPosition);
            HitPlayer(hit.transform.GetComponent<NetworkObject>());
        }
        else
        {
            Debug.Log("Raycast did not hit anything. Drawing to max distance.");
        }

        currentAmmo--;
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

        int targetId = playerHit.Owner.ClientId;
        int attackerId = attackerNetObj.Owner.ClientId;

        Debug.Log($"Player {attackerId} hit Player {targetId} for {damage} damage.");
        PlayerManager.Instance.DamagePlayer(targetId, damage, attackerId);
    }

    protected IEnumerator ShowShotLine(Vector3 start, Vector3 end)
    {
        if (shotLinePrefab == null)
            yield break;

        GameObject tempGO = Instantiate(shotLinePrefab, firePoint.position, Quaternion.identity, firePoint.parent);

        LineRenderer lr = tempGO.GetComponent<LineRenderer>();
        if (lr == null)
        {
            Debug.LogWarning("Shot Line Prefab has no LineRenderer!");
            Destroy(tempGO);
            yield break;
        }

        lr.positionCount = 2;
        lr.enabled = true;

        float distance = Vector3.Distance(start, end);
        float travelTime = distance / speed;
        float elapsed = 0f;

        Vector3 direction = (end - start).normalized;

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / travelTime;
            Vector3 currentPos = Vector3.Lerp(start, end, t);

            lr.SetPosition(0, currentPos);
            lr.SetPosition(1, currentPos - (direction * 0.5f));

            yield return null;
        }

        Destroy(tempGO);
    }


    protected IEnumerator Reload()
    {
        isReloading = true;

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;

        isReloading = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestWeaponOwnershipServerRpc(NetworkObject weaponNetObj)
    {
        if (weaponNetObj != null)
        {
            weaponNetObj.GiveOwnership(Owner);
        }
    }
    protected abstract void HandleShootInput();
}
