using UnityEngine;
using Unity.Netcode;

/// <summary>
/// The central brain of the Enemy.
/// Coordinates Movement, Combat, and Visuals.
/// Holds NetworkVariables to sync state between Server logic and Client visuals.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(EnemyAnimation))]
public class EnemyController : NetworkBehaviour
{
    private enum AIState { Chasing, Attacking }

    [Header("Animation Settings")]
    [SerializeField] private Vector2 attackAnimationSpeedRange = new Vector2(0.8f, 1.2f);

    // --- Network State ---
    private readonly NetworkVariable<bool> _netIsMoving = new NetworkVariable<bool>(false);
    private readonly NetworkVariable<bool> _netIsAttacking = new NetworkVariable<bool>(false);
    private readonly NetworkVariable<int> _netAttackID = new NetworkVariable<int>(0);
    private readonly NetworkVariable<float> _netAttackSpeed = new NetworkVariable<float>(1f);

    // --- Components ---
    private HealthComponent _health;
    private EnemyMovement _movement;
    private EnemyCombat _combat;
    private EnemyAnimation _animation;

    private AIState _currentState;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _movement = GetComponent<EnemyMovement>();
        _combat = GetComponent<EnemyCombat>();
        _animation = GetComponent<EnemyAnimation>();
    }

    public override void OnNetworkSpawn()
    {
        // 1. Client Listeners
        _netIsAttacking.OnValueChanged += OnAttackStateChanged;
        _netIsMoving.OnValueChanged += OnMovingStateChanged;

        // Sync initial state for late joiners
        if (_netIsAttacking.Value)
        {
            _animation.ToggleAttackState(true, _netAttackID.Value, _netAttackSpeed.Value);
        }
        _animation.SetMoving(_netIsMoving.Value);

        // 2. Server Logic
        if (IsServer)
        {
            _health.OnDeath += HandleDeathLogic;
            _movement.InitializeServer();
            _currentState = AIState.Chasing;
        }
        else
        {
            // Clients don't run NavMesh logic
            _movement.DisableMovement();
        }
    }

    public override void OnNetworkDespawn()
    {
        _netIsAttacking.OnValueChanged -= OnAttackStateChanged;
        _netIsMoving.OnValueChanged -= OnMovingStateChanged;

        if (IsServer)
        {
            _health.OnDeath -= HandleDeathLogic;
        }
    }

    private void Update()
    {
        if (!IsServer || _health.IsDead) return;

        // 1. Sync Movement State
        // We check the Movement component to see if it's actually moving
        if (_netIsMoving.Value != _movement.IsMoving)
        {
            _netIsMoving.Value = _movement.IsMoving;
        }

        // 2. Update Combat Logic (Timers, Target Finding)
        _combat.UpdateTargeting();

        if (_combat.CurrentTarget == null) return;

        // 3. State Machine
        switch (_currentState)
        {
            case AIState.Chasing: HandleChasingState(); break;
            case AIState.Attacking: HandleAttackingState(); break;
        }

        // 4. Rotation
        _movement.RotateTowards(_combat.CurrentTarget.position);
    }

    #region Server AI Logic

    private void HandleChasingState()
    {
        float distance = Vector3.Distance(transform.position, _combat.CurrentTarget.position);

        if (distance > _combat.AttackRange)
        {
            _movement.MoveTo(_combat.CurrentTarget.position);
        }
        else if (distance <= _combat.TooCloseRange)
        {
            // Back away logic
            Vector3 retreatDirection = (transform.position - _combat.CurrentTarget.position).normalized;
            Vector3 retreatPos = transform.position + retreatDirection * 2f;
            // Simple sample, ideally check NavMesh.SamplePosition validity
            _movement.MoveTo(retreatPos);
        }
        else
        {
            // In range
            _movement.Stop();
            if (_combat.IsAttackReady)
            {
                StartAttackSequence();
            }
        }
    }

    private void HandleAttackingState()
    {
        float distance = Vector3.Distance(transform.position, _combat.CurrentTarget.position);
        if (distance > _combat.AttackRange)
        {
            Vector3 direction = (_combat.CurrentTarget.position - transform.position).normalized;
            // We move manually via agent if needed, or just let root motion handle it (if enabled)
            // Here we use the movement component helper but maybe slower?
            // For now, let's just keep it simple: don't move or move very slowly
        }
    }

    private void StartAttackSequence()
    {
        _currentState = AIState.Attacking;

        // Randomize
        int attackIndex = Random.Range(0, 2);
        float randomSpeed = Random.Range(attackAnimationSpeedRange.x, attackAnimationSpeedRange.y);

        // Sync
        _netAttackID.Value = attackIndex;
        _netAttackSpeed.Value = randomSpeed;
        _netIsAttacking.Value = true;
    }

    private void HandleDeathLogic()
    {
        _movement.DisableMovement();
        _movement.SnapToGround();

        _netIsMoving.Value = false;
        _netIsAttacking.Value = false;

        Invoke(nameof(DespawnEnemy), 5f);
    }

    private void DespawnEnemy()
    {
        if (IsServer && IsSpawned) GetComponent<NetworkObject>().Despawn();
    }

    #endregion

    #region Animation Callbacks (Server)

    // Called by AttackStateBehaviour
    public void OnAttackAnimationStarted() { }

    // Called by AttackStateBehaviour
    public void OnAttackAnimationFinished()
    {
        if (!IsServer) return;

        _currentState = AIState.Chasing;
        _combat.ResetAttackCooldown();
        _netIsAttacking.Value = false;
    }

    // Called via Animation Event -> Hit()
    public void Hit()
    {
        if (!IsServer) return;
        _combat.DealDamage();
    }

    #endregion

    #region Client Visual Responses

    private void OnAttackStateChanged(bool previous, bool current)
    {
        _animation.ToggleAttackState(current, _netAttackID.Value, _netAttackSpeed.Value);
    }

    private void OnMovingStateChanged(bool previous, bool current)
    {
        _animation.SetMoving(current);
    }

    #endregion

    public void Initialize(EnemySpawner spawner)
    {
        // Hook for spawner initialization if needed
    }
}