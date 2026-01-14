using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

[RequireComponent(typeof(Animator), typeof(NavMeshAgent))]
public class EnemyAnimation : NetworkBehaviour
{
    private static readonly int VelocityZParam = Animator.StringToHash("VelocityZ");
    private static readonly int AnimationSpeedMultiplierParam = Animator.StringToHash("AnimationSpeedMultiplier");

    private static readonly int IsAttackingParam = Animator.StringToHash("IsAttacking");
    private static readonly int DieParam = Animator.StringToHash("Die");
    private static readonly int HitTriggerParam = Animator.StringToHash("Hit");

    private static readonly int AttackIdParam = Animator.StringToHash("AttackID");
    private static readonly int DeathIdParam = Animator.StringToHash("DeathID");
    private static readonly int HitIdParam = Animator.StringToHash("HitID");
    private static readonly int AttackSpeedParam = Animator.StringToHash("AttackSpeed");
    private static readonly int CycleOffsetParam = Animator.StringToHash("CycleOffset");
    private static readonly int IsMirroredParam = Animator.StringToHash("IsMirrored");

    private static readonly int AttacksTagHash = Animator.StringToHash("Attacks");

    [Header("Locomotion")]
    [SerializeField] private float baseRunSpeed = 4.0f;

    [Header("Animation Counts")]
    [SerializeField][Min(1)] private int attackAnimationCount = 2;
    [SerializeField][Min(1)] private int deathAnimationCount = 2;
    [SerializeField][Min(1)] private int hitReactionCount = 2;

    [Header("Injury Visuals")]
    [SerializeField][Range(0, 1)] private float injuryStartThreshold = 0.6f;
    [SerializeField][Range(0, 1)] private float injuryMaxThreshold = 0.3f;

    private Animator _animator;
    private NavMeshAgent _agent;

    private int _injuredLayerIndex = -1;
    private int _upperBodyLayerIndex = -1;
    private int _reactionsLayerIndex = -1;

    private bool _isDead = false;
    private float _manualMoveSpeed = -1f;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();

        _injuredLayerIndex = _animator.GetLayerIndex("Injured");
        _upperBodyLayerIndex = _animator.GetLayerIndex("UpperBody");
        _reactionsLayerIndex = _animator.GetLayerIndex("Reactions");
    }

    private void Start()
    {
        // Only the server should drive the random cycle offset
        if (IsServer)
        {
            _animator.SetFloat(CycleOffsetParam, Random.value);
        }
    }

    private void Update()
    {
        // Only the Server calculates animation parameters based on NavMesh.
        // The Client just receives the result via NetworkAnimator.
        if (!IsServer) return;

        if (_isDead) return;

        float currentSpeed;
        float forwardSpeed = 0f;

        if (_manualMoveSpeed >= 0f)
        {
            currentSpeed = _manualMoveSpeed;
            forwardSpeed = 1.0f;
            _manualMoveSpeed = -1f;
        }
        else
        {
            Vector3 localVelocity = transform.InverseTransformDirection(_agent.velocity);
            currentSpeed = _agent.velocity.magnitude;

            if (currentSpeed > 0.1f)
            {
                forwardSpeed = localVelocity.z / currentSpeed;
            }
        }

        _animator.SetFloat(VelocityZParam, forwardSpeed, 0.1f, Time.deltaTime);

        float multiplier = 1.0f;
        if (currentSpeed > 0.1f)
        {
            multiplier = currentSpeed / baseRunSpeed;
            if (multiplier < 0.5f) multiplier = 0.5f;
        }
        _animator.SetFloat(AnimationSpeedMultiplierParam, multiplier);
    }

    public void SetManualMovement(float speed)
    {
        _manualMoveSpeed = speed;
    }

    public void UpdateHealthStatus(float currentHealth, float maxHealth)
    {
        if (!IsServer) return;
        if (_injuredLayerIndex == -1 || _isDead) return;

        float healthPercent = currentHealth / maxHealth;
        float weight = 0f;

        if (healthPercent < injuryStartThreshold)
        {
            weight = Mathf.InverseLerp(injuryStartThreshold, injuryMaxThreshold, healthPercent);
        }

        _animator.SetLayerWeight(_injuredLayerIndex, weight);
    }

    public void PlayHitReaction()
    {
        if (!IsServer) return;
        if (_isDead) return;

        bool mirror = Random.value > 0.5f;
        _animator.SetBool(IsMirroredParam, mirror);

        int hitIndex = Random.Range(0, hitReactionCount);
        _animator.SetFloat(HitIdParam, hitIndex);
        _animator.SetTrigger(HitTriggerParam);
        _animator.SetBool(IsAttackingParam, false);
    }

    public void StartAttack(float speedMultiplier)
    {
        if (!IsServer) return;
        if (_isDead) return;

        if (!IsInAttackState())
        {
            int attackIndex = Random.Range(0, attackAnimationCount);
            _animator.SetFloat(AttackIdParam, attackIndex);
        }

        bool mirror = Random.value > 0.5f;
        _animator.SetBool(IsMirroredParam, mirror);

        _animator.SetFloat(AttackSpeedParam, speedMultiplier);
        _animator.SetBool(IsAttackingParam, true);
    }

    public void EndAttack()
    {
        if (!IsServer) return;
        _animator.SetBool(IsAttackingParam, false);
    }

    public void StartDeath()
    {
        if (!IsServer) return;
        _isDead = true;

        if (_upperBodyLayerIndex != -1) _animator.SetLayerWeight(_upperBodyLayerIndex, 0f);
        if (_injuredLayerIndex != -1) _animator.SetLayerWeight(_injuredLayerIndex, 0f);
        if (_reactionsLayerIndex != -1) _animator.SetLayerWeight(_reactionsLayerIndex, 0f);

        int deathIndex = Random.Range(0, deathAnimationCount);
        _animator.SetFloat(DeathIdParam, deathIndex);

        bool mirror = Random.value > 0.5f;
        _animator.SetBool(IsMirroredParam, mirror);

        _animator.SetTrigger(DieParam);
    }

    public bool IsInAttackState()
    {
        if (_upperBodyLayerIndex == -1) return false;

        AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(_upperBodyLayerIndex);
        AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(_upperBodyLayerIndex);
        return current.tagHash == AttacksTagHash || next.tagHash == AttacksTagHash;
    }

    public Animator GetAnimator() => _animator;
}