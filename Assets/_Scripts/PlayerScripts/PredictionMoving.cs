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
    
    private Vector2 _moveInput;
    private bool _isAnalogMovement;
    private Vector2 _mouseLook, _joystickLook;
    internal bool canMove = true;

    private Rigidbody _rb;
    private Camera _camera;
    private bool _isGrounded;
    private bool _jumpPressed;
    private bool _isSprinting;
    private bool _isMovingBackwards;
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
            _camera = Camera.main;
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

    public void OnSprint(InputAction.CallbackContext context)
    {
        _isSprinting = context.ReadValueAsButton();
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
        
        Vector3 direction = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
        float speed = _isSprinting ? moveRate * sprintMultiplier : moveRate;
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
            vel *= _isSprinting ? 2f : 1f;
            animator.SetFloat("Velocity", vel);
        }
    }
    
    private void Update()
    {
        if (!IsOwner || !_playerInput.currentActionMap.name.Equals("Gameplay")) return;

        float targetYaw = !isJoystick
            ? GetYawFromMouse()
            : GetYawFromJoystickOrMovement();

        Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation, rotateRate * Time.deltaTime);
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
