using System;
using FishNet.Object;
using UnityEngine;

public class RobotSpawnManager : NetworkBehaviour
{
    public static RobotSpawnManager Instance;

    [SerializeField]
    private Transform[] spawnPoints;
    [SerializeField]
    private GameObject robot;
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
        Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

        GameObject instance = Instantiate(robot, spawnPoint.position, spawnPoint.rotation);
        _currentRobot = instance;
        LittleRobot robotScript = instance.GetComponent<LittleRobot>();
        robotScript.patrolPoints = new Transform[spawnPoints.Length];

        Array.Copy(spawnPoints, robotScript.patrolPoints, spawnPoints.Length);

        robotScript.OnRobotKilled += HandleRobotKilled;

        ServerManager.Spawn(instance);
    }

    public void DespawnRobot(NetworkObject robot)
    {
        if (!IsServerInitialized)
            return;

        if (robot != null && robot.IsSpawned)
        {
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
}
