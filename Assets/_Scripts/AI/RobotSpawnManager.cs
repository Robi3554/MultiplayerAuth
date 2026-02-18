using System;
using FishNet.Object;
using UnityEngine;

public class RobotSpawnManager : NetworkBehaviour
{
    public static RobotSpawnManager Instance;

    [SerializeField]
    private Transform[] spawnPoints = new Transform[0];
    [SerializeField]
    private GameObject[] robots;
    [SerializeField]
    private int timeBetweenSpawns;
    private float _timer;

    private GameObject _currentRobot;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _timer = timeBetweenSpawns;
    }

    private void Update()
    {
        if (!IsServerInitialized)
            return;

        if (_currentRobot != null)
            return;

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            SpawnRobot();
            _timer = timeBetweenSpawns;
        }
    }

    private void SpawnRobot()
    {
        GameObject robot = robots[0]; // spawn a random robot

        Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

        GameObject instance = Instantiate(robot, spawnPoint.position, spawnPoint.rotation);
        _currentRobot = instance;
        if (robot.GetComponent<LittleRobot>() != null)
        {
            LittleRobot robotScript = instance.GetComponent<LittleRobot>();
            robotScript.patrolPoints = new Transform[spawnPoints.Length];

            Array.Copy(spawnPoints, robotScript.patrolPoints, spawnPoints.Length);

            robotScript.OnRobotKilled += HandleRobotKilled;
        }
        if (robot.GetComponent<KamikazeRobot>() != null)
        {
            KamikazeRobot robotScript = instance.GetComponent<KamikazeRobot>();
            robotScript.patrolPoints = new Transform[spawnPoints.Length];

            Array.Copy(spawnPoints, robotScript.patrolPoints, spawnPoints.Length);

            robotScript.OnRobotKilled += HandleRobotKilled;
        }
        ServerManager.Spawn(instance);
    }

    public void DespawnRobot(NetworkObject robot)
    {
        Debug.Log($"Am I Server? {IsServerInitialized}. Is Object Spawned? {robot.IsSpawned}");
        
        if (!IsServerInitialized)
            return;
        
        Debug.Log($"SERVER: Prima conditie {robot != null} - a doua : {robot.IsSpawned}");

        if (robot != null && robot.IsSpawned)
        {
            Debug.Log("Am intrat si aici");
            ServerManager.Despawn(robot);
            Debug.Log($"SERVER: Robot {robot.name} despawned");
            _currentRobot = null;
        }
    }

    private void HandleRobotKilled(LittleRobot robot)
    {
        robot.OnRobotKilled -= HandleRobotKilled;
        _currentRobot = null;
    }
    private void HandleRobotKilled(KamikazeRobot robot)
    {
        robot.OnRobotKilled -= HandleRobotKilled;
        _currentRobot = null;
    }
}
