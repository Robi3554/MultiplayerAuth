using FishNet.Component.Ownership;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;
using UnityEngine;

public class ProjectileScript : TickNetworkBehaviour
{
    private PredictionRigidbody _predictionRb = new();
    private Vector3 _initialVelocity;
    private bool _initialized;

    private struct MoveData : IReplicateData
    {
        public Vector3 Velocity;
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    private struct ReconcileData : IReconcileData
    {
        public PredictionRigidbody Rigidbody;
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    private void Awake()
    {
        _predictionRb.Initialize(GetComponent<Rigidbody>());
    }

    public void SetInitialVelocity(Vector3 velocity)
    {
        if (!_initialized)
        {
            _initialVelocity = velocity;
            _initialized = true;
            Debug.Log($"[Projectile] SetInitialVelocity called with {velocity}");
        }
    }

    public override void OnStartNetwork()
    {

        Debug.Log($"[Projectile] OnTick called. Initialized: {_initialized}, isOwner: {base.Owner.IsLocalClient}");

        SetTickCallbacks(TickCallback.Tick);

        if (IsServerStarted)
            Destroy(gameObject, 3f);
    }

    protected override void TimeManager_OnTick()
    {
        if (!_initialized)
            return;

        MoveData data = new MoveData
        {
            Velocity = _initialVelocity
        };

        PerformReplicate(data);
    }

    [Replicate]
    private void PerformReplicate(MoveData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        if (!_initialized)
        {
            _initialVelocity = data.Velocity;
            _initialized = true;
            Debug.Log($"[Projectile] Initialized on {(IsServerInitialized ? "Server" : "Client")} with velocity {_initialVelocity}");
        }

        _predictionRb.Rigidbody.linearVelocity = data.Velocity;
        _predictionRb.Simulate();
    }


    [Reconcile]
    private void PerformReconcile(ReconcileData data, Channel channel = Channel.Unreliable)
    {
        _predictionRb.Reconcile(data.Rigidbody);
    }

    public override void CreateReconcile()
    {
        ReconcileData data = new ReconcileData
        {
            Rigidbody = _predictionRb
        };

        PerformReconcile(data);
    }
}
