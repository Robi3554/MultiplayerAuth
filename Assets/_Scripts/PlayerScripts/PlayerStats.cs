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
    public readonly SyncVar<bool> isRespawning = new SyncVar<bool>(false);

    public readonly SyncVar<Team> team = new SyncVar<Team>(Team.None);
    public int damageMult = 1;

    [SerializeField] private Animator animator;
    [SerializeField] private TMP_Text _usernameTextOnBillboard;
    [SerializeField] private AudioSource _hitAudioSource;
    [SerializeField] private AudioClip _hitAudioClip;
    [SerializeField] private AudioSource _deathAudioSource;
    [SerializeField] private AudioClip _deathAudioClip;
    [SerializeField] private GameObject _damageTakenVfx;
    [SerializeField] private Canvas _playerGlued;
    [SerializeField] private GameObject _floatingDamage;
    private int damageTextCount = 0;
    private int accumulatedDamage = 0;
    private Coroutine damageRoutine;

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

    private bool isHeadBig;
    private bool isDamageAmp;

    private static readonly Color RebelsColor = new Color(0.9f, 0.3f, 0.3f);
    private static readonly Color AIColor = new Color(0.3f, 0.5f, 0.9f);

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Subscribe to team changes on all clients so billboard color stays in sync
        team.OnChange += OnTeamChanged;
        ApplyBillboardTeamColor(team.Value);

        // Notify scoreboard that this player spawned
        Debug.Log("PlayerStats OnStartClient: Registering player with ScoreboardManager: " + username.Value);
        if (ScoreboardManager.Instance != null)
        {
            ScoreboardManager.Instance.RegisterPlayer(this);
        }
        else
        {
            ScoreboardManager.AddPlayerToInitialList(this);
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

        team.OnChange -= OnTeamChanged;

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
        AnalyticsManager.EnsureInstance().RegisterPlayer(Owner, this);
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
        _usernameTextOnBillboard.text = username;
        ApplyBillboardTeamColor(team.Value);
    }

    private void OnTeamChanged(Team previous, Team current, bool asServer)
    {
        ApplyBillboardTeamColor(current);
    }

    private void ApplyBillboardTeamColor(Team t)
    {
        if (_usernameTextOnBillboard == null) return;

        _usernameTextOnBillboard.color = GetTeamColor(t);
    }

    void Update()
    {
        if (!IsOwner)
            return;

        if (killText != null)
        {
            if (GameModeManager.Instance != null && GameModeManager.Instance.gameMode.Value == GameMode.TeamDeathmatch)
            {
                int teamTotal = team.Value == Team.Rebels
                    ? GameModeManager.Instance.rebelKills.Value
                    : GameModeManager.Instance.aiKills.Value;
                killText.text = teamTotal.ToString();
            }
            else
            {
                killText.text = kills.Value.ToString();
            }
        }

        if (deathText != null)
        {
            if (GameModeManager.Instance != null && GameModeManager.Instance.gameMode.Value == GameMode.TeamDeathmatch)
            {
                int teamDeaths = team.Value == Team.Rebels
                    ? GameModeManager.Instance.rebelDeaths.Value
                    : GameModeManager.Instance.aiDeaths.Value;
                deathText.text = teamDeaths.ToString();
            }
            else
            {
                deathText.text = deaths.Value.ToString();
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isRespawning.Value) return;

        SetHealth(damage);

        TargetHitSound();
        TargetShakeCamera(Owner, 0.5f, 0.1f);
        TargetDamagedVFX();
        AccumulateDamage(damage);
    }

    [Server]
    public void HealPlayer(int healAmount)
    {
        if ((health.Value + healAmount) >= 100)
        {
            health.Value = 100;
        }
        else
        {
            health.Value += healAmount;
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

        DeathSound();
    }
    public void ResetHealth()
    {
        health.Value = 100;
    }

    private void SetHealth(int value)
    {
        health.Value = Mathf.Clamp(health.Value - value, 0, 100);
    }

    [ObserversRpc]
    private void TargetHitSound()
    {
        if (_canPlayHitSound)
        {
            _hitAudioSource.PlayOneShot(_hitAudioClip);
        }
    }

    [ObserversRpc]
    private void DeathSound()
    {
        if (_deathAudioSource && _deathAudioClip)
        {
            _deathAudioSource.PlayOneShot(_deathAudioClip);
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

    #region Floating Damage Text
    [ObserversRpc]
    private void ShowText(int damage)
    {
        GameObject text = Instantiate(_floatingDamage, _playerGlued.transform);

        RectTransform rt = text.GetComponent<RectTransform>();
        RectTransform usernameRT = _usernameTextOnBillboard.GetComponent<RectTransform>();

        Vector2 basePos = usernameRT.anchoredPosition + new Vector2(0, 30f);

        float stackOffset = damageTextCount * 15f;

        float randomX = UnityEngine.Random.Range(-15f, 15f);
        float randomY = UnityEngine.Random.Range(0f, 10f);

        rt.anchoredPosition = basePos + new Vector2(randomX, randomY + stackOffset);

        TMP_Text tmp = text.GetComponent<TMP_Text>();
        tmp.text = damage.ToString();
        tmp.color = GetTeamColor(team.Value);

        damageTextCount++;

        StartCoroutine(ResetDamageTextCount());
    }

    private Color GetTeamColor(Team t)
    {
        // In FFA, all names are white since there are no teams.
        if (GameModeManager.Instance == null ||
            GameModeManager.Instance.gameMode.Value == GameMode.FreeForAll)
            return Color.white;

        return t switch
        {
            Team.Rebels => RebelsColor,
            Team.AI => AIColor,
            _ => Color.white
        };
    }

    private IEnumerator ResetDamageTextCount()
    {
        yield return new WaitForSeconds(0.5f);
        damageTextCount = Mathf.Max(0, damageTextCount - 1);
    }

    private void AccumulateDamage(int damage)
    {
        accumulatedDamage += damage;
        if (damageRoutine != null)
            StopCoroutine(damageRoutine);

        damageRoutine = StartCoroutine(ShowAccumulatedDamage());
    }

    private IEnumerator ShowAccumulatedDamage()
    {
        yield return new WaitForSeconds(0.1f);

        ShowText(accumulatedDamage);

        accumulatedDamage = 0;
        damageRoutine = null;
    }
    #endregion

    private void InitUI()
    {
        var playerHealth = GameObject.Find("PlayerHUD").transform.Find("Player Health");
        healthText = _healthText;
        healthSlider = _healthSlider;
        healthSlider.maxValue = health.Value;

        StartCoroutine(InitScore());
    }

    private IEnumerator InitScore()
    {
        while (GameModeManager.Instance == null)
            yield return null;

        var gm = GameModeManager.Instance;

        while (gm.gameMode == null)
            yield return null;

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

    public static string GetColoredName(string name, Team team)
    {
        if (GameModeManager.Instance == null ||
            GameModeManager.Instance.gameMode.Value == GameMode.FreeForAll)
        {
            return $"<color=white>{name}</color>";
        }

        return team switch
        {
            Team.Rebels => $"<color=#E53935>{name}</color>",
            Team.AI => $"<color=#1E88E5>{name}</color>",
            _ => $"<color=white>{name}</color>"
        };
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
        if (!isHeadBig)
        {
            var originalScale = head.localScale;

            head.localScale *= multiplier;

            isHeadBig = true;

            yield return new WaitForSeconds(10f);

            head.localScale = originalScale;

            isHeadBig = false;
        }
    }
    #endregion

    //Player damage multiplier change
    #region Damage Mult
    public void ChangeMult(int multiplier)
    {
        ServerChangeMult(multiplier);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerChangeMult(int multiplier)
    {
        StartCoroutine(ChangeMultCo(multiplier));
    }

    private IEnumerator ChangeMultCo(int multiplier)
    {
        if (!isDamageAmp)
        {
            int oldMultiplier = damageMult;
            damageMult = multiplier;

            isDamageAmp = true;

            yield return new WaitForSeconds(10f);

            isDamageAmp = false;

            damageMult = oldMultiplier;
        }
    }
    #endregion
}
