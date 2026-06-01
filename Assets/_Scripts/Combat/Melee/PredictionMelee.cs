using FishNet.CodeGenerating;
using FishNet.Component.Animating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using NUnit.Framework.Constraints;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class PredictionMelee : NetworkBehaviour, IWeaponInfo
{
    [SerializeField] private int weaponId;

    public int WeaponId => weaponId;

    [Header("Weapon Settings")]

	[AllowMutableSyncType]
	[SerializeField] private SyncVar<float> cooldownTime = new SyncVar<float>(1.25f);
	[AllowMutableSyncType]
	[SerializeField] private SyncVar<int> damage = new SyncVar<int>(10);

    protected PlayerStats playerStats;

    protected int Damage => damage.Value * playerStats.damageMult;

    [Header("References")]
	[SerializeField] private ParticleSystem VFX_SLASH;
    [SerializeField] private AudioSource swingAudioSource;
    [SerializeField] private AudioClip swingAudioClip;

	[Header("Animation")]
	[SerializeField] private Animator animator;
	[SerializeField] private NetworkAnimator netAnimator;
	[SerializeField] private MeshCollider meshCollider;
	private static readonly int SlashTriggerHash = Animator.StringToHash("SlashTrigger");

	private static readonly int IsSlashingHash = Animator.StringToHash("IsSlashing");
	private CapsuleCollider playerCollider;
	private bool _meleePressed;
	[AllowMutableSyncType]
	private SyncVar<bool> _isOnCooldown = new SyncVar<bool>(false);
	private float _cooldownTimer;
	private bool _isAnimating = false;
	private bool Slash;

	[SerializeField]
	private GameObject ammoTextObj;

    public override void OnStartNetwork()
	{
		base.OnStartNetwork();
		playerCollider = GetComponentInParent<CapsuleCollider>();
		playerStats = GetComponentInParent<PlayerStats>();
	}

    private void OnEnable()
    {
        ammoTextObj.SetActive(false);
    }

    private void OnDisable()
    {
        ammoTextObj.SetActive(true);
    }

    public void OnDamage(InputAction.CallbackContext context)
	{
		if (!this.isActiveAndEnabled) return;

		//don't allow melee attacks while respawning
		if (playerStats != null && playerStats.isRespawning.Value)
			return;

		Debug.Log("Melee: left click pressed");
		if (context.performed)
		{
			_meleePressed = true;
		}
	}
	[ServerRpc] // a trebuit sa fac asa ca latra parrel syncu ca nu se seteaza sync vars pe server side
	private void StartCooldownServerRpc()
	{
		_isOnCooldown.Value = true;
	}
	[ServerRpc]
	private void ResetCooldownServerRpc()
	{
		_isOnCooldown.Value = false;
	}
	private void Update()
	{
		if (!IsOwner)
		{
			return;
		}

		if (_isOnCooldown.Value)
		{
			_cooldownTimer += Time.deltaTime;
			if (_cooldownTimer >= cooldownTime.Value)
			{
				ResetCooldownServerRpc();
				_cooldownTimer = 0f;
			}
		}

		// Check if animation has finished and reset the flag
		if (_isAnimating && !animator.GetBool(IsSlashingHash))
		{
			Debug.Log("Melee: Animation finished, resetting _isAnimating");
			_isAnimating = false;
			StartCooldownServerRpc();
			_cooldownTimer = 0f;
		}

		if (_meleePressed && !_isOnCooldown.Value && !_isAnimating && !animator.GetBool(IsSlashingHash))
		{
			Slash = true;
			PerformSlashRequestServerRpc();
			netAnimator.SetTrigger(SlashTriggerHash);
			_isAnimating = true;
			_meleePressed = false;
		}
	}
	void OnDrawGizmosSelected()
	{
		// Gizmos.color = Color.blue;
		// Gizmos.DrawSphere(slashPoint.position, attackRange.Value);
		// Gizmos.color = Color.yellow;
		// Gizmos.DrawLine(debugHitPosition, debugDirectionToTarget);
	}

	[ServerRpc(RequireOwnership = false)]
	private void PerformSlashRequestServerRpc()
	{
		Slash = true;
	}

	// [ServerRpc(RequireOwnership = false)]
	// private void PlayServerWeaponVfx()
	// {
	// 	Debug.Log("Melee: server weapon vfx");
	// 	PlayObserverWeaponVfx();
	// }

	[ObserversRpc]
	public void PlayObserverWeaponVfx()
	{
		if (VFX_SLASH != null && !VFX_SLASH.isPlaying)
		{
			VFX_SLASH.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			VFX_SLASH.Play();
		}
	}

	[ObserversRpc]
	public void PlayObserverWeaponSfx()
	{
		if (swingAudioSource != null && swingAudioClip != null)
		{
			swingAudioSource.PlayOneShot(swingAudioClip);
		}
	}

	public void DealDamage(Collider enemyCollider)
	{
		if (Slash)
		{
			Debug.Log("Melee: Attack pressed");
			if (enemyCollider.CompareTag("Player") && enemyCollider != playerCollider)
			{
				Debug.Log("Melee: Hit a player");
				int targetId = enemyCollider.transform.GetComponent<NetworkObject>().Owner.ClientId;
				int attackerId = transform.GetComponent<NetworkObject>().Owner.ClientId;
				Debug.Log($"DAVEEEEEEE: target: {targetId} aaaand attacker: {attackerId}");
				DamagePlayerServerRpc(targetId, Damage, attackerId);

			}
			else if (enemyCollider.CompareTag("Robot"))
			{
				Debug.Log("Melee: Hit robot!");
				if(enemyCollider.GetComponent<KamikazeRobot>() != null)
				{
					var robot = enemyCollider.GetComponent<KamikazeRobot>();
					robot.DestroyRobot(playerCollider.GetComponent<NetworkObject>());
					// DespawnRobotServerRpc(robot.NetworkObject);
				}
				else if(enemyCollider.GetComponent<LittleRobot>() != null)
				{
					var robot = enemyCollider.GetComponent<LittleRobot>();
					robot.DestroyRobot(playerCollider.GetComponent<NetworkObject>());
					// DespawnRobotServerRpc(robot.NetworkObject);
				}
			}
			else if (enemyCollider.GetComponentInParent<Turret>() is Turret turret)
			{
				Debug.Log("Melee: Hit turret!");
				DamageTurretServerRpc(turret.NetworkObject, Damage);
			}
			else if (enemyCollider.gameObject.layer == LayerMask.NameToLayer("Projectile"))
			{
				Debug.Log("Melee: Hit a bullet!");
				// Clients should not try to pass GameObjects/NetworkObjects over RPCs.
				// Instead report the hit position to the server and let the server
				// find and despawn the authoritative projectile objects.
				Vector3 hitPoint = enemyCollider.ClosestPoint(transform.position);
				ReportProjectileHitServerRpc(hitPoint);
			}

			_meleePressed = false;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void EndSlashWindow(){
		Slash = false;
	}

	// Client reports a local projectile hit point; server finds authoritative projectile(s)
	// near that point and despawns them. This avoids passing object references from
	// clients which may be null on dedicated server setups.
	[ServerRpc(RequireOwnership = false)]
	private void ReportProjectileHitServerRpc(Vector3 hitPoint)
	{
		if (!IsServer)
			return;

		// Tweak this radius to match projectile size / melee reach.
		float despawnRadius = 1.0f;
		Collider[] hits = Physics.OverlapSphere(hitPoint, despawnRadius);
		foreach (var c in hits)
		{
			if (c == null)
				continue;

			if (c.gameObject.layer == LayerMask.NameToLayer("Projectile"))
			{
				var netObj = c.GetComponent<NetworkObject>();
				if (netObj != null)
				{
					ServerManager.Despawn(netObj);
				}
				else
				{
					// If projectile isn't networked, destroy locally on server.
					GameObject.Destroy(c.gameObject);
				}
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void DespawnRobotServerRpc(NetworkObject robot)
	{
		// Debug.Log("DAVIDDDDDDDDDDDDDDDDDDDD: Despawned robot");
		RobotSpawnManager.Instance.DespawnRobot(robot);
		// Debug.Log("DAVIDDDDDDDDDDDDDDDDDDDD: OUT");
    }

	[ServerRpc(RequireOwnership = false)]
	private void DamagePlayerServerRpc(int targetId, int damageAmount, int attackerId)
	{
		PlayerManager.Instance.DamagePlayer(targetId, damageAmount, attackerId);
	}

	[ServerRpc(RequireOwnership = false)]
	private void DamageTurretServerRpc(NetworkObject turretObj, int damageAmount)
	{
		if (turretObj != null && turretObj.TryGetComponent(out Turret turret))
			turret.TakeDamage(damageAmount);
	}


    // Add this method to handle animation completion
    public void OnAnimationComplete()
    {
        // Debug.Log("Melee: Animation completed");
        _isAnimating = false;
    }
}