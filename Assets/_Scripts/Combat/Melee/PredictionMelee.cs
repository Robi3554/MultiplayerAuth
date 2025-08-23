using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

public class PredictionMelee : NetworkBehaviour
{
    [Header("Weapon Settings")]
	[SerializeField] private float cooldownTime = 1f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private int damage = 10;

    [Header("References")]
    [SerializeField] private Collider meleeCollider;
    [SerializeField] private Transform slashPoint;
    [SerializeField] private float coneAngle = 60f;

	private CapsuleCollider playerCollider;
	private bool _meleePressed;
    private bool _processedMelee;
    private bool _isOnCooldown;
    private float _cooldownTimer;

    private struct AttackData : IReplicateData
    {
	    public bool Slash;
	    public Vector3 Direction;
	    public Vector3 Position;
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
		playerCollider = GetComponentInParent<CapsuleCollider>();
	}

    private void Update()
	{
		if (!IsOwner)
		{
			return;
		}

		if (_isOnCooldown)
		{
			_cooldownTimer += Time.deltaTime;
			if (_cooldownTimer >= cooldownTime)
			{
				_isOnCooldown = false;
				_cooldownTimer = 0f;
			}
		}

		if (Input.GetMouseButton(0) && !_processedMelee && !_isOnCooldown)
		{
			_meleePressed = true;
			_processedMelee = true;
		}
		else if (!Input.GetMouseButton(0))
		{
			_processedMelee = false;
		}
	}

    private void OnTick()
	{
		if (IsOwner && _meleePressed && !_isOnCooldown)
		{
			AttackData attackData = new AttackData
			{
				Slash = true,
				Direction = slashPoint.forward,
				Position = slashPoint.position
			};

			PerformReplicate(attackData);
			_meleePressed = false;
		}

		CreateReconcile();
	}

	[Replicate]
	private void PerformReplicate(AttackData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
	{
		if (data.Slash && IsServerInitialized)
		{
			Debug.Log("Melee: Attack pressed");
		//get what we can hit, check if the enemy is in front of the player, call dmg function on server and start cooldown timer after hit
			Collider[] hits = Physics.OverlapSphere(slashPoint.position, attackRange);

		foreach (var hit in hits)
		{
			if (hit.CompareTag("Player") && hit != playerCollider)
			{
				Debug.Log("Melee: Detected a player in range");
				Vector3 directionToTarget = hit.transform.position - data.Position;
				float angle = Vector3.Angle(data.Direction, directionToTarget);

				if (angle <= coneAngle * 0.5f) // Half the cone angle
				{
					Debug.Log("Melee: Hit a player");
				    int targetId = hit.transform.GetComponent<NetworkObject>().Owner.ClientId;
				    int attackerId = transform.GetComponent<NetworkObject>().Owner.ClientId;
				    PlayerManager.Instance.DamagePlayer(targetId, damage, attackerId);
				}
			}
		}

		_isOnCooldown = true;
		_cooldownTimer = 0f;
		}
	}

	[Reconcile]
	private void PerformReconcile(ReconcileData data, Channel channel = Channel.Unreliable)
	{

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
