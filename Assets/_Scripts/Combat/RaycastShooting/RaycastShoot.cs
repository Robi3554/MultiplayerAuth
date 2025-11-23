using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class RaycastShoot : MonoBehaviour
{
    private TMP_Text ammoText;

    protected int CurrentAmmo
    {
        get => _currentAmmo;
        set
        {
            var prevAmmo = _currentAmmo;
            _currentAmmo = Mathf.Clamp(value, 0, maxAmmo);

            canShoot = _currentAmmo != 0;
            if (prevAmmo != _currentAmmo && _currentAmmo == 0)
            {
                Reload();
            }
        }
    }

    private int _currentAmmo;

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
    
    [SerializeField]
    protected AudioSource shootAudioSource;
    [SerializeField]
    protected AudioClip shootAudioClip;
    [SerializeField]
    protected AudioSource reloadAudioSource;
    
    protected bool canShoot;
    protected float nextShootTime = 0f;
    private bool canPlayShootSound;
    protected bool canPlayReloadSound;
    protected bool isReloading;
    private Coroutine _reloadCoroutine;

    protected LayerMask combinedLayer;

    private PlayerNetworkInitializer playerNet;

    private void Start()
    {
        combinedLayer = playerLayer | wallLayer;       
        playerNet = GetComponentInParent<PlayerNetworkInitializer>();
        InitializeWeapon();
    }

    protected virtual void OnEnable()
    {
        CurrentAmmo = maxAmmo;
        DelayWeaponUse();
    }

    public void OnDamage(InputAction.CallbackContext context)
    {
        if (!playerNet || !playerNet.IsOwner)
            return;
        
        if (!this.isActiveAndEnabled || !context.performed || !canShoot) return;

        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            reloadAudioSource.Stop();
            isReloading = false;
        }
        
        HandleShootInput(context);
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (!playerNet || !playerNet.IsOwner)
            return;
        
        if (!this.isActiveAndEnabled) return;

        if (context.performed && CurrentAmmo != maxAmmo && !isReloading)
        {
            Reload();
        }
    }

    protected void Update()
    {
        if (!playerNet || !playerNet.IsOwner)
            return;

        ammoText.text = $"{CurrentAmmo}/{maxAmmo}";
    }

    protected virtual void Shoot()
    {
        Vector3 origin = firePoint.position;
        Vector3 direction = -firePoint.up;
        Vector3 hitPosition = origin + direction * maxDistance;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, combinedLayer))
        {
            hitPosition = hit.point;

            var hitNetObj = hit.transform.GetComponent<FishNet.Object.NetworkObject>();
            if (hitNetObj != null)
                playerNet?.NotifyHitServer(hitNetObj, damage);
        }

        // locally show the bullet
        //ShowShotLine(origin, hitPosition);

        // tell the server to show the tracer for others
        playerNet?.NotifyShotServer(origin, hitPosition);

        CurrentAmmo--;
    }


    public void CreateBulletEffect(Vector3 start, Vector3 end)
    {
        if (shotLinePrefab == null)
            return;

        if (canPlayShootSound)
        {
            shootAudioSource.PlayOneShot(shootAudioClip);
        }
        GameObject tempGO = Instantiate(shotLinePrefab, firePoint.position, Quaternion.identity);
        tempGO.GetComponent<LineProjectile>().Initialize(speed, start, end);
    }

    protected void Reload()
    {
        _reloadCoroutine = StartCoroutine(ReloadClient());
        playerNet?.NotifyReloadServer(maxAmmo);
    }

    protected IEnumerator ReloadClient()
    {
        isReloading = true;
        
        if (canPlayReloadSound)
        {
            reloadAudioSource.Play();
        }
        yield return new WaitForSeconds(reloadTime);
        CurrentAmmo = maxAmmo;
        
        isReloading = false;
    }

    protected IEnumerator LockWeaponUse(float delay)
    {
        canShoot = false;
        yield return new WaitForSeconds(delay);
        canShoot = true;
    }

    public void InitializeWeapon()
    {
        ammoText = GameObject.Find("PlayerHUD").transform.Find("Player Ammo").transform.Find("Ammo Text").GetComponent<TMP_Text>();

        nextShootTime = 0f;
        canPlayShootSound = shootAudioSource && shootAudioClip;
        canPlayReloadSound = reloadAudioSource;

        if (reloadAudioSource)
        {
            reloadAudioSource.pitch = reloadAudioSource.clip.length / reloadTime;
        }
    }

    private void DelayWeaponUse()
    {
        StopCoroutine(LockWeaponUse(afterChangeDelay));
        StartCoroutine(LockWeaponUse(afterChangeDelay));
    }

    protected abstract void HandleShootInput(InputAction.CallbackContext context);
}
