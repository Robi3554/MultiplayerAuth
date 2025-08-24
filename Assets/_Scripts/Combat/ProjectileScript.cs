using FishNet.Object;
using FishNet.Object.Prediction;
using UnityEngine;

public class ProjectileScript : NetworkBehaviour
{
    [SerializeField] private float _lifetime = 3f;
    private Rigidbody _rigidbody;
    private Vector3 _velocity;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 velocity)
    {
        _velocity = velocity;
        _rigidbody.linearVelocity = velocity;

        if (IsServerInitialized)
        {
            Invoke(nameof(DestroyProjectile), _lifetime);
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        if (IsServerInitialized || base.Owner.IsLocalClient)
        {
            base.TimeManager.OnTick += OnTick;
        }
    }

    private void OnTick()
    {
        if (IsServerInitialized || IsOwner)
        {
            _rigidbody.MovePosition(transform.position + _velocity * (float)base.TimeManager.TickDelta);
        }
    }

    private void DestroyProjectile()
    {
        if (IsServerInitialized)
        {
            Despawn(gameObject);
        }
    }

    public override void OnStopNetwork()
    {
        if (base.TimeManager != null)
        {
            base.TimeManager.OnTick -= OnTick;
        }
    }
}