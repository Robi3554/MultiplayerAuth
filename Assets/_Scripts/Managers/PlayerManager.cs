using System.Collections;
using System.Collections.Generic;
using FishNet.Component.Animating;
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

    private int 
        deadLayer,
        aliveLayer;

    private void Start()
    {
        deadLayer = LayerMask.NameToLayer("Dead");
        aliveLayer = LayerMask.NameToLayer("Player");
    }

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

        // Friendly fire check: in TDM, skip damage if attacker and victim are on the same team.
        // attackerClientId == -1 means environmental damage (turret), always applies.
        if (LobbyData.ResolvedGameMode == GameMode.TeamDeathmatch && attackerClientId >= 0
            && players.ContainsKey(attackerClientId))
        {
            Team victimTeam = players[victimClientId].stats.team.Value;
            Team attackerTeam = players[attackerClientId].stats.team.Value;
            if (victimTeam == attackerTeam && victimTeam != Team.None)
            {
                return;
            }
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

        // Prevent multiple respawns for the same player
        if (victimStats.isRespawning.Value)
            return;

        SetPlayerAnimation(victim.playerObject, "Death");

        victim.playerObject.layer = deadLayer;
        SetPlayersLayer(victim.playerObject, deadLayer);

        victimStats.AddDeath();
        victimStats.isRespawning.Value = true;

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
        //wait for death screen countdown (5 seconds) + UI cleanup time (.75 seconds)
        yield return new WaitForSeconds(5.75f);
        victimStats.ResetHealth(); // reset
        int spawnIndex = Random.Range(0, spawnPoints.Count);
        RespawnPlayer(victim.connection, victim.playerObject, spawnIndex);
        ReloadPlayerGuns(victim.connection, victim.playerObject);

        victim.playerObject.layer = aliveLayer;
        SetPlayersLayer(victim.playerObject, aliveLayer);

        SetPlayerAnimation(victim.playerObject, "Idle");

        victimStats.isRespawning.Value = false;
    }

    [TargetRpc]
    void RespawnPlayer(NetworkConnection conn, GameObject player, int spawn)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb)
            rb.linearVelocity = Vector3.zero;

        player.transform.position = spawnPoints[spawn].position;
        player.transform.rotation = spawnPoints[spawn].rotation;
    }

    [TargetRpc]
    void ReloadPlayerGuns(NetworkConnection conn, GameObject player)
    {
        var weapons = player.GetComponentsInChildren<Weapon>(true);
        foreach (var weapon in weapons)
        {
            weapon.OnDeathReload();
        }
    }

    [ObserversRpc]
    void SetPlayersLayer(GameObject player, int layer) => player.layer = layer;

    [ObserversRpc]
    void SetPlayerAnimation(GameObject player, string trigger)
    {
        player.GetComponentInChildren<NetworkAnimator>().SetTrigger(trigger);
    }

    // NEW METHOD: Called by GameModeManager to reset all players
    [Server]
    public void ResetAllPlayers()
    {

        foreach (var kvp in players)
        {
            var player = kvp.Value;
            if (player.stats != null)
            {
                // Reset stats
                player.stats.kills.Value = 0;
                player.stats.deaths.Value = 0;
                player.stats.health.Value = 100;
                player.stats.isRespawning.Value = false;

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