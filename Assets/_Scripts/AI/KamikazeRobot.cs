using System;
using _Scripts.Managers;
using FishNet.Object;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class KamikazeRobot : NetworkBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 12f;
    public LayerMask playerMask;

    [Header("Chase Settings")]
    public float safeDistance = 15f;
    public float chaseDistanceStep = 10f;
    public int maxChaseAttempts = 8;
    public float repathDelay = 0.1f;

    [Header("Agent Stats")]
    public float minSpeed = 3.5f;
    public float maxSpeed = 5f;
    public int Damage = 50;

    [Header("Patroling")]
    public Transform[] patrolPoints;

    [SerializeField] private AudioSource kamikazeAudioSource;
    [SerializeField] private AudioSource deathAudioSource;

    private Vector3 currentPoint;
    private int currIndex;
    private int prevIndex;
    private bool dirClockwise = true;

    private NavMeshAgent agent;
    private Transform targetPlayer;
    private float nextPathUpdateTime = 0.1f;

    private enum State { Patroling, Chasing }
    private State currentState = State.Patroling;

    public event Action<KamikazeRobot> OnRobotKilled;

    public void Start()
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
                        currentState = State.Chasing;
                        nextPathUpdateTime = 0f;
                    }
                    else
                    {
                        Patrol();
                    }
                    break;
                }
            case State.Chasing:
                {
                    if (targetPlayer == null)
                    {
                        ExitChase();
                        break;
                    }
                    float dist = Vector3.Distance(transform.position, targetPlayer.position);
                    if (dist > detectionRadius)
                    {
                        ExitChase();
                        break;

                    }
                    float closestDist = DetectClosestPlayer();
                    if(closestDist < dist) // daca cel mai apropiat inamic e mai aproape decat cel deja detectat, schimba
                    {
                        dist = closestDist;
                        nextPathUpdateTime = 0;
                    }
                    if (Time.time >= nextPathUpdateTime)
                    {
                        ChasePlayer();
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

    private void ChasePlayer()
    {
        if (targetPlayer == null) return;

        float dist = Vector3.Distance(transform.position, targetPlayer.position);
        agent.speed = Mathf.Lerp(minSpeed, maxSpeed, dist / detectionRadius);

        for (int i = 0; i < maxChaseAttempts; i++)
        {
            Vector3 chaseDir = (targetPlayer.position - transform.position).normalized;

            // Vector3 candidatePos = transform.position + chaseDir * chaseDistanceStep;
            if (NavMesh.SamplePosition(chaseDir, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            } else {
                //if navmesh sampling fails, try to go directly to player
                if (NavMesh.SamplePosition(targetPlayer.position, out NavMeshHit playerHit, 2f, NavMesh.AllAreas))
                {
                    agent.SetDestination(playerHit.position);
                }
            }
        }

    }

    private void ExitChase()
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
        ParticlesManager.Instance.PlayEffect(transform.position, EffectType.SmallExplosion);
        RpcPlayDeathSound();
        
        OnRobotKilled?.Invoke(this);

        // powerup.TriggerEffect(player);
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

        if(col.CompareTag("Player"))
        {
            if (!IsServerInitialized) return;

            int targetId = col.GetComponent<NetworkObject>().Owner.ClientId;
            int attackerId = transform.GetComponent<NetworkObject>().Owner.ClientId;
            Debug.Log($"Kamikaze robot exploded: target: {targetId}");
            PlayerManager.Instance.DamagePlayer(targetId, Damage, attackerId);
            ParticlesManager.Instance.PlayEffect(transform.position, EffectType.Explosion);
            RpcPlayExplosionSound();
            Despawn(this.NetworkObject);
        }
    }

    [ObserversRpc]
    private void RpcPlayExplosionSound()
    {
        var manager = PersistentAudioSourceManager.GetInstance();
        if (manager != null && kamikazeAudioSource != null)
            manager.PlaySoundBasedOnRefencedSource(kamikazeAudioSource);
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