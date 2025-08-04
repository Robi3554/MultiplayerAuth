using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    public readonly SyncVar<int> health = new SyncVar<int>(100);
    public readonly SyncVar<int> kills = new SyncVar<int>(0);
    public readonly SyncVar<int> deaths = new SyncVar<int>(0);

    private TMP_Text healthText;
    private TMP_Text killText;
    private TMP_Text deathText;

    public bool isRespawning = false;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            InitTexts();
            if (healthText != null)
                healthText.text = health.Value.ToString();
            health.OnChange += OnHealthChanged;
        }
    }

    void Update()
    {
        if (!IsOwner)
            return;

        if (killText != null)
            killText.text = "K:" + kills.Value.ToString();

        if (deathText != null)
            deathText.text = "D:" + deaths.Value.ToString();
    }

    [Server]
    public void TakeDamage(int damage)
    {
        if (isRespawning) return;
        health.Value = Mathf.Clamp(health.Value - damage, 0, 100);
    }

    [Server]
    public void ResetHealth()
    {
        health.Value = 100;
    }

    [Server]
    public void AddKill()
    {
        kills.Value++;
    }

    [Server]
    public void AddDeath()
    {
        deaths.Value++;
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
    private void InitTexts()
    {
        healthText = GameObject.Find("PlayerHUD").transform.Find("Health Text").GetComponent<TMP_Text>();
        killText = GameObject.Find("PlayerHUD").transform.Find("Kill Text").GetComponent<TMP_Text>();
        deathText = GameObject.Find("PlayerHUD").transform.Find("Death Text").GetComponent<TMP_Text>();
    }

    private void OnHealthChanged(int previous, int current, bool asServer)
    {
        if (healthText != null)
            healthText.text = current.ToString();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (IsOwner)
            health.OnChange -= OnHealthChanged;
    }
}
