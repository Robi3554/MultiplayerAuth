using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public Dictionary<int, Player> players = new Dictionary<int, Player>();

    [SerializeField] List<Transform> spawnPoints = new List<Transform>();

    public void HealPlayer(int playerId, int healAmount)
    {
        if (!IsServerInitialized)
        {
            return;
        }
        if (!players.ContainsKey(playerId))
        {
            return;
        }
        var player = players[playerId];
        var playerStats = player.stats;
        if (playerStats == null) return;

        playerStats.HealPlayer(healAmount);

    }

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
        ReloadPlayerGuns(victim.connection, victim.playerObject);
        RespawnPlayer(victim.connection, victim.playerObject, spawnIndex);

        StartCoroutine(ClearRespawningFlag(victimStats));
    }

    [TargetRpc]
    void RespawnPlayer(NetworkConnection conn, GameObject player, int spawn)
    {
        player.transform.position = spawnPoints[spawn].position;
    }

    [TargetRpc]
    void ReloadPlayerGuns(NetworkConnection conn, GameObject player)
    {
        var weapons = player.GetComponentsInChildren<Weapon>(true);
        Debug.Log("Weapons : " +  weapons.Length);
        foreach (var weapon in weapons)
        {
            weapon.OnDeathReload();
        }
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
        public string username;
    }
}
