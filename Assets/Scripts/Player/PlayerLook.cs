using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Manages the player's rotation.
/// Refactored for Multiplayer: Only rotates if IsOwner is true.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PlayerLook : NetworkBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The main camera used to cast rays for aiming.")]
    [SerializeField] private Camera mainCamera;

    [Header("Configuration")]
    [Tooltip("The layer mask representing the ground plane.")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("How quickly the player turns to face the movement or aim direction.")]
    [SerializeField][Min(0)] private float rotationSpeed = 15f;

    private PlayerMovement _playerMovement;
    private Vector2 _aimInput;
    private bool _isAiming = false;

    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        InputProcessor.onAim += HandleAim;
        InputProcessor.onAttackStarted += HandleAttackStarted;
        InputProcessor.onAttackCanceled += HandleAttackCanceled;
    }

    private void OnDisable()
    {
        InputProcessor.onAim -= HandleAim;
        InputProcessor.onAttackStarted -= HandleAttackStarted;
        InputProcessor.onAttackCanceled -= HandleAttackCanceled;
    }

    private void Update()
    {
        // MULTIPLAYER FIX: Only rotate our own character.
        // NetworkTransform will sync this rotation to others.
        if (!IsOwner) return;

        if (_isAiming)
        {
            RotatePlayerToAimPoint();
        }
        else
        {
            RotatePlayerToMoveDirection();
        }
    }

    private void HandleAim(Vector2 input)
    {
        if (!IsOwner) return;
        _aimInput = input;
    }

    private void HandleAttackStarted()
    {
        if (!IsOwner) return;
        _isAiming = true;
    }

    private void HandleAttackCanceled()
    {
        if (!IsOwner) return;
        _isAiming = false;
    }

    private void RotatePlayerToAimPoint()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(_aimInput);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, groundMask))
        {
            Vector3 lookPoint = hit.point;
            Vector3 direction = lookPoint - transform.position;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void RotatePlayerToMoveDirection()
    {
        Vector3 moveDirection = _playerMovement.MoveDirection;
        if (moveDirection == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }
}