using System.Collections;
using FishNet.Object;
using UnityEngine;
using UnityEngine.AI;

public class LittleRobot : NetworkBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 12f;
    public LayerMask playerMask;

    [Header("Flee Settings")]
    public float safeDistance = 20f;
    public float fleeDistanceStep = 10f;
    public int maxFleeAttempts = 8;
    public float repathDelay = 0.5f;

    [Header("Agent Speed")]
    public float minSpeed = 3.5f;
    public float maxSpeed = 6f;

    private NavMeshAgent agent;
    private Transform targetPlayer;
    private float nextPathUpdateTime = 0f;

    private enum State { Idle, Fleeing }
    private State currentState = State.Idle;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (!IsServerInitialized)
            return;

        float closestDist = DetectClosestPlayer();

        switch (currentState)
        {
            case State.Idle:
                if (closestDist < detectionRadius)
                    currentState = State.Fleeing;
                break;

            case State.Fleeing:
                if (Time.time >= nextPathUpdateTime)
                {
                    FleeFromPlayer();
                    nextPathUpdateTime = Time.time + repathDelay;
                }

                if (targetPlayer == null || closestDist > safeDistance)
                {
                    currentState = State.Idle;
                    agent.ResetPath();
                }
                break;
        }

        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion lookRot = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 5f);
        }
    }

    private float DetectClosestPlayer()
    {
        Collider[] players = Physics.OverlapSphere(transform.position, detectionRadius, playerMask);
        float closestDist = Mathf.Infinity;
        targetPlayer = null;

        foreach (Collider c in players)
        {
            float dist = Vector3.Distance(transform.position, c.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                targetPlayer = c.transform;
            }
        }

        return closestDist;
    }

    private void FleeFromPlayer()
    {
        if (targetPlayer == null) return;

        float dist = Vector3.Distance(transform.position, targetPlayer.position);
        agent.speed = Mathf.Lerp(maxSpeed, minSpeed, dist / detectionRadius);

        for (int i = 0; i < maxFleeAttempts; i++)
        {
            Vector3 fleeDir = (transform.position - targetPlayer.position).normalized;

            float angleOffset = Random.Range(-45f, 45f);
            fleeDir = Quaternion.Euler(0, angleOffset, 0) * fleeDir;
            fleeDir.Normalize();

            Vector3 candidatePos = transform.position + fleeDir * fleeDistanceStep;

            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }

        agent.ResetPath();
    }

    public void DestroyRobot(Collider player)
    {
        StartCoroutine(ChangeSpeed(player));
        Destroy(gameObject);
    }

    private IEnumerator ChangeSpeed(Collider player)
    {
        player.GetComponent<PredictionMoving>().moveRate += 5;

        yield return new WaitForSeconds(5f);

        player.GetComponent<PredictionMoving>().moveRate -= 5;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}