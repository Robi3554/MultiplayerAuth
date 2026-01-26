using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : NetworkBehaviour
{
    public readonly SyncVar<string> username = new SyncVar<string>("");
    public readonly SyncVar<int> health = new SyncVar<int>(100);
    public readonly SyncVar<int> kills = new SyncVar<int>(0);
    public readonly SyncVar<int> deaths = new SyncVar<int>(0);
    public int damageMult = 1;
    
    [SerializeField] private Animator animator;
    [SerializeField] private TMP_Text _usernameTextOnBillboard;
    [SerializeField] private AudioSource _hitAudioSource;
    [SerializeField] private AudioClip _hitAudioClip;
    [SerializeField] private GameObject _damageTakenVfx;
    
    [Header("UI stats")]
    [SerializeField] private TMP_Text _killText; 
    [SerializeField] private TMP_Text _deathText;
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private Slider _healthSlider;
    private TMP_Text healthText;
    private TMP_Text killText;
    private TMP_Text deathText;
    private bool _canPlayHitSound;

    private Slider healthSlider;

    public bool isRespawning = false;

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Notify scoreboard that this player spawned
        if (ScoreboardManager.Instance != null)
        {
            ScoreboardManager.Instance.RegisterPlayer(this);
        }

        if (IsOwner)
        {
            InitUI();
            if (healthText != null)
                healthText.text = health.Value.ToString();
            health.OnChange += OnHealthChanged;
            _canPlayHitSound = _hitAudioSource && _hitAudioClip;
        
            if (!string.IsNullOrEmpty(ConnectionInfo.username))
            {
                CmdSetUsername(ConnectionInfo.username);
            }
            else
            {
                CmdSetUsername("Player " + OwnerId); 
            }
            animator = gameObject.GetComponentInChildren<Animator>();
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        // Notify scoreboard that this player despawned
        if (ScoreboardManager.Instance != null)
        {
            ScoreboardManager.Instance.UnregisterPlayer(this);
        }

        if (IsOwner)
            health.OnChange -= OnHealthChanged;
    }
    
    [ServerRpc]
    private void CmdSetUsername(string username)
    {
        // Server-side validation - never trust client input
        string sanitized = SanitizeUsername(username);
        this.username.Value = sanitized;
        RpcSetUsername(sanitized);
    }

    /// <summary>
    /// Sanitizes a username to only allow alphanumeric characters, max 20 chars.
    /// </summary>
    private static string SanitizeUsername(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove all non-alphanumeric characters (letters and digits only)
        string sanitized = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-zA-Z0-9]", "");

        // Limit to max length
        const int MaxUsernameLength = 20;
        if (sanitized.Length > MaxUsernameLength)
            sanitized = sanitized.Substring(0, MaxUsernameLength);

        return sanitized;
    }
    
    [ObserversRpc(BufferLast = true)]
    private void RpcSetUsername(string username)
    {
        // This runs on all clients, including the host.
        // It sets the text on the billboard for everyone to see.
        _usernameTextOnBillboard.text = username;
    }

    void Update()
    {
        if (!IsOwner)
            return;

        if (killText != null)
            killText.text = kills.Value.ToString();

        if (deathText != null)
            deathText.text = deaths.Value.ToString();
    }

    public void TakeDamage(int damage)
    {
        if (isRespawning) return;
        
        SetHealth(damage);
        
        TargetHitSound(Owner);
        TargetShakeCamera(Owner, 0.5f, 0.1f);
        TargetDamagedVFX();
    }

    [Server]
    public void HealPlayer(int healAmount)
    {
        if ((health.Value + healAmount) >= 100)
        {
            Debug.Log("Player healed to 100Hp");
            health.Value = 100;
        }
        else
        {
            health.Value += healAmount;
            Debug.Log($"Player healed to {health.Value}");
        }
    }

    [Server]
    public void AddKill()
    {
        kills.Value++;
        
        // Notify game mode manager
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnPlayerKill(this);
        }
    }

    public void AddDeath()
    {
        deaths.Value++;
    }
    public void ResetHealth()
    {
        health.Value = 100;
    }

    private void SetHealth(int value)
    {
        health.Value = Mathf.Clamp(health.Value - value, 0, 100);
    }
    
    [TargetRpc]
    private void TargetHitSound(NetworkConnection target)
    {
        if (_canPlayHitSound)
        {
            _hitAudioSource.PlayOneShot(_hitAudioClip);
        }
    }

    [TargetRpc]
    private void TargetShakeCamera(NetworkConnection target, float amplitude, float duration)
    {
        CameraShake.Instance.ShakeCamera(amplitude, duration);
    }

    [ObserversRpc]
    private void TargetDamagedVFX()
    {
        if (_damageTakenVfx != null)
        {
            _damageTakenVfx.GetComponent<ParticleSystem>().Play();
        }
    }

    private void InitUI()
    {
        var playerHealth = GameObject.Find("PlayerHUD").transform.Find("Player Health");
        healthText = _healthText;
        healthSlider = _healthSlider;
        healthSlider.maxValue = health.Value;
        killText = _killText;
        deathText = _deathText;
    }

    private void OnHealthChanged(int previous, int current, bool asServer)
    {
        if (healthText != null)
        {
            healthText.text = current.ToString();
            healthSlider.value = health.Value;
        }
    }

    //Player head size code
    #region Head Size Change
    public void HeadSizeChange(NetworkObject obj, float multiplier)
    {
        ObserverHeadSizeChange(obj, multiplier);
    }

    [ObserversRpc]
    private void ObserverHeadSizeChange(NetworkObject obj, float multiplier)
    {
        Transform head = FindChild(obj.transform, "mixamorig:Head");

        StartCoroutine(ChangeHeadCo(head, multiplier));
    }

    private Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private IEnumerator ChangeHeadCo(Transform head, float multiplier)
    {
        var originalScale = head.localScale;

        head.localScale *= multiplier;

        yield return new WaitForSeconds(10f);

        head.localScale = originalScale;
    }
    #endregion

    //Player damage multiplier change
    #region Damage Mult
    public void ChangeMult(int multiplier)
    {
        ServerChangeMult(multiplier);
    }

    [ServerRpc]
    private void ServerChangeMult(int multiplier)
    {
        StartCoroutine(ChangeMultCo(multiplier));
    }

    private IEnumerator ChangeMultCo(int multiplier)
    {
        int oldMultiplier = damageMult;
        damageMult = multiplier;

        yield return new WaitForSeconds(10f);

        damageMult = oldMultiplier;
    }
    #endregion
}
