using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class RaycastShoot : MonoBehaviour
{
    private TMP_Text ammoText;

    protected int CurrentAmmo
    {
        get => currentAmmo;
        set
        {
            currentAmmo = value;

            if(currentAmmo <= 0)
            {
                StartCoroutine(Reload());
                playerNet?.NotifyReloadServer(maxAmmo);
            }
        }
    }

    protected int currentAmmo;

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
    [SerializeField]
    protected AudioClip reloadAudioClip;
    
    private bool isReloading = false;
    protected bool canShoot = true;
    protected float nextShootTime = 0f;
    private bool canPlayShootSound;
    private bool canPlayReloadSound;

    protected LayerMask combinedLayer;

    private PlayerNetworkInitializer playerNet;

    private void Start()
    {
        combinedLayer = playerLayer | wallLayer;       
        playerNet = GetComponentInParent<PlayerNetworkInitializer>();
    }

    protected void OnEnable()
    {
        currentAmmo = maxAmmo;
        InitializeWeapon();
        if(currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            playerNet?.NotifyReloadServer(maxAmmo);
        }
    }

    private void OnDisable()
    {
        ammoText = null;
        isReloading = false;
    }

    public void OnDamage(InputAction.CallbackContext context)
    {
        if (!this.isActiveAndEnabled) return;

        if (isReloading)
            return;

        if (context.performed)
        {
            if (currentAmmo <= 0)
            {
                StartCoroutine(Reload());
                playerNet?.NotifyReloadServer(maxAmmo);
            }
            else if (currentAmmo > 0)
            {
                HandleShootInput();
            }
        }
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (!this.isActiveAndEnabled) return;

        if (context.performed && currentAmmo != maxAmmo)
        {
            StartCoroutine(Reload());
            playerNet?.NotifyReloadServer(maxAmmo);
        }
    }

    protected void Update()
    {
        if (playerNet != null && !playerNet.IsOwner)
            return;

        if (ammoText == null)
        {
            ammoText = GameObject.Find("PlayerHUD").transform.Find("Player Ammo").transform.Find("Ammo Text").GetComponent<TMP_Text>();
        }

        ammoText.text = $"{currentAmmo}/{maxAmmo}";
    }

    protected virtual void Shoot()
    {
        if (!canShoot) return;

        Vector3 origin = firePoint.position;
        Vector3 direction = -firePoint.up;
        Vector3 hitPosition = origin + direction * maxDistance;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, Mathf.Infinity, combinedLayer))
        {
            hitPosition = hit.point;
            Debug.Log("Raycast hit at position: " + hitPosition);

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

    protected IEnumerator Reload()
    {
        isReloading = true;
        if (canPlayReloadSound)
        {
            reloadAudioSource.PlayOneShot(reloadAudioClip);
        }
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
    }

    protected IEnumerator WeaponChangeDelay(float delay)
    {
        canShoot = false;
        yield return new WaitForSeconds(delay);
        canShoot = true;
    }

    public void InitializeWeapon()
    {
        if (ammoText == null)
            ammoText = GameObject.Find("PlayerHUD").transform.Find("Player Ammo").transform.Find("Ammo Text").GetComponent<TMP_Text>();

        canShoot = false;
        nextShootTime = 0f;
        canPlayShootSound = shootAudioSource && shootAudioClip;
        canPlayReloadSound = reloadAudioSource && reloadAudioClip;
        
        reloadAudioSource.pitch = reloadAudioClip.length / reloadTime;
        
        StartCoroutine(WeaponChangeDelay(afterChangeDelay));
    }

    protected abstract void HandleShootInput();
}
