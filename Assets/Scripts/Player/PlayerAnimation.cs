using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Animator), typeof(PlayerMovement))]
public class PlayerAnimation : NetworkBehaviour
{
    [Header("Animation Smoothing")]
    [SerializeField] private float animationSmoothTime = 8f;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int DodgeParam = Animator.StringToHash("Dodge");
    private static readonly int AttackParam = Animator.StringToHash("Attack");

    private Animator _animator;
    private PlayerMovement _playerMovement;
    private float _currentSpeed;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        InputProcessor.onDodgeStarted += HandleDodge;
        InputProcessor.onAttackStarted += HandleAttack;
    }

    private void OnDisable()
    {
        InputProcessor.onDodgeStarted -= HandleDodge;
        InputProcessor.onAttackStarted -= HandleAttack;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // 1. Sync Speed
        float targetSpeed = _playerMovement.MoveInput.magnitude;
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * animationSmoothTime);

        if (_currentSpeed < 0.01f) _currentSpeed = 0f;

        _animator.SetFloat(SpeedParam, _currentSpeed);
    }

    // --- DODGE LOGIC ---

    private void HandleDodge()
    {
        if (!IsOwner) return;

        // 1. Play Immediately Locally (Instant response)
        _animator.SetTrigger(DodgeParam);

        // 2. Force Server to play it
        TriggerDodgeServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void TriggerDodgeServerRpc()
    {
        // Play on Server (God sees this)
        _animator.SetTrigger(DodgeParam);

        // Tell other clients (Third player sees this)
        TriggerDodgeClientRpc();
    }

    [Rpc(SendTo.NotServer)]
    private void TriggerDodgeClientRpc()
    {
        // Owner already played it, don't play twice
        if (IsOwner) return;
        _animator.SetTrigger(DodgeParam);
    }

    // --- ATTACK LOGIC ---

    private void HandleAttack()
    {
        if (!IsOwner) return;

        // 1. Play Immediately Locally
        _animator.SetTrigger(AttackParam);

        // 2. Force Server to play it
        TriggerAttackServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void TriggerAttackServerRpc()
    {
        // Play on Server (God sees this)
        _animator.SetTrigger(AttackParam);

        // Tell other clients
        TriggerAttackClientRpc();
    }

    [Rpc(SendTo.NotServer)]
    private void TriggerAttackClientRpc()
    {
        if (IsOwner) return;
        _animator.SetTrigger(AttackParam);
    }
}