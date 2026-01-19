using UnityEngine;

/// <summary>
/// Handles combat logic: Finding targets, checking ranges, and dealing damage.
/// </summary>
public class EnemyCombat : MonoBehaviour
{
    [Header("AI Ranges")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float tooCloseRange = 1f;
    [SerializeField] private float targetSearchInterval = 0.5f;

    [Header("Attack Configuration")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.7f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector2 postAttackDelayRange = new Vector2(0.5f, 1.5f);
    [SerializeField] private float attackMoveSpeedMultiplier = 0.5f;

    public Transform CurrentTarget { get; private set; }
    public float AttackRange => attackRange;
    public float TooCloseRange => tooCloseRange;
    public float AttackMoveSpeedMultiplier => attackMoveSpeedMultiplier;

    private float _searchTimer;
    private float _cooldownTimer;

    // Used to check if we can attack based on cooldown
    public bool IsAttackReady => _cooldownTimer <= 0f;

    private void Update()
    {
        // Timers update continuously
        if (_searchTimer > 0) _searchTimer -= Time.deltaTime;
        if (_cooldownTimer > 0) _cooldownTimer -= Time.deltaTime;
    }

    public void UpdateTargeting()
    {
        if (_searchTimer <= 0f)
        {
            _searchTimer = targetSearchInterval;
            FindClosestTarget();
        }
    }

    public void ResetAttackCooldown()
    {
        _cooldownTimer = Random.Range(postAttackDelayRange.x, postAttackDelayRange.y);
    }

    /// <summary>
    /// Called via Animation Event on the Server.
    /// </summary>
    public void DealDamage()
    {
        if (attackPoint == null) return;

        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRadius, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(attackDamage);
            }
        }
    }

    private void FindClosestTarget()
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        float closestDistance = float.MaxValue;
        Transform newTarget = null;

        foreach (var player in players)
        {
            if (player.TryGetComponent<HealthComponent>(out var h) && h.IsDead)
                continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                newTarget = player.transform;
            }
        }

        CurrentTarget = newTarget;
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