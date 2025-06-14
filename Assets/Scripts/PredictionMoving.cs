using UnityEngine;
using FishNet.Object;
using FishNet.Component.Animating;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;
using System.Collections.Generic;

public class PredictionMoving : TickNetworkBehaviour
{
    [Header("Server Side")]
    private float _serverMoveRate;
    private float _serverJumpForce;

    [Header("Non Onwer Reconciliation")]
    private readonly Queue<InterpolationData> _interpolationBuffer = new();
    private const float InterpolationDelay = 0.1f;

    [SerializeField] 
    private float moveRate = 5f;
    [SerializeField] 
    private float jumpForce = 7f;
    [SerializeField] 
    private LayerMask groundLayer;
    [SerializeField] 
    private Transform groundCheck;
    [SerializeField] 
    private float groundCheckRadius = 0.2f;
    [SerializeField] private Animator animator;
    [SerializeField] private NetworkAnimator netAnimator;
    Camera _camera;
    private PredictionRigidbody _predictionRb = new();
    private uint _lastReplicateTick;
    private bool _isGrounded;
    private bool _jumpPressed;

    private struct MoveData : IReplicateData
    {
        public float Horizontal;
        public float Vertical;
        public bool Jump;
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

    private struct InterpolationData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Time;
    }

    private void Awake()
    {
        _predictionRb.Initialize(GetComponent<Rigidbody>());
    }

    public override void OnStartNetwork()
    {
        SetTickCallbacks(TickCallback.Tick | TickCallback.PostTick);

        if(IsServerInitialized)
        {
            _serverMoveRate = moveRate;
            _serverJumpForce = jumpForce;
        }
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner) {
            _camera = Camera.main;
            if (_camera != null) {
                _camera.transform.SetParent(transform);
                _camera.transform.localPosition = new Vector3(0, 2, -5);
                _camera.transform.localRotation = Quaternion.Euler(10, 0, 0);
            }
        }
    }
    protected override void TimeManager_OnTick()
    {
        if (IsOwner)
            PerformReplicate(BuildMoveData());

        CreateReconcile();
    }

    private void Update()
    {
        if (IsOwner)
        {
            if (Input.GetButtonDown("Jump"))
            {
                _jumpPressed = true;
                netAnimator.SetTrigger("Jumping");
            }
        }


        if (IsOwner && _interpolationBuffer.Count < 2)
            return;

        float renderTime = Time.time - InterpolationDelay;

        InterpolationData from = default, to = default;

        bool found = false;

        foreach (var pair in _interpolationBuffer)
        {
            if (pair.Time >= renderTime)
            {
                from = pair;
            }
            else
            {
                to = pair;
                found = true;
                break;
            }
        }

        if (found)
        {
            float t = Mathf.InverseLerp(from.Time, to.Time, renderTime);
            transform.position = Vector3.Lerp(from.Position, to.Position, t);
            transform.rotation = Quaternion.Slerp(from.Rotation, to.Rotation, t);
        }
    }

    private void LateUpdate()
    {
    }

    private MoveData BuildMoveData()
    {
        MoveData data = new MoveData
        {
            Horizontal = Input.GetAxisRaw("Horizontal"),
            Vertical = Input.GetAxisRaw("Vertical"),
            Jump = _jumpPressed
        };

        _jumpPressed = false;
        return data;
    }

    public override void CreateReconcile()
    {
        ReconcileData rd = new ReconcileData
        {
            Rigidbody = _predictionRb
        };
        PerformReconcile(rd);
    }

    [Replicate]
    private void PerformReplicate(MoveData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        _lastReplicateTick = data.GetTick();

        float move = IsServerInitialized ? _serverMoveRate : moveRate;
        float jump = IsServerInitialized ? _serverJumpForce : jumpForce;

        Vector3 direction = new Vector3(data.Horizontal, 0f, data.Vertical).normalized;
        Vector3 velocity = direction * move;
        velocity.y = _predictionRb.Rigidbody.linearVelocity.y;
        animator.SetFloat("Velocity", velocity.magnitude / move);

        _isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);

        if (data.Jump && _isGrounded)
            velocity.y = jump;

        _predictionRb.Rigidbody.linearVelocity = velocity;
        _predictionRb.Simulate();
    }

    [Reconcile]
    private void PerformReconcile(ReconcileData data, Channel channel = Channel.Unreliable)
    {
        _predictionRb.Reconcile(data.Rigidbody);

        if (!IsOwner)
        {
            _interpolationBuffer.Enqueue(new InterpolationData
            {
                Position = _predictionRb.Rigidbody.position,
                Rotation = _predictionRb.Rigidbody.rotation,
                Time = Time.time,
            });

            while(_interpolationBuffer.Count > 10)
            {
                _interpolationBuffer.Dequeue();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
