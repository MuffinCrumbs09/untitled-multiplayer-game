using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

[RequireComponent(typeof(HealthComponent), typeof(NavMeshAgent), typeof(EnemyAnimation))]
public class EnemyController : NetworkBehaviour
{
    private enum AIState { Chasing, Attacking }

    [Header("AI Ranges")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float tooCloseRange = 1f;

    [Header("Randomization Settings")]
    [SerializeField] private Vector2 scaleRange = new Vector2(0.9f, 1.1f);
    [SerializeField] private Vector2 speedRange = new Vector2(3.0f, 4.5f);
    [SerializeField] private Vector2 accelerationRange = new Vector2(6.0f, 10.0f);
    [SerializeField] private Vector2 radiusRange = new Vector2(0.3f, 0.4f);
    [SerializeField] private Vector2 postAttackDelayRange = new Vector2(0.5f, 1.5f);
    [SerializeField] private Vector2 attackAnimationSpeedRange = new Vector2(0.8f, 1.2f);

    [Header("Attack Configuration")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.7f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float retreatSpeed = 1.5f;
    [SerializeField] private float attackMoveSpeedMultiplier = 0.5f;

    private HealthComponent _health;
    private NavMeshAgent _agent;
    private EnemyAnimation _animation;
    private Transform _target;
    private float _cooldownTimer;
    private AIState _currentState;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _agent = GetComponent<NavMeshAgent>();
        _animation = GetComponent<EnemyAnimation>();

        _agent.stoppingDistance = 0;
        _agent.updateRotation = false;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            _agent.enabled = false;
            return;
        }

        _health.OnDeath += HandleDeath;

        ApplyRandomStats();
        _currentState = AIState.Chasing;
        FindTarget();

        // Ensure agent is placed correctly on the NavMesh immediately
        ValidateNavMeshPosition();
    }

    private void ValidateNavMeshPosition()
    {
        // Try to find a point on the NavMesh close to current position
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            _agent.Warp(hit.position);
            _agent.enabled = true;
        }
        else
        {
            // If we can't find a spot, disable the agent to prevent error logs
            Debug.LogWarning($"Enemy spawned off NavMesh at {transform.position}");
            _agent.enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            _health.OnDeath -= HandleDeath;
        }
    }

    private void ApplyRandomStats()
    {
        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        transform.localScale = Vector3.one * randomScale;

        _agent.speed = Random.Range(speedRange.x, speedRange.y);
        _agent.acceleration = Random.Range(accelerationRange.x, accelerationRange.y);
        _agent.radius = Random.Range(radiusRange.x, radiusRange.y);
        _agent.avoidancePriority = Random.Range(0, 100);
    }

    private void FindTarget()
    {
        var player = FindAnyObjectByType<PlayerMovement>();
        if (player != null)
        {
            _target = player.transform;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        // Ensure agent is actually on the mesh before giving commands
        if (!_agent.isOnNavMesh || !_agent.isActiveAndEnabled) return;

        if (_target == null)
        {
            FindTarget();
            return;
        }

        _cooldownTimer -= Time.deltaTime;

        switch (_currentState)
        {
            case AIState.Chasing: HandleChasingState(); break;
            case AIState.Attacking: HandleAttackingState(); break;
        }
        UpdateRotation();
    }

    // --- AI LOGIC ---

    private void HandleChasingState()
    {
        float distance = Vector3.Distance(transform.position, _target.position);

        if (distance > attackRange)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_target.position);
        }
        else if (distance <= tooCloseRange)
        {
            _agent.isStopped = false;
            Vector3 retreatDirection = (transform.position - _target.position).normalized;

            // Sample position for retreat to prevent going off-mesh
            Vector3 retreatPos = transform.position + retreatDirection * 2f;
            if (NavMesh.SamplePosition(retreatPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
        }
        else
        {
            _agent.isStopped = true;
            if (_cooldownTimer <= 0f)
            {
                StartAttackSequence();
            }
        }
    }

    private void StartAttackSequence()
    {
        _currentState = AIState.Attacking;
        float randomSpeed = Random.Range(attackAnimationSpeedRange.x, attackAnimationSpeedRange.y);
        _animation.StartAttack(randomSpeed);
    }

    private void HandleAttackingState()
    {
        float distance = Vector3.Distance(transform.position, _target.position);

        // Allow some movement during attack (lunging)
        if (distance > attackRange)
        {
            float forwardSpeed = _agent.speed * attackMoveSpeedMultiplier;
            Vector3 direction = (_target.position - transform.position).normalized;

            // Use Move instead of SetDestination for manual control
            _agent.Move(direction * forwardSpeed * Time.deltaTime);
        }
    }

    private void UpdateRotation()
    {
        Vector3 lookDirection;

        if (_currentState == AIState.Chasing && _agent.velocity.magnitude > 0.1f)
        {
            lookDirection = _agent.desiredVelocity;
        }
        else
        {
            lookDirection = _target.position - transform.position;
        }

        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _agent.angularSpeed * Time.deltaTime);
        }
    }

    // --- ANIMATION EVENTS ---

    public void Hit()
    {
        if (!IsServer) return;

        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRadius, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(attackDamage);
            }
        }
    }

    public void OnAttackAnimationFinished()
    {
        if (!IsServer) return;
        _currentState = AIState.Chasing;
        _cooldownTimer = Random.Range(postAttackDelayRange.x, postAttackDelayRange.y);
    }

    public void OnAttackAnimationStarted()
    {
        if (!IsServer) return;
        _animation.EndAttack();
    }

    // --- LIFECYCLE ---

    public void Initialize(EnemySpawner spawner, Transform target)
    {
        _target = target;
    }

    private void HandleDeath()
    {
        _agent.enabled = false;
        GetComponent<Collider>().enabled = false;
        _animation.StartDeath();
        Invoke(nameof(DespawnEnemy), 5f);
    }

    private void DespawnEnemy()
    {
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}