using FishNet.Object;
using UnityEngine;

public class ProjectileScript : NetworkBehaviour
{
    [SerializeField]
    private float maxTravelDistance;
    [SerializeField]
    private float minDamage;
    private int damage;

    private int finalDamage;

    private Rigidbody rb;
    private Vector3 spawnPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void ServerInitialize(Vector3 velocity, int damage, float maxDistance)
    {
        spawnPosition = transform.position;

        this.damage = damage;
        this.maxTravelDistance = maxDistance;

        rb.linearVelocity = velocity;

        InitializeObserversRpc(velocity, damage, maxDistance);
    }

    public void InitializeClientProjectile(Vector3 velocity, int damage, float maxDistance)
    {
        spawnPosition = transform.position;

        this.damage = damage;
        this.maxTravelDistance = maxDistance;

        rb.linearVelocity = velocity;
    }

    [ObserversRpc(BufferLast = true)]
    private void InitializeObserversRpc(Vector3 velocity, int damage, float maxDistance)
    {
        if (!IsServerInitialized)
        {
            spawnPosition = transform.position;
            this.damage = damage;
            this.maxTravelDistance = maxDistance;
            rb.linearVelocity = velocity;
        }
    }

    private void Update()
    {
        if (!IsServerInitialized)
            return;

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

        if (col.gameObject.CompareTag("Player"))
        {
            col.GetComponent<PlayerStats>().TakeDamage(finalDamage);
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