using FishNet.Object;
using UnityEngine;

public class ProjectileScript : NetworkBehaviour
{
    [SerializeField]
    private float maxTravelDistance;
    [SerializeField]
    private float minDamage;
    private int damage;
    private int attackerId;

    private int finalDamage;

    private Rigidbody rb;
    private Vector3 spawnPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void ServerInitialize(Vector3 velocity, int damage, float maxDistance, int attackerId)
    {
        spawnPosition = transform.position;

        this.damage = damage;
        this.maxTravelDistance = maxDistance;
        this.attackerId = attackerId;

        rb.linearVelocity = velocity;

        InitializeObserversRpc(velocity, damage, maxDistance, attackerId);
    }

    [ObserversRpc(BufferLast = true)]
    private void InitializeObserversRpc(Vector3 velocity, int damage, float maxDistance, int attackerId)
    {
        if (!IsServerInitialized)
        {
            spawnPosition = transform.position;
            this.damage = damage;
            this.maxTravelDistance = maxDistance;
            this.attackerId = attackerId;
            rb.linearVelocity = velocity;
        }
    }

    private void Update()
    {
        float distance = Vector3.Distance(spawnPosition, transform.position);

        float t = distance / maxTravelDistance;
        float falloff = Mathf.Lerp(damage, minDamage, t);
        finalDamage = (int)falloff;

        if (distance >= maxTravelDistance)
            ServerManager.Despawn(gameObject);
    }

    private void OnTriggerEnter(Collider col)
    {
        if (!IsServerInitialized)
            return;

        Debug.Log($"Hit attacker={attackerId}");

        if (col.gameObject.CompareTag("Player"))
        {
            var targetPlayer = col.GetComponent<NetworkObject>();
            var targetId = (int)targetPlayer.Owner.ClientId;

            PlayerManager.Instance.DamagePlayer(targetId, finalDamage, attackerId);
            DespawnProjectile();
        }
        else if (attackerId >= 0 && col.GetComponentInParent<Turret>() is Turret turret)
        {
            turret.TakeDamage(finalDamage);
            DespawnProjectile();
        }
        else if (col.gameObject.CompareTag("Wall"))
        {
            DespawnProjectile();
        }
    }

    private void DespawnProjectile()
    {
        if (IsServerInitialized)
        {
            ServerManager.Despawn(gameObject);
        }
    }
}