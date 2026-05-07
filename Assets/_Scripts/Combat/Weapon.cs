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

    protected ChangeWeapons cw;

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

    [SerializeField]
    protected WeaponHUD weaponHUD;
    
    protected Coroutine reloadCoroutine;

    protected bool isOnSwapCooldown = true;
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
        cw = GetComponentInParent<ChangeWeapons>();
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
        if (!this.isActiveAndEnabled || !playerNet.IsOwner || !context.performed || currentAmmo <= 0 || playerStats.isRespawning.Value) return;
        
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

    protected virtual void Update()
    {
        if (playerNet == null || !playerNet.IsOwner)
            return;

        if (ammoText == null)
        {
            // Use the player's own HUD reference instead of scene-global Find.
            // GameObject.Find returns the first ACTIVE match in the entire scene, so
            // in a multi-player game it silently resolves to another player's HUD if
            // that player's SetActive(false) hasn't fired yet — causing the ammo text
            // to target a disabled canvas element that is never visible.
            var hud = playerNet.PlayerHUD;
            if (hud == null) return; // HUD not assigned in prefab, retry next frame
            ammoText = hud.transform
                .Find("Player Ammo (1)")?.transform
                .Find("Ammo Text")?.GetComponent<TMP_Text>();
        }

        if (ammoText != null)
            ammoText.text = $"{currentAmmo}";
    }

    protected abstract void Shoot();

    protected void Reload()
    {
        if (isReloading || playerStats.isRespawning.Value) return;
        
        weaponHUD.StartCooldown(reloadTime);
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

    protected IEnumerator WeaponSwapCooldown(float delay)
    {
        yield return new WaitForSeconds(delay);
        isOnSwapCooldown = false;
    }

    public void InitializeWeapon()
    {
        isOnSwapCooldown = true;

        // Reset ammoText so Update() always re-fetches it with the correct ownership
        // context. InitializeWeapon is called from OnEnable(), which fires before
        // Start() and before OnStartClient() — at that point playerNet is null and
        // other players' PlayerHUDs are still active, so Find("PlayerHUD") would
        // silently return the wrong player's HUD and cache a stale reference.
        ammoText = null;

        nextShootTime = 0f;
        canPlayShootSound = shootAudioSource && shootAudioClip;
        canPlayReloadSound = reloadAudioSource;

        reloadAudioSource.pitch = reloadAudioSource.clip.length / reloadTime;

        weaponHUD.StartCooldown(afterChangeDelay);

        StartCoroutine(WeaponSwapCooldown(afterChangeDelay));
    }

    public virtual void OnDeathReload()
    {
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
