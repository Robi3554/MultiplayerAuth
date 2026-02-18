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
        Debug.Log("DAVEEE: DamagePlayer entered!");
        if (!IsServerInitialized)
            return;
        
        Debug.Log("DAVEEE: continai");

        if (!players.ContainsKey(victimClientId))
        {
            return;
        }
        Debug.Log("DAVEEE: Am ajuns aici?");

        var victim = players[victimClientId];
        Debug.Log("DAVEEE: vicky {}");
        var victimStats = victim.stats;
        if (victimStats == null) return;
        
        Debug.Log("DAVEEE: de aici incolo dam damage!!!!");

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

        // Prevent multiple respawns for the same player
        if (victimStats.isRespawning)
            return;

        victimStats.AddDeath();
        victimStats.isRespawning = true;
        var playerMovement = victim.playerObject.GetComponent<PredictionMoving>();
        playerMovement.canMove = false;

        // ADD KILL TO ATTACKER AND CHECK WIN CONDITION
        if (players.ContainsKey(attackerClientId))
        {
            var attackerStats = players[attackerClientId].stats;
            if (attackerStats != null)
            {
                attackerStats.AddKill(); // This triggers GameModeManager.OnPlayerKill()
            }
        }

        //show death screen on client and wait before respawning
        DeathScreenManager.Instance.ShowDeathScreen(victim.connection);
        StartCoroutine(RespawnAfterDelay(victim, victimStats));
    }

    private IEnumerator RespawnAfterDelay(Player victim, PlayerStats victimStats)
    {
        //wait for death screen countdown (5 seconds) + UI cleanup time (0.5 seconds)
        yield return new WaitForSeconds(5.5f);
        victimStats.ResetHealth(); // reset
        int spawnIndex = Random.Range(0, spawnPoints.Count);
        RespawnPlayer(victim.connection, victim.playerObject, spawnIndex);
        ReloadPlayerGuns(victim.connection, victim.playerObject);

        victimStats.isRespawning = false;
    }

    [TargetRpc]
    void RespawnPlayer(NetworkConnection conn, GameObject player, int spawn)
    {
        var playerMovement = player.GetComponent<PredictionMoving>();
        playerMovement.canMove = false;

        //disable rigidbody during teleport to prevent physics glitches
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        player.transform.position = spawnPoints[spawn].position;
        player.transform.rotation = spawnPoints[spawn].rotation;
        
    }

    [TargetRpc]
    void ReloadPlayerGuns(NetworkConnection conn, GameObject player)
    {
        var weapons = player.GetComponentsInChildren<Weapon>(true);
        Debug.Log("Weapons : " + weapons.Length);
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

    // NEW METHOD: Called by GameModeManager to reset all players
    [Server]
    public void ResetAllPlayers()
    {
        Debug.Log("[PlayerManager] Resetting all players...");

        foreach (var kvp in players)
        {
            var player = kvp.Value;
            if (player.stats != null)
            {
                // Reset stats
                player.stats.kills.Value = 0;
                player.stats.deaths.Value = 0;
                player.stats.health.Value = 100;
                player.stats.isRespawning = false;

                // Respawn at random location
                int spawnIndex = Random.Range(0, spawnPoints.Count);
                RespawnPlayer(player.connection, player.playerObject, spawnIndex);
                ReloadPlayerGuns(player.connection, player.playerObject);
            }
        }
    }

    // NEW METHOD: Get random spawn point (for GameModeManager)
    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("[PlayerManager] No spawn points configured!");
            return null;
        }

        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }

    public class Player
    {
        public GameObject playerObject;
        public NetworkConnection connection;
        public PlayerStats stats;
        public string username;
    }
}