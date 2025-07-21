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

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            InitTexts();
        }
    }

    void Update()
    {
        if (!IsOwner)
            return;

        if(healthText != null)
        {
            healthText.text = health.Value.ToString();
        }

        if(killText != null)
        {
            killText.text = "K:" + kills.Value.ToString();
        }

        if(deathText != null)
        {
            deathText.text = "D:" + deaths.Value.ToString();
        }
    }

    [Server]
    public void TakeDamage(int damage)
    {
        health.Value = Mathf.Clamp(health.Value -  damage, 0, 100);
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

    private void InitTexts()
    {
        healthText = GameObject.Find("PlayerHUD").transform.Find("Health Text").GetComponent<TMP_Text>();
        killText = GameObject.Find("PlayerHUD").transform.Find("Kill Text").GetComponent<TMP_Text>();
        deathText = GameObject.Find("PlayerHUD").transform.Find("Death Text").GetComponent<TMP_Text>();
    }
}
