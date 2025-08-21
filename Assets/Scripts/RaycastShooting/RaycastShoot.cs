using System.Collections;
using UnityEngine;
using FishNet.Object;
using TMPro;

public abstract class RaycastShoot : NetworkBehaviour
{
    private TMP_Text ammoText;

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
    [SerializeField]
    protected int maxAmmo;
    [SerializeField]
    protected int currentAmmo;
    [SerializeField]
    protected float reloadTime;

    private bool isReloading = false;

    protected float nextShootTime = 0f;

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    private void Start()
    {
        currentAmmo = maxAmmo;
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
