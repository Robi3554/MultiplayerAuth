using System;
using FishNet.Object;
using UnityEngine;

public class RobotSpawnManager : NetworkBehaviour
{
    [SerializeField]
    private Transform[] spawnPoints;
    [SerializeField]
    private GameObject robot;
    [SerializeField]
    private int timeBetweenSpawns;
    private float _timer;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _timer = timeBetweenSpawns;
    }

    private void Update()
    {
        if (!IsServerInitialized)
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
        LittleRobot robotScript = instance.GetComponent<LittleRobot>();
        robotScript.patrolPoints = new Transform[spawnPoints.Length];

        Array.Copy(spawnPoints, robotScript.patrolPoints, spawnPoints.Length);

        ServerManager.Spawn(instance);
    }
}
