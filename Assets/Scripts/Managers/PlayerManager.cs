using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    private TMP_Text healthText;
    private TMP_Text killText;
    private TMP_Text deathText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InstantiateTexts();
    }

    public Dictionary<int, Player> players = new Dictionary<int, Player>();

    [SerializeField] List<Transform> spawnPoints = new List<Transform>();

    public void DamagePlayer(int victimClientId, int damage, int attackerClientId)
    {
        if (!IsServerInitialized)
            return;

        if (!players.ContainsKey(victimClientId))
        {
            return;
        }

        var victim = players[victimClientId];
        var victimStats = victim.stats;
        if (victimStats == null) return;

        victimStats.TakeDamage(damage);

        if (victimStats.health.Value <= 0)
        {
            PlayerKilled(victimClientId, attackerClientId);
        }
    }

    void PlayerKilled(int victimClientId, int attackerClientId)
    {
        var victim = players[victimClientId];
        var victimStats = victim.stats;

        victimStats.AddDeath();

        victimStats.isRespawning = true;
        victimStats.ResetHealth();

        if (players.ContainsKey(attackerClientId))
        {
            players[attackerClientId].stats?.AddKill();
        }

        int spawnIndex = Random.Range(0, spawnPoints.Count);
        RespawnPlayer(victim.connection, victim.playerObject, spawnIndex);

        StartCoroutine(ClearRespawningFlag(victimStats));
    }

    [TargetRpc]
    void RespawnPlayer(NetworkConnection conn, GameObject player, int spawn)
    {
        player.transform.position = spawnPoints[spawn].position;
    }

    private void InstantiateTexts()
    {
        healthText = GameObject.Find("PlayerHUD").transform.Find("Health Text").GetComponent<TMP_Text>();
        killText = GameObject.Find("PlayerHUD").transform.Find("Kill Text").GetComponent<TMP_Text>();
        deathText = GameObject.Find("PlayerHUD").transform.Find("Death Text").GetComponent<TMP_Text>();
    }

    IEnumerator ClearRespawningFlag(PlayerStats stats)
    {
        yield return new WaitForSeconds(1f);
        stats.isRespawning = false;
    }

    public class Player
    {
        public GameObject playerObject;
        public NetworkConnection connection;
        public PlayerStats stats;
    }
}
