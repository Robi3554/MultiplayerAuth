using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public abstract class RaycastShoot : NetworkBehaviour
{
    private TMP_Text ammoText;

    protected readonly SyncVar<int> currentAmmo = new();

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
    protected float reloadTime;
    [SerializeField]
    protected float afterChangeDelay;

    private bool isReloading = false;

    protected bool canShoot = true;

    protected float nextShootTime = 0f;

    protected LayerMask combinedLayer;

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    private void Start()
    {
        combinedLayer = playerLayer | wallLayer;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        currentAmmo.Value = maxAmmo;
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

        StartCoroutine(WeaponChangeDelay(afterChangeDelay));
    }

    private void OnDisable()
    {
        ammoText = null;
    }

    protected void Update()
    {
        if (!base.IsOwner)
            return;

        ammoText.text = currentAmmo.Value.ToString() + '/' + maxAmmo.ToString();

        if (isReloading)
            return;

        if (currentAmmo.Value <= 0f || Input.GetKeyDown(KeyCode.R))
            ReloadServerRpc();
        else if (currentAmmo.Value > 0)
            HandleShootInput();
    }

    protected virtual void Shoot()
    {
        if(!canShoot) return;

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

        currentAmmo.Value--;
        ShowShotLineObserversRpc(origin, hitPosition);
    }

    [ServerRpc]
    protected virtual void ShootObserverRpc()
    {
        Shoot();
    }

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

    [ObserversRpc]
    protected void ShowShotLineObserversRpc(Vector3 start, Vector3 end)
    {
        ShowShotLine(start, end);
    }

    protected void ShowShotLine(Vector3 start, Vector3 end)
    {
        if (shotLinePrefab == null)
            return;

        GameObject tempGO = Instantiate(shotLinePrefab, firePoint.position, Quaternion.identity);

        tempGO.GetComponent<LineProjectile>().Initialize(speed, start, end);
    }

    protected IEnumerator Reload()
    {
        isReloading = true;

        yield return new WaitForSeconds(reloadTime);

        currentAmmo.Value = maxAmmo;

        isReloading = false;
    }

    [ServerRpc]
    protected void ReloadServerRpc()
    {
        StartCoroutine(Reload());
    }

    protected IEnumerator WeaponChangeDelay(float delay)
    {
        canShoot = false;

        yield return new WaitForSeconds(delay);

        canShoot = true;

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
