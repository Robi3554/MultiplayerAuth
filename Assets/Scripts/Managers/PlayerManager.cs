using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public Dictionary<int, Player> players = new Dictionary<int, Player>();

    [SerializeField] List<Transform> spawnPoints = new List<Transform>();

    public void DamagePlayer(int victimClientId, int damage, int attackerClientId)
    {
        if (!base.IsServerInitialized)
            return;

        if (!players.ContainsKey(victimClientId))
        {
            Debug.LogError($"[DamagePlayer] Victim ClientId {victimClientId} not found in players dictionary. Keys: {string.Join(", ", players.Keys)}");
            return;
        }

        players[victimClientId].health -= damage;
        Debug.Log($"Player {victimClientId} took {damage} damage. Health now: {players[victimClientId].health}");

        if (players[victimClientId].health <= 0)
        {
            PlayerKilled(victimClientId, attackerClientId);
        }
    }

    void PlayerKilled(int victimClientId, int attackerClientId)
    {
        Debug.Log($"Player {victimClientId} was killed by {attackerClientId}");

        if (!players.ContainsKey(victimClientId))
        {
            Debug.LogError($"[PlayerKilled] Victim ClientId {victimClientId} not found.");
            return;
        }

        players[victimClientId].deaths++;
        players[victimClientId].health = 100;

        if (players.ContainsKey(attackerClientId))
        {
            players[attackerClientId].kills++;
        }
        else
        {
            Debug.LogWarning($"[PlayerKilled] Attacker ClientId {attackerClientId} not found. Kill not credited.");
        }

        Debug.Log($"Player {victimClientId} deaths: {players[victimClientId].deaths} | Player {attackerClientId} kills: {(players.ContainsKey(attackerClientId) ? players[attackerClientId].kills : 0)}");

        int spawnIndex = Random.Range(0, spawnPoints.Count);
        RespawnPlayer(players[victimClientId].connection, players[victimClientId].playerObject, spawnIndex);
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

    public class Player
    {
        public int health = 100;
        public GameObject playerObject;
        public NetworkConnection connection;
        public int kills = 0;
        public int deaths = 0;
    }
}