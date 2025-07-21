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

    private void Update()
    {

    }

    public Dictionary<int, Player> players = new Dictionary<int, Player>();

    [SerializeField] List<Transform> spawnPoints = new List<Transform>();

    public void DamagePlayer(int victimClientId, int damage, int attackerClientId)
    {
        if (!IsServerInitialized)
            return;

        if (!players.ContainsKey(victimClientId))
        {
            Debug.LogError($"[DamagePlayer] Victim {victimClientId} not found.");
            return;
        }

        var victim = players[victimClientId];
        var victimStats = victim.stats;
        if (victimStats == null) return;

        victimStats.TakeDamage(damage);
        Debug.Log($"[DamagePlayer] Player {victimClientId} took {damage} damage. Health: {victimStats.health}");

        if (victimStats.health.Value <= 0)
        {
            PlayerKilled(victimClientId, attackerClientId);
        }
    }

    void PlayerKilled(int victimClientId, int attackerClientId)
    {
        Debug.Log($"[PlayerKilled] Player {victimClientId} was killed by {attackerClientId}");

        if (!players.ContainsKey(victimClientId)) return;
        var victim = players[victimClientId];
        var victimStats = victim.stats;
        if (victimStats != null)
        {
            victimStats.AddDeath();
            victimStats.ResetHealth();
        }

        if (players.ContainsKey(attackerClientId))
        {
            var attackerStats = players[attackerClientId].stats;
            attackerStats?.AddKill();
        }

        int spawnIndex = Random.Range(0, spawnPoints.Count);
        RespawnPlayer(victim.connection, victim.playerObject, spawnIndex);
    }

    [TargetRpc]
    void RespawnPlayer(NetworkConnection conn, GameObject player, int spawn)
    {
        if (spawn >= 0 && spawn < spawnPoints.Count)
        {
            player.transform.position = spawnPoints[spawn].position;
        }
        else
        {
            Debug.LogWarning("[RespawnPlayer] Invalid spawn index.");
        }
    }

    private void InstantiateTexts()
    {
        healthText = GameObject.Find("PlayerHUD").transform.Find("Health Text").GetComponent<TMP_Text>();
        killText = GameObject.Find("PlayerHUD").transform.Find("Kill Text").GetComponent<TMP_Text>();
        deathText = GameObject.Find("PlayerHUD").transform.Find("Death Text").GetComponent<TMP_Text>();
    }

    public class Player
    {
        public GameObject playerObject;
        public NetworkConnection connection;
        public PlayerStats stats;
    }
}