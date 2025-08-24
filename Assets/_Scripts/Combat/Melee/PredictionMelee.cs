using FishNet.CodeGenerating;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using LiteNetLib;
using UnityEngine;

public class PredictionMelee : NetworkBehaviour
{
    [Header("Weapon Settings")]

	[AllowMutableSyncType]
	[SerializeField] private SyncVar<float> cooldownTime = new SyncVar<float>(1f);
	[AllowMutableSyncType]
    [SerializeField] private SyncVar<float> attackRange = new SyncVar<float>(3f);
	[AllowMutableSyncType]
    [SerializeField] private SyncVar<int> damage = new SyncVar<int>(10);

    [Header("References")]
    [SerializeField] private Transform slashPoint;
    [SerializeField] private float coneAngle = 60f;

	private CapsuleCollider playerCollider;
	private bool _meleePressed;
    private bool _processedMelee;
    private bool _isOnCooldown;
    private float _cooldownTimer;
    private Vector3 debugDirectionToTarget = new Vector3(0, 0, 0);
	private Vector3 debugHitPosition = new Vector3(0, 0, 0);
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

		Debug.Log($"OnStartNetwork called. TimeManager: {base.TimeManager}");

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
			if (_cooldownTimer >= cooldownTime.Value)
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
				Direction = slashPoint.up,
				Position = slashPoint.position
			};

			PerformReplicate(attackData);
			_meleePressed = false;
		}

		CreateReconcile();
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(slashPoint.position, attackRange.Value);
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(debugHitPosition, debugDirectionToTarget);
    }

	void OnDrawGizmos()
	{
		
    }

	public override void OnStartServer()
	{
		base.OnStartServer();
		Debug.Log("PredictionMelee: OnStartServer called");
	}
	public override void OnStartClient()
{
    base.OnStartClient();
    Debug.Log($"PredictionMelee: OnStartClient called. IsServerInitialized: {IsServerInitialized}");
}

	[Replicate]
	private void PerformReplicate(AttackData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
	{
		if (data.Slash && IsServerInitialized) // IsServerInitialized is false here on client, idk why
		{
			Debug.Log("Melee: Attack pressed");
			//get what we can hit, check if the enemy is in front of the player, call dmg function on server and start cooldown timer after hit
			Collider[] hits = Physics.OverlapSphere(slashPoint.position, attackRange.Value);
			// we could add more stuff here. if we want to apply damage the closest enemy only for e.g. 
			// currently, it damages every enemy in area technically (did not test yet, only 1 target)
			foreach (var hit in hits)
			{
				if (hit.CompareTag("Player") && hit != playerCollider)
				{
					debugDirectionToTarget = data.Position;
					debugHitPosition = hit.transform.position;
					// get the horizontal direction to the target
					Vector3 directionToTarget = hit.transform.position - data.Position;
					directionToTarget.y = 0; // zero out the vertical component
					directionToTarget.Normalize(); // normalize the direction

					// get the horizontal forward direction of the slash point
					Vector3 forwardDirection = data.Direction;
					forwardDirection.y = 0; 
					forwardDirection.Normalize(); 

					Debug.Log($"Direction to target: {directionToTarget}");

					float angle = Mathf.Atan2(directionToTarget.x, directionToTarget.z) - Mathf.Atan2(forwardDirection.x, forwardDirection.z);
					angle = Mathf.Abs(angle * Mathf.Rad2Deg);
					if (angle > 180) // there s sometimes a bug that return angle between 320 and 350 ish. happened a few times
					{
						angle = 360 - angle;
					}
					Debug.Log($"Angle: {angle}");

					if (angle <= coneAngle * 0.5f) // half the cone angle 
					{
						Debug.Log("Melee: Hit a player");
						int targetId = hit.transform.GetComponent<NetworkObject>().Owner.ClientId;
						int attackerId = transform.GetComponent<NetworkObject>().Owner.ClientId;
						PlayerManager.Instance.DamagePlayer(targetId, damage.Value, attackerId);
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
