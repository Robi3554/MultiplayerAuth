using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Component.Animating;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    public Dictionary<int, Player> players = new Dictionary<int, Player>();

    [SerializeField]
    private Transform ffaSpawnParent;
    private readonly List<Transform> _ffaSpawnPoints = new();
    
    [SerializeField]
    private Transform tdmRebelsParent;
    private readonly List<Transform> _tdmRebelSpawnPoints = new();
    
    [SerializeField]
    private Transform tdmAiParent;
    private readonly List<Transform> _tdmAiSpawnPoints = new();

    private int
        deadLayer,
        aliveLayer;

    
    private void Awake()
    {
        Instance = this;
        
        foreach (Transform child in ffaSpawnParent)
        {
            _ffaSpawnPoints.Add(child);
        }
        
        foreach (Transform child in tdmRebelsParent)
        {
            _tdmRebelSpawnPoints.Add(child);
        }
        
        foreach (Transform child in tdmAiParent)
        {
            _tdmAiSpawnPoints.Add(child);
        }
    }
    
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

        string victimName = victim.stats.username.Value;
        Team victimTeam = victim.stats.team.Value;

        string killerName = "Robot";
        Team killerTeam = Team.None;

        int weaponId = -1;

        if (attackerClientId >= 0 && players.ContainsKey(attackerClientId))
        {
            var attacker = players[attackerClientId];

            killerName = attacker.stats.username.Value;
            killerTeam = attacker.stats.team.Value;

            var weaponChanger = attacker.playerObject.GetComponent<ChangeWeapons>();

            if (weaponChanger != null)
            {
                weaponId = weaponChanger.GetCurrentWeaponId();
            }
        }

        RpcSendKillFeed(killerName, victimName, killerTeam, victimTeam, weaponId);

        SetPlayerAnimation(victim.playerObject, "Death");

        victim.playerObject.layer = deadLayer;
        SetPlayersLayer(victim.playerObject, deadLayer);

        victimStats.isRespawning.Value = true;

        // ADD KILL TO ATTACKER AND CHECK WIN CONDITION
        // Deaths only count when killed by another player, not by environment (turrets/robots).
        if (attackerClientId >= 0 && players.ContainsKey(attackerClientId))
        {
            victimStats.AddDeath();
            var attackerStats = players[attackerClientId].stats;
            if (attackerStats != null)
            {
                attackerStats.AddKill(); // This triggers GameModeManager.OnPlayerKill()
            }
        }

        //show death screen on client and wait before respawning
        if(GameModeManager.Instance.isGameActive.Value)
            DeathScreenManager.Instance.ShowDeathScreen(victim.connection);
        StartCoroutine(RespawnAfterDelay(victim, victimStats));
    }

    private IEnumerator RespawnAfterDelay(Player victim, PlayerStats victimStats)
    {
        //wait for death screen countdown (5 seconds) + UI cleanup time (.75 seconds)
        yield return new WaitForSeconds(5.75f);
        victimStats.ResetHealth(); // reset
        RespawnPlayer(victim.connection, victim.playerObject, victim.stats);
        ReloadPlayerGuns(victim.connection, victim.playerObject);

        victim.playerObject.layer = aliveLayer;
        SetPlayersLayer(victim.playerObject, aliveLayer);

        SetPlayerAnimation(victim.playerObject, "Idle");

        victimStats.isRespawning.Value = false;
    }

    [TargetRpc]
    void RespawnPlayer(NetworkConnection conn, GameObject player, PlayerStats stats)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb)
            rb.linearVelocity = Vector3.zero;

        List<Transform> availableSpawnPoints;
        switch (LobbyData.ResolvedGameMode)
        {
            case GameMode.FreeForAll:
                availableSpawnPoints = _ffaSpawnPoints;
                break;
            case GameMode.TeamDeathmatch:
                var team = stats.team.Value;
                if (team == Team.Rebels)
                    availableSpawnPoints = _tdmRebelSpawnPoints;
                else if (team == Team.AI)
                    availableSpawnPoints = _tdmAiSpawnPoints;
                else
                    availableSpawnPoints = _ffaSpawnPoints; // Fallback
                break;
            default:
                availableSpawnPoints = _ffaSpawnPoints;
                break;
        }

        Transform spawnPoint = null;

        while (availableSpawnPoints.Count > 0)
        {
            var randomIndex = Random.Range(0, availableSpawnPoints.Count);
            spawnPoint = availableSpawnPoints[randomIndex];
            try
            {
                var occupied = players.Values.ToList()
                    .Where(p => !p.stats.isRespawning.Value && p.playerObject != player)
                    .Any(p =>
                    {
                        var distance = Vector3.Distance(p.playerObject.transform.position, spawnPoint.position);
                        return distance < 2f;
                    });

                if (!occupied)
                {
                    break; // Found an unoccupied spawn point
                }

                availableSpawnPoints.RemoveAt(randomIndex); // Remove occupied spawn point and try again
            }
            catch (Exception e)
            {
                break; // If there's an error checking occupancy, just use this spawn point
            }
        }

        if (!spawnPoint) return;
        
        player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
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

    [ObserversRpc]
    private void RpcSendKillFeed(string killerName, string victimName, Team killerTeam, Team victimTeam, int weaponId)
    {
        KillFeedUI ui = FindFirstObjectByType<KillFeedUI>();
        if (ui == null) return;

        string killerColored = PlayerStats.GetColoredName(killerName, killerTeam);
        string victimColored = PlayerStats.GetColoredName(victimName, victimTeam);

        ui.AddFeedItem(killerColored, victimColored, weaponId);
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
                RespawnPlayer(player.connection, player.playerObject, player.stats);
                ReloadPlayerGuns(player.connection, player.playerObject);
            }
        }
    }

    public class Player
    {
        public GameObject playerObject;
        public NetworkConnection connection;
        public PlayerStats stats;
        public string username;
    }
}