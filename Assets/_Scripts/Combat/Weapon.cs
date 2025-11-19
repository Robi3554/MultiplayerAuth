using System.Collections;
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
                StartCoroutine(ReloadClient());
                playerNet?.NotifyReloadServer(maxAmmo);
            }
        }
    }

    protected int currentAmmo;

    [SerializeField]
    protected Transform firePoint;

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
    protected AudioClip reloadAudioClip;

    protected bool canShoot = true;
    protected float nextShootTime = 0f;
    protected bool canPlayShootSound;
    protected bool canPlayReloadSound;

    protected LayerMask combinedLayer;

    protected PlayerNetworkInitializer playerNet;

    protected virtual void Start()
    {
        playerNet = GetComponentInParent<PlayerNetworkInitializer>();
    }

    protected virtual void OnEnable()
    {
        currentAmmo = maxAmmo;
        InitializeWeapon();
    }

    private void OnDisable()
    {
        ammoText = null;
    }

    public void OnDamage(InputAction.CallbackContext context)
    {
        if (!this.isActiveAndEnabled || !context.performed || currentAmmo <= 0) return;

        HandleShootInput(context);
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (!this.isActiveAndEnabled) return;

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

    protected virtual void Shoot()
    {
        
    }

    protected void Reload()
    {
        StartCoroutine(ReloadClient());
        playerNet?.NotifyReloadServer(maxAmmo);
    }

    protected IEnumerator ReloadClient()
    {
        if (canPlayReloadSound)
        {
            reloadAudioSource.PlayOneShot(reloadAudioClip);
        }
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
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

    protected abstract void HandleShootInput(InputAction.CallbackContext context);
}
