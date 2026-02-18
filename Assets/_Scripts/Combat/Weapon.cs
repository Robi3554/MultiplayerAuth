using System.Collections;
using GameKit.Dependencies.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class Weapon : MonoBehaviour
{
    protected TMP_Text ammoText;

    protected int CurrentAmmo
    {
        get => currentAmmo;
        set
        {
            currentAmmo = value;

            if (currentAmmo <= 0)
            {
                reloadCoroutine = StartCoroutine(ReloadClient());
                playerNet?.NotifyReloadServer(maxAmmo);
            }
        }
    }

    protected int currentAmmo;

    [SerializeField]
    protected Transform firePoint;

    protected PlayerStats playerStats;

    [SerializeField]
    protected float speed;
    [SerializeField]
    protected float maxDistance;
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
    protected ParticleSystem muzzleFlash;
    
    [SerializeField]
    protected BoxCollider wallCheckCollider;
    
    protected Coroutine reloadCoroutine;

    protected bool canShoot = true;
    protected float nextShootTime = 0f;
    protected bool canPlayShootSound;
    protected bool canPlayReloadSound;
    protected bool isReloading;

    protected LayerMask combinedLayer;

    protected PlayerNetworkInitializer playerNet;

    protected virtual void Start()
    {
        playerNet = GetComponentInParent<PlayerNetworkInitializer>();
        currentAmmo = maxAmmo;
        playerStats = GetComponentInParent<PlayerStats>();
    }

    protected virtual void OnEnable()
    {
        InitializeWeapon();
    }

    private void OnDisable()
    {
        ammoText = null;
    }

    public void OnDamage(InputAction.CallbackContext context)
    {
        if (!this.isActiveAndEnabled || !playerNet.IsOwner || !context.performed || currentAmmo <= 0) return;
        
        var colliders = Physics.OverlapBox(wallCheckCollider.bounds.center,
            wallCheckCollider.size.Multiply(wallCheckCollider.transform.lossyScale) / 2,
            wallCheckCollider.transform.rotation, wallCheckCollider.includeLayers);
        if (colliders.Length > 0) return;

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadAudioSource.Stop();
        }
        isReloading = false;

        HandleShootInput(context);
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (!this.isActiveAndEnabled || !playerNet.IsOwner) return;

        if (context.performed && currentAmmo != maxAmmo)
        {
            Reload();
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

    protected abstract void Shoot();

    protected void Reload()
    {
        if (isReloading) return;
        
        reloadCoroutine = StartCoroutine(ReloadClient());
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
        canPlayReloadSound = reloadAudioSource;

        reloadAudioSource.pitch = reloadAudioSource.clip.length / reloadTime;

        StartCoroutine(WeaponChangeDelay(afterChangeDelay));
    }

    public void OnDeathReload()
    {
        Debug.Log("Start reload after death");
        currentAmmo = maxAmmo;
    }

    protected abstract void HandleShootInput(InputAction.CallbackContext context);

    protected int Damage => damage * playerStats.damageMult;
    
    public void PlayMuzzleFlash()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
    }
}
