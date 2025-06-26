using System.Collections;
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

    public void DamagePlayer(int playerID, int damage, int attackerID)
    {
        if (!base.IsServerInitialized)
            return;

        players[playerID].health -= damage;
        print("Player " + playerID.ToString() + " health is " + players[playerID].health);

        if (players[playerID].health <= 0)
        {
            PlayerKilled(playerID, attackerID);
        }
    }

    void PlayerKilled(int playerID, int attackerID)
    {
        print("Player " + playerID.ToString() + " was killed by " + attackerID.ToString());

        players[playerID].deaths++;
        players[playerID].health = 100;
        players[attackerID].kills++;

        RespawnPlayer(players[playerID].connection, players[playerID].playerObject, Random.Range(0, spawnPoints.Count));
    }

    [TargetRpc]
    void RespawnPlayer(NetworkConnection conn, GameObject player, int spawn)
    {
        player.transform.position = spawnPoints[spawn].position;
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