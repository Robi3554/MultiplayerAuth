using FishNet.CodeGenerating;
using FishNet.Component.Animating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

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
	[SerializeField] private ParticleSystem VFX_SLASH;

	[Header("Animation")]
    [SerializeField] private NetworkAnimator netAnimator;
	private CapsuleCollider playerCollider;
	private bool _meleePressed;
    private bool _isOnCooldown = false;
    private float _cooldownTimer;
    private Vector3 debugDirectionToTarget = new Vector3(0, 0, 0);
	private Vector3 debugHitPosition = new Vector3(0, 0, 0);
	private bool Slash;
	private Vector3 Direction;
	private Vector3 Position;

	public override void OnStartNetwork()
	{
		base.OnStartNetwork();
		playerCollider = GetComponentInParent<CapsuleCollider>();
	}

	public void OnDamage(InputAction.CallbackContext context)
	{
		if (!this.isActiveAndEnabled) return;
		
		Debug.Log("Melee: left click pressed");
		if (context.performed && !_isOnCooldown)
		{
			_meleePressed = true;
			netAnimator.SetTrigger("SlashTrigger");
			Debug.Log("do your job wtf");
		}
	}

	private void FixedUpdate()
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
		// Debug.Log($"Melee: checking if all conditions for attack are right: IsOwner:{IsOwner}, _meleePressed:{_meleePressed}, _isOnCooldown:{_isOnCooldown}");

		if (IsOwner && _meleePressed && !_isOnCooldown)
		{
			Direction = -slashPoint.up;
			Position = slashPoint.position;
			PerformSlashRequestServerRpc(Direction, Position);
			 _meleePressed = false;
		}
	}
	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(slashPoint.position, attackRange.Value);
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(debugHitPosition, debugDirectionToTarget);
    }

	[ServerRpc]
	private void PerformSlashRequestServerRpc(Vector3 direction, Vector3 position)
	{
		Slash = true;
		Direction = direction;
		Position = position;
		PerformSlash();
	}
	[ObserversRpc]
	private void PlaySlashVfx()
	{
		VFX_SLASH.Play();
	}
	private void PerformSlash()
	{
		if (Slash)
		{
			Debug.Log("Melee: Attack pressed");
			PlaySlashVfx();
			//get what we can hit, check if the enemy is in front of the player, call dmg function on server and start cooldown timer after hit
			Collider[] hits = Physics.OverlapSphere(slashPoint.position, attackRange.Value);
			// we could add more stuff here. if we want to apply damage the closest enemy only for e.g. 
			// currently, it damages every enemy in area technically (did not test yet, only 1 target)
			foreach (var hit in hits)
			{
				if (hit.CompareTag("Player") && hit != playerCollider)
				{
					debugDirectionToTarget = Position;
					debugHitPosition = hit.transform.position;
					// get the horizontal direction to the target
					Vector3 directionToTarget = hit.transform.position - Position;
					directionToTarget.y = 0; // zero out the vertical component
					directionToTarget.Normalize(); // normalize the direction

					// get the horizontal forward direction of the slash point
					Vector3 forwardDirection = Direction;
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
			_meleePressed = false;
			Slash = false;
		}
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
}
