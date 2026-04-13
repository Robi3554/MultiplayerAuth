using FishNet.CodeGenerating;
using FishNet.Component.Animating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;
using System.Collections.Generic;

public class PredictionMelee : NetworkBehaviour
{
	[Header("Weapon Settings")]

	[AllowMutableSyncType]
	[SerializeField] private SyncVar<float> cooldownTime = new SyncVar<float>(1.25f);
	[AllowMutableSyncType]
	[SerializeField] private SyncVar<float> attackRange = new SyncVar<float>(3f);
	[SerializeField] private float buffer = 0.5f; //extra distance to account for latency and hitbox size
	[AllowMutableSyncType]
	[SerializeField] private SyncVar<int> damage = new SyncVar<int>(10);

    protected PlayerStats playerStats;

    protected int Damage => damage.Value * playerStats.damageMult;

    [Header("References")]
	[SerializeField] private float coneAngle = 60f;
	[SerializeField] private VisualEffect VFX_SLASH;

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
	private List<int> _hitTargetsThisSwing = new List<int>();
	public override void OnStartNetwork()
	{
		base.OnStartNetwork();
		playerCollider = GetComponentInParent<CapsuleCollider>();
		playerStats = GetComponentInParent<PlayerStats>();
		// VFXEventAttribute EventSettings = VFX_SLASH.CreateVFXEventAttribute();	
	}

	public void OnDamage(InputAction.CallbackContext context)
	{
		if (!this.isActiveAndEnabled) return;

		//don't allow melee attacks while respawning
		if (playerStats != null && playerStats.isRespawning.Value)
			return;

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
			_isAnimating = false;
			StartCooldownServerRpc();
			_cooldownTimer = 0f;
		}

		if (_meleePressed && !_isOnCooldown.Value && !_isAnimating && !animator.GetBool(IsSlashingHash))
		{
			// Slash = true;
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

	
	public void PlayObserverWeaponVfx()
	{
		if (VFX_SLASH != null)
		{
			VFX_SLASH.SendEvent("OnSlash");
		}
	}

	public void DealDamage(Collider enemyCollider)
	{
		if (!IsOwner) return;
		if (!Slash) return;
		
		int id = enemyCollider.gameObject.GetInstanceID(); //this is used to track who was hit this swing, so they dont get hit twice
		if (_hitTargetsThisSwing.Contains(id)) return; //already hit this person!
		_hitTargetsThisSwing.Add(id);

		if (enemyCollider.CompareTag("Player") && enemyCollider != playerCollider)
		{
			if (enemyCollider.TryGetComponent(out NetworkObject playerNetObj))
			{
				RequestDamage(playerNetObj);
			}
		}
		else if (enemyCollider.CompareTag("Robot"))
		{
			NetworkObject robotNob = enemyCollider.GetComponentInParent<NetworkObject>();
			if (robotNob != null)
			{
				RequestRobotDestroyServerRpc(robotNob);
			}else 
			{
				Debug.LogWarning($"Hit Robot tag on {enemyCollider.name} but found no NetworkObject!");
			}
		}
		
		_meleePressed = false;
	
	}
	private void RequestDamage(NetworkObject target)
	{
		// if(Vector3.Distance(transform.position, target.transform.position) > attackRange.Value + buffer) return; //check distance on server to prevent cheating with hitbox size or latency

		if (target != null && target.Owner.IsValid)
		{
			int targetId = target.Owner.ClientId;
			int attackerId = Owner.ClientId;
			
			PlayerManager.Instance.DamagePlayer(targetId, Damage, attackerId);
		}
	}

	private void RequestRobotDestroyServerRpc(NetworkObject robotNob)
	{
		if (robotNob == null) return;

		if (robotNob.TryGetComponent(out KamikazeRobot kami))
		{
			kami.DestroyRobot(playerCollider.GetComponent<NetworkObject>());
		}
		else if (robotNob.TryGetComponent(out LittleRobot little))
		{
			little.DestroyRobot(playerCollider.GetComponent<NetworkObject>());
		}
		RobotSpawnManager.Instance.DespawnRobot(robotNob); //despawn on server
	}
	[ServerRpc(RequireOwnership = false)]
	public void EndSlashWindow()
	{
		Slash = false;
		_hitTargetsThisSwing.Clear();
	}

    //[ServerRpc(RequireOwnership = false)]
	// private void DespawnRobot(NetworkObject robot)
	// {
	// 	RobotSpawnManager.Instance.DespawnRobot(robot);
    // }


    // Add this method to handle animation completion
    public void OnAnimationComplete()
    {
        _isAnimating = false;
    }
}
