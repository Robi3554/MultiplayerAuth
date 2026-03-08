using System.Collections;
using System.Collections.Generic;
using FishNet.Component.Animating;
using FishNet.Object;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PredictionMoving : NetworkBehaviour
{
    [Header("Movement")]
    public float moveRate = 5f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float rotateRate = 7f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private NetworkAnimator netAnimator;

    [Header("Input")]
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private bool isJoystick;
    
    public bool IsJoystickMode => isJoystick;

    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [SerializeField] private float decelSpeed = 5f;

    [Header("Dash AfterImage")]
    [SerializeField] private float refreshRate = 0.2f;
    [SerializeField] private GameObject model;
    [SerializeField] private Material afterImageMaterial;
    [SerializeField] private int poolSize = 10;
    private Queue<AfterImageInstance> afterImagePool = new Queue<AfterImageInstance>();
    private SkinnedMeshRenderer[] characterMeshes;
    
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioSource dashAudioSource;
    [SerializeField] private AudioClip dashAudioClip;

    private Vector2 _moveInput;
    private bool _isAnalogMovement;
    private Vector2 _mouseLook, _joystickLook;
    internal bool canMove = true;
    private PlayerNetworkInitializer _playerNet;

    private Rigidbody _rb;
    private Camera _camera;
    private bool _isGrounded;
    private bool _jumpPressed;
    private bool _isDashing;
    private bool _canDash = true;
    private bool _isMovingBackwards;
    private PlayerStats _playerStats;
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _playerStats = GetComponent<PlayerStats>();
    }

    private void Start()
    {
        characterMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
        InitializePool();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
            _camera = Camera.main; // May be null during scene transition; Update will retry
    }
    
    // new input system
    public void OnMove(InputAction.CallbackContext context)
    {
        //don't process movement input while respawning
        if (_playerStats != null && _playerStats.isRespawning.Value)
        {
            _moveInput = Vector2.zero;
            return;
        }

        _moveInput = context.ReadValue<Vector2>();
        _isAnalogMovement = context.control.device is not Keyboard;
        
        _playerNet.ControlFootstepSoundsServer(this, _moveInput.magnitude);
    }

    //Why do we have the OnMouseLook and OnJoystickLook if we don't use them?
    public void OnMouseLook(InputAction.CallbackContext context)
    {
        //don't process look input while respawning
        if (_playerStats != null && _playerStats.isRespawning.Value)
            return;

        _mouseLook = context.ReadValue<Vector2>();
    }

    public void OnJoystickLook(InputAction.CallbackContext context)
    {
        //don't process look input while respawning
        if (_playerStats != null && _playerStats.isRespawning.Value)
            return;

        _joystickLook = context.ReadValue<Vector2>();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (_playerStats != null && _playerStats.isRespawning.Value)
            return;

        if (!context.performed || !_canDash || _isDashing || !canMove)
            return;

        _playerNet.PlayDashSoundServer(this);
        StartCoroutine(PerformDash());
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (_playerStats != null && _playerStats.isRespawning.Value)
            return;

        if (context.performed && _isGrounded && !_jumpPressed)
        {
            _jumpPressed = true;
            if (netAnimator)
                netAnimator.SetTrigger("Jumping");
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        
        // Don't allow movement while dead
        if (_playerStats != null && _playerStats.isRespawning.Value)
        {
            _rb.linearVelocity = Vector3.zero;
            animator.SetFloat("Velocity", 0f);
            return;
        }
        
        _isGrounded = Physics.CheckSphere(
            groundCheck.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);

        if (_isDashing) return;

        if (!canMove) return;

        Vector3 direction = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
        float speed = moveRate;
        Vector3 velocity = direction * speed;
        velocity.y = _rb.linearVelocity.y;

        //calculate if we are moving backwards
        float dotProductDirection = Vector3.Dot(transform.forward, direction);
        _isMovingBackwards = dotProductDirection < 0f && direction.magnitude > 0.1f;
        float movingBackwards = _isMovingBackwards ? -1f : 1f;
        animator.SetFloat("VelocityBackwardsValue", movingBackwards);
        if (_jumpPressed)
        {
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            _jumpPressed = false;
        }

        _rb.linearVelocity = velocity;

        if (_isAnalogMovement)
        {
            animator.SetFloat("Velocity", direction.magnitude);
        }
        else
        {
            var vel = direction.magnitude > 0 ? 0.5f : 0f;
            vel *= 1f;
            animator.SetFloat("Velocity", vel);
        }
    }
    
    private void Update()
    {
        if (!IsOwner || !_playerInput.currentActionMap.name.Equals("Gameplay")) return;

        // Retry Camera.main if it wasn't ready during OnStartClient (FishNet scene transition)
        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null) return;
        }

        float targetYaw = !isJoystick
            ? GetYawFromMouse()
            : GetYawFromJoystickOrMovement();

        Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation, rotateRate * Time.deltaTime);
    }
    
    public void ToggleInputMode()
    {
        isJoystick = !isJoystick;
        Debug.Log($"[PredictionMoving] Input mode toggled to: {(isJoystick ? "Joystick" : "Mouse & Keyboard")}");
    }
    
    public void SetInputMode(bool useJoystick)
    {
        isJoystick = useJoystick;
        Debug.Log($"[PredictionMoving] Input mode set to: {(isJoystick ? "Joystick" : "Mouse & Keyboard")}");
    }

    private float GetYawFromMouse()
    {
        if (_camera == null || Mouse.current == null)
            return transform.eulerAngles.y;

        Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 dir = (hit.point - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                return Quaternion.LookRotation(dir).eulerAngles.y;
        }

        return transform.eulerAngles.y;
    }

    private IEnumerator PerformDash()
    {
        _canDash = false;
        _isDashing = true;

        Vector3 dashDir = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
        if (dashDir == Vector3.zero)
            dashDir = transform.forward;

        float originalDamping = _rb.linearDamping;
        _rb.linearDamping = 0f;
        _rb.linearVelocity = Vector3.zero;

        _rb.AddForce(dashDir * dashForce, ForceMode.VelocityChange);

        StartCoroutine(SpawnAfterImages());

        yield return new WaitForSeconds(dashDuration);

        _rb.linearDamping = originalDamping;
        _isDashing = false;

        yield return new WaitForSeconds(dashCooldown);

        _canDash = true;
    }

    private IEnumerator SpawnAfterImages()
    {
        while (_isDashing)
        {
            CreateAfterImage();

            yield return new WaitForSeconds(refreshRate);
        }
    }

    #region AfterImageGeneration
    private void CreateAfterImage()
    {
        if (afterImagePool.Count == 0)
            return;

        AfterImageInstance instance = afterImagePool.Dequeue();

        instance.root.transform.position = transform.position;
        instance.root.transform.rotation = transform.rotation;

        for (int i = 0; i < characterMeshes.Length; i++)
        {
            characterMeshes[i].BakeMesh(instance.meshes[i]);

            instance.meshRenderers[i].transform.position =
                characterMeshes[i].transform.position;

            instance.meshRenderers[i].transform.rotation =
                characterMeshes[i].transform.rotation;

            instance.meshRenderers[i].transform.localScale =
                characterMeshes[i].transform.lossyScale;
        }

        instance.root.SetActive(true);
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            AfterImageInstance instance = new AfterImageInstance();

            instance.root = new GameObject("AfterImage_Pooled");
            instance.root.SetActive(false);

            int meshCount = characterMeshes.Length;
            instance.meshes = new Mesh[meshCount];
            instance.meshRenderers = new MeshRenderer[meshCount];

            for (int j = 0; j < meshCount; j++)
            {
                GameObject child = new GameObject(characterMeshes[j].name);
                child.transform.SetParent(instance.root.transform);

                MeshFilter mf = child.AddComponent<MeshFilter>();
                MeshRenderer mr = child.AddComponent<MeshRenderer>();

                Mesh mesh = new Mesh();
                mf.mesh = mesh;

                Material mat = new Material(afterImageMaterial);
                if (characterMeshes[j].material.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", characterMeshes[j].material.GetTexture("_BaseMap"));
                mr.material = mat;

                instance.meshes[j] = mesh;
                instance.meshRenderers[j] = mr;
            }

            var fade = instance.root.AddComponent<AfterImageFade>();
            fade.Initialize(this, instance);
            instance.fadeScript = fade;

            afterImagePool.Enqueue(instance);
        }
    }

    public void ReturnToPool(AfterImageInstance instance)
    {
        afterImagePool.Enqueue(instance);
    }
    #endregion

    private float GetYawFromJoystickOrMovement()
    {
        if (_joystickLook.sqrMagnitude > 0.1f)
        {
            Vector3 lookDir = new Vector3(_joystickLook.x, 0f, _joystickLook.y);
            return Quaternion.LookRotation(lookDir).eulerAngles.y;
        }
        
        Vector3 dir = new Vector3(_moveInput.x, 0f, _moveInput.y);
        if (dir.sqrMagnitude > 0.01f)
            return Quaternion.LookRotation(dir).eulerAngles.y;

        return transform.eulerAngles.y;
    }

    public void SetRunAnimFalse()
    {
        animator.SetFloat("Velocity", 0);
        _rb.linearVelocity = new Vector3(Mathf.Lerp(_rb.linearVelocity.x, 0, decelSpeed + Time.deltaTime), _rb.linearVelocity.y, Mathf.Lerp(_rb.linearVelocity.z, 0, decelSpeed + Time.deltaTime));
    }
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
    
    [ObserversRpc]
    public void ControlFootstepSounds(float speed)
    {
        if (footstepAudioSource)
        {
            if (speed > 0.05f)
            {
                footstepAudioSource.Play();
            }
            else
            {
                footstepAudioSource.Stop();
            }
        }
    }

    [ObserversRpc]
    public void PlayDashSound()
    {
        if (dashAudioSource && dashAudioClip)
        {
            dashAudioSource.PlayOneShot(dashAudioClip);
        }
    }
}
