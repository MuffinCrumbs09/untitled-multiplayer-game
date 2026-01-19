using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handles physical movement, pathfinding, and rotation for the enemy.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Randomization Settings")]
    [SerializeField] private Vector2 scaleRange = new Vector2(0.9f, 1.1f);
    [SerializeField] private Vector2 speedRange = new Vector2(3.0f, 4.5f);
    [SerializeField] private Vector2 accelerationRange = new Vector2(6.0f, 10.0f);
    [SerializeField] private Vector2 radiusRange = new Vector2(0.3f, 0.4f);

    [Header("Physics")]
    [Tooltip("Layers to snap to when dying to prevent floating.")]
    [SerializeField] private LayerMask groundLayer;

    private NavMeshAgent _agent;

    public bool IsMoving => _agent.enabled && _agent.velocity.sqrMagnitude > 0.1f;
    public float AngularSpeed => _agent.angularSpeed;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.stoppingDistance = 0;
        _agent.updateRotation = false;
    }

    public void InitializeServer()
    {
        ApplyRandomStats();
        ValidateNavMeshPosition();
    }

    public void DisableMovement()
    {
        if (_agent.isActiveAndEnabled)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }
    }

    public void MoveTo(Vector3 position)
    {
        if (!_agent.isActiveAndEnabled) return;

        _agent.isStopped = false;
        _agent.SetDestination(position);
    }

    public void Stop()
    {
        if (!_agent.isActiveAndEnabled) return;
        _agent.isStopped = true;
    }

    public void RotateTowards(Vector3 targetPosition)
    {
        Vector3 lookDirection;

        if (IsMoving)
        {
            lookDirection = _agent.desiredVelocity;
        }
        else
        {
            lookDirection = targetPosition - transform.position;
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

    public void SnapToGround()
    {
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down,
            out RaycastHit hit, 5f, groundLayer))
        {
            transform.position = hit.point;
        }
    }

    private void ValidateNavMeshPosition()
    {
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit,
            2.0f, NavMesh.AllAreas))
        {
            _agent.Warp(hit.position);
            _agent.enabled = true;
        }
        else
        {
            Debug.LogWarning($"Enemy spawned off NavMesh at {transform.position}");
            _agent.enabled = false;
        }
    }

    private void ApplyRandomStats()
    {
        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        transform.localScale = Vector3.one * randomScale;

        _agent.speed = Random.Range(speedRange.x, speedRange.y);
        _agent.acceleration = Random.Range(accelerationRange.x,
            accelerationRange.y);
        _agent.radius = Random.Range(radiusRange.x, radiusRange.y);
        _agent.avoidancePriority = Random.Range(0, 100);
    }
}