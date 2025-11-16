using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

public class PredictionShooting : NetworkBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 10f;

    private bool _firePressed;
    private bool _processedFire;

    private struct ShootData : IReplicateData
    {
        public bool Fire;
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
        base.OnStartNetwork();
        if (base.Owner.IsLocalClient)
        {
            base.TimeManager.OnTick += OnTick;
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            if (Input.GetMouseButton(0) && !_processedFire)
            {
                Debug.Log("[Shooting] Fire button pressed");
                _firePressed = true;
                _processedFire = true;
            }
            else if (!Input.GetMouseButton(0))
            {
                _processedFire = false;
            }
        }
    }

    private void OnTick()
    {
        if (IsOwner && _firePressed)
        {
            Debug.Log("[Shooting] Processing fire input");

            ShootData shootData = new ShootData
            {
                Fire = true,
                Direction = firePoint.forward
            };

            PerformReplicate(shootData);
            _firePressed = false;
        }

        CreateReconcile();
    }

    [Replicate]
    private void PerformReplicate(ShootData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        Debug.Log($"[Shooting] Replicate Received - Fire:{data.Fire} IsServer:{IsServerInitialized} State:{state}");

        if (data.Fire && IsServerInitialized)
        {
            Debug.Log($"[Shooting] Server spawning projectile at {firePoint.position}");

            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(data.Direction));
            NetworkObject netObject = projectile.GetComponent<NetworkObject>();

            if (netObject == null)
            {
                Debug.LogError("[Shooting] Missing NetworkObject on projectile!");
                return;
            }

            Spawn(netObject);

            ProjectileScript proj = projectile.GetComponent<ProjectileScript>();
            if (proj == null)
            {
                Debug.LogError("[Shooting] Missing ProjectileScript on projectile!");
                return;
            }

            //proj.Initialize(data.Direction * projectileSpeed);
        }
    }

    [Reconcile]
    private void PerformReconcile(ReconcileData data, Channel channel = Channel.Unreliable)
    {
        Debug.Log("[Shooting] Reconciliation");
    }

    public override void CreateReconcile()
    {
        ReconcileData rd = new ReconcileData();
        PerformReconcile(rd);
    }

    public override void OnStopNetwork()
    {
        if (base.TimeManager != null)
        {
            base.TimeManager.OnTick -= OnTick;
        }
    }
}