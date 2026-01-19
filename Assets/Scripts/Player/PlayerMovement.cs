using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles player movement using standard velocity (Code-Driven).
/// Refactored for Multiplayer: Only runs logic if IsOwner is true.
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(Animator), typeof(HealthComponent))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Base movement speed in meters per second.")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("How fast the character moves while rolling/dodging.")]
    [SerializeField] private float dodgeSpeed = 10f;
    [Tooltip("How fast the character turns to face movement direction.")]
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Physics")]
    [Tooltip("The force of gravity applied to the player.")]
    [SerializeField] private float gravity = -19.62f;

    public Vector2 MoveInput { get; private set; }
    public Vector3 MoveDirection { get; private set; }

    private CharacterController _controller;
    private Animator _animator;
    private HealthComponent _health;
    private Transform _cameraTransform;
    private Vector3 _verticalVelocity;
    private bool _isDodging = false;
    private Vector3 _currentDodgeDirection;
    private bool _isDead = false;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _health = GetComponent<HealthComponent>();

        // Disable Root Motion so our code drives everything.
        _animator.applyRootMotion = false;
    }

    public override void OnNetworkSpawn()
    {
        _health.OnDeath += OnDeath;

        if (!IsOwner) return;

        SetupSurvivorCamera();
    }

    public override void OnNetworkDespawn()
    {
        _health.OnDeath -= OnDeath;
    }

    private void SetupSurvivorCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        _cameraTransform = mainCam.transform;

        // Disable the God camera logic.
        if (mainCam.TryGetComponent<GodCameraController>(out var gc))
        {
            gc.enabled = false;
        }

        // Enable and assign the survivor follow camera.
        if (mainCam.TryGetComponent<CameraController>(out var sc))
        {
            sc.enabled = true;
            sc.SetTarget(this.transform);
        }
    }

    private void OnEnable()
    {
        InputProcessor.onMove += HandleMove;
    }

    private void OnDisable()
    {
        InputProcessor.onMove -= HandleMove;
    }

    private void OnDeath()
    {
        _isDead = true;
        // Disable the controller so we don't slide or collide while dead
        if (_controller != null) _controller.enabled = false;
    }

    private void Update()
    {
        if (!IsOwner || _isDead) return;

        if (_isDodging)
        {
            PerformDodgeMovement();
        }
        else
        {
            MoveCharacter();
        }

        ApplyGravity();
    }

    private void HandleMove(Vector2 input)
    {
        if (!IsOwner || _isDodging || _isDead) return;
        MoveInput = input;
    }

    /// <summary>
    /// Locks movement direction and starts the dodge state.
    /// </summary>
    public void BeginDodge()
    {
        if (_isDead) return;
        _isDodging = true;

        if (MoveDirection.magnitude > 0.1f)
        {
            _currentDodgeDirection = MoveDirection.normalized;
        }
        else
        {
            _currentDodgeDirection = transform.forward;
        }
    }

    /// <summary>
    /// Unlocks movement and ends the dodge state.
    /// </summary>
    public void EndDodge()
    {
        _isDodging = false;
    }

    private void PerformDodgeMovement()
    {
        _controller.Move(_currentDodgeDirection *
                         dodgeSpeed * Time.deltaTime);
    }

    private void MoveCharacter()
    {
        if (_cameraTransform == null) return;

        Vector3 camForward = _cameraTransform.forward;
        Vector3 camRight = _cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        MoveDirection = (camForward * MoveInput.y + camRight * MoveInput.x);

        if (MoveDirection.magnitude > 0.1f)
        {
            _controller.Move(MoveDirection * moveSpeed * Time.deltaTime);

            Quaternion targetRotation =
                Quaternion.LookRotation(MoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _verticalVelocity.y < 0)
        {
            _verticalVelocity.y = -2f;
        }
        else
        {
            _verticalVelocity.y += gravity * Time.deltaTime;
        }

        _controller.Move(_verticalVelocity * Time.deltaTime);
    }
}