using FishNet.Object;
using UnityEngine;
using FishNet.Connection;
using System.Collections.Generic;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;

public class PredictionShooting : TickNetworkBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 10f;

    private bool _firePressed;

    private struct ShootData : IReplicateData
    {
        public Vector3 Direction;
        private uint _tick;

        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    private struct ReconcileData : IReconcileData
    {
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public override void OnStartNetwork()
    {
        SetTickCallbacks(TickCallback.Tick);
    }

    private void Update()
    {
        if (IsOwner && Input.GetMouseButtonDown(0))
            _firePressed = true;
    }

    protected override void TimeManager_OnTick()
    {
        if (!IsOwner || !_firePressed)
            return;

        _firePressed = false;

        ShootData data = new ShootData
        {
            Direction = firePoint.forward
        };

        PerformReplicate(data);
    }

    [Replicate]
    private void PerformReplicate(ShootData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        Debug.Log($"[Shoot] Replicating. IsServer: {IsServerStarted}, IsOwner: {IsOwner}");

        // Instantiate locally
        GameObject go = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(data.Direction));

        ProjectileScript proj = go.GetComponent<ProjectileScript>();
        proj.SetInitialVelocity(data.Direction * projectileSpeed);

        // Server spawns the authoritative copy
        if (IsServerStarted)
        {
            Spawn(go);
            Debug.Log("[Shoot] Spawned on server");
        }
        else
        {
            Debug.Log("[Shoot] Predicted projectile instantiated on client");
            // Do NOT call Spawn() on client
        }
    }


    [Reconcile]
    private void PerformReconcile(ReconcileData data, Channel channel = Channel.Unreliable)
    {
        
    }

    public override void CreateReconcile()
    {
        ReconcileData data = new ReconcileData();
        PerformReconcile(data);
    }
}
