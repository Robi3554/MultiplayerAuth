using System.Collections;
using FishNet.Component.Animating;
using FishNet.Object;
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

    private Vector2 _moveInput;
    private bool _isAnalogMovement;
    private Vector2 _mouseLook, _joystickLook;
    internal bool canMove = true;

    private Rigidbody _rb;
    private Camera _camera;
    private bool _isGrounded;
    private bool _jumpPressed;
    private bool _isDashing;
    private bool _canDash = true;
    private bool _isMovingBackwards;
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
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
        _moveInput = context.ReadValue<Vector2>();
        _isAnalogMovement = context.control.device is not Keyboard;
    }

    public void OnMouseLook(InputAction.CallbackContext context)
    {
        _mouseLook = context.ReadValue<Vector2>();
    }

    public void OnJoystickLook(InputAction.CallbackContext context)
    {
        _joystickLook = context.ReadValue<Vector2>();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed && _canDash && !_isDashing)
            StartCoroutine(PerformDash());
    }

    public void OnJump(InputAction.CallbackContext context)
    {
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
        
        _isGrounded = Physics.CheckSphere(
            groundCheck.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);

        if (_isDashing) return;

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

        yield return new WaitForSeconds(dashDuration);

        _rb.linearDamping = originalDamping;
        _isDashing = false;

        yield return new WaitForSeconds(dashCooldown);

        _canDash = true;
    }

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

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
