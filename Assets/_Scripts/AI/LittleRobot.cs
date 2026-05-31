using System;
using _Scripts.Managers;
using FishNet.Object;
using UnityEngine;
using UnityEngine.AI;

public class LittleRobot : NetworkBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 12f;
    public LayerMask playerMask;

    [Header("Flee Settings")]
    public float safeDistance = 15f;
    public float fleeDistanceStep = 10f;
    public int maxFleeAttempts = 8;
    public float repathDelay = 0.5f;

    [Header("Agent Speed")]
    public float minSpeed = 3.5f;
    public float maxSpeed = 6f;

    [Header("Patroling")]
    public Transform[] patrolPoints;
    private Vector3 currentPoint;
    private int currIndex;
    private int prevIndex;
    private bool dirClockwise = true;
    
    [SerializeField] private AudioSource deathAudioSource;

    [Header("PowerupEffects")]
    public PowerupEffect powerup;

    private NavMeshAgent agent;
    private Transform targetPlayer;
    private float nextPathUpdateTime = 0f;

    private enum State { Patroling, Fleeing }
    private State currentState = State.Patroling;

    public event Action<LittleRobot> OnRobotKilled;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(patrolPoints[0].transform.position);
        currentPoint = patrolPoints[0].transform.position;
        currIndex = 0;
    }

    void Update()
    {
        if (!IsServerInitialized)
            return;

        switch (currentState)
        {
            case State.Patroling:
                {
                    float closestDist = DetectClosestPlayer();

                    if (targetPlayer != null && closestDist < detectionRadius)
                    {
                        currentState = State.Fleeing;
                        nextPathUpdateTime = 0f;
                    }
                    else
                    {
                        Patrol();
                    }
                    break;
                }

            case State.Fleeing:
                {
                    if (targetPlayer == null)
                    {
                        ExitFlee();
                        break;
                    }

                    float dist = Vector3.Distance(transform.position, targetPlayer.position);

                    if (dist > safeDistance)
                    {
                        ExitFlee();
                        break;
                    }

                    if (Time.time >= nextPathUpdateTime)
                    {
                        FleeFromPlayer();
                        nextPathUpdateTime = Time.time + repathDelay;
                    }
                    break;
                }
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

    private void Patrol()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                prevIndex = currIndex;
                if (dirClockwise)
                {
                    currIndex++;
                    if (currIndex >= patrolPoints.Length)
                        currIndex = 0;
                    currentPoint = patrolPoints[currIndex].transform.position;
                    agent.SetDestination(currentPoint);
                }
                else
                {
                    currIndex--;
                    if (currIndex <= 0)
                        currIndex = patrolPoints.Length - 1;
                    currentPoint = patrolPoints[currIndex].transform.position;
                    agent.SetDestination(currentPoint);
                }
            }
        }
    }

    private void SwitchDirection()
    {
        dirClockwise = !dirClockwise;

        int aux = currIndex;
        currIndex = prevIndex;
        prevIndex = aux;
    }

    private void FleeFromPlayer()
    {
        if (targetPlayer == null) return;

        float dist = Vector3.Distance(transform.position, targetPlayer.position);
        agent.speed = Mathf.Lerp(maxSpeed, minSpeed, dist / detectionRadius);

        for (int i = 0; i < maxFleeAttempts; i++)
        {
            Vector3 fleeDir = (transform.position - targetPlayer.position).normalized;

            float angleOffset = UnityEngine.Random.Range(-45f, 45f);
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

    private void ExitFlee()
    {
        if (!agent.pathPending &&
        agent.remainingDistance <= agent.stoppingDistance &&
        (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f))
        {
            prevIndex = currIndex;

            if (dirClockwise)
            {
                currIndex = (currIndex + 1) % patrolPoints.Length;
            }
            else
            {
                currIndex--;
                if (currIndex < 0)
                    currIndex = patrolPoints.Length - 1;
            }

            agent.SetDestination(patrolPoints[currIndex].position);
        }
    }

    public void DestroyRobot(NetworkObject player)
    {
        Debug.Log($"[Robot Log] LittleRobot {gameObject.name} was destroyed. Killer: {(player != null ? player.name : "Unknown")}");
        ParticlesManager.Instance.PlayEffect(transform.position, EffectType.SmallExplosion);
        RpcPlayDeathSound();
        
        OnRobotKilled?.Invoke(this);

        powerup.TriggerEffect(player);
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("PatrolPoint"))
        {
            currentPoint = agent.destination;
        }

        // Logic for taking damage/being killed via collision (e.g., projectiles or player melee)
        if (col.CompareTag("Projectile") || col.CompareTag("PlayerAttack"))
        {
            Debug.Log($"[Robot Log] LittleRobot {gameObject.name} took damage from {col.gameObject.name} via Trigger.");
        }
    }
    
    [ObserversRpc]
    private void RpcPlayDeathSound()
    {
        var manager = PersistentAudioSourceManager.GetInstance();
        if (manager != null && deathAudioSource != null)
            manager.PlaySoundBasedOnRefencedSource(deathAudioSource);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}