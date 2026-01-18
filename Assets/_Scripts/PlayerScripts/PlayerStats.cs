using System;
using FishNet;
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
    
    [SerializeField] private Animator animator;
    [SerializeField] private TMP_Text _usernameTextOnBillboard;
    [SerializeField] private AudioSource _hitAudioSource;
    [SerializeField] private AudioClip _hitAudioClip;
    [SerializeField] private GameObject _damageTakenVfx;
    [SerializeField] private GameObject deathScreenUI;
    [SerializeField] private TMP_Text respawnTimerText; // optional: assign in inspector (child text that shows time)
    [SerializeField] private float respawnDuration = 5f; // default countdown length
    private Coroutine _respawnCoroutine;
    
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
        this.username.Value = username;
        RpcSetUsername(username);
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
        
        if (health.Value <= 0)
        {
            OnPlayerDeath();
        }
        
        TargetHitSound(Owner);
        TargetShakeCamera(Owner, 0.5f, 0.1f);
        TargetDamagedVFX();
    }

    private void OnPlayerDeath()
    {
        isRespawning = true;
        ShowDeathScreen();
        PlayDeathAnimation();
    }
    private void PlayDeathAnimation()
    {
        if(animator == null)
        {
            Debug.LogError("No animator in PlayerStats::PlayDeatAnimation");
            return;
        }
        animator.SetTrigger("isDying");
        Debug.Log("Death animation triggered on server");
    }
    
    private void ShowDeathScreen()
    {
        if (deathScreenUI == null) return;
        deathScreenUI.SetActive(true);

        // start countdown (stop previous if any)
        if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);
        _respawnCoroutine = StartCoroutine(RespawnCountdown(respawnDuration));
    }
    
    public void HideDeathScreen()
    {
        if (deathScreenUI != null)
        {
            deathScreenUI.SetActive(false);
           if (_respawnCoroutine != null)
           {
               StopCoroutine(_respawnCoroutine);
               _respawnCoroutine = null;
           }
           // optional: reset text
           if (respawnTimerText != null) respawnTimerText.text = "";
        }
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

    public void AddKill()
    {
        kills.Value++;
    }

    public void AddDeath()
    {
        deaths.Value++;
    }
    public void ResetHealth()
    {
        health.Value = 100;
        HideDeathScreen();
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

    private System.Collections.IEnumerator RespawnCountdown(float seconds)
    {
        float t = seconds;
        while (t > 0f)
        {
            if (respawnTimerText != null)
                respawnTimerText.text = $"Respawning in {Mathf.CeilToInt(t)}";
            yield return new WaitForSeconds(1f);
            t -= 1f;
        }
        if (respawnTimerText != null)
            respawnTimerText.text = "Respawning";
        _respawnCoroutine = null;
    }
}
