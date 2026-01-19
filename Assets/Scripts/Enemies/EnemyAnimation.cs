using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles visual animation playback for enemies.
/// Reacts to Health events and NetworkVariable changes.
/// </summary>
[RequireComponent(typeof(Animator), typeof(HealthComponent))]
public class EnemyAnimation : MonoBehaviour
{
    // --- Animator Hashes ---
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

    [Header("Locomotion")]
    [SerializeField] private float baseRunSpeed = 4.0f;
    [SerializeField] private float smoothingSpeed = 5f;

    [Header("Injury Visuals")]
    [SerializeField][Range(0, 1)] private float injuryStartThreshold = 0.6f;
    [SerializeField][Range(0, 1)] private float injuryMaxThreshold = 0.3f;

    private Animator _animator;
    private HealthComponent _health;

    // State driven by Server
    private bool _serverSaysMoving = false;
    private bool _isDead = false;
    private bool _isAttacking = false;

    // Smoothing Variables
    private Vector3 _previousPosition;
    private float _currentSmoothedSpeed = 0f;
    private float _currentForwardBlend = 0f;

    private int _injuredLayerIndex = -1;
    private int _upperBodyLayerIndex = -1;
    private int _reactionsLayerIndex = -1;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _health = GetComponent<HealthComponent>();

        _injuredLayerIndex = _animator.GetLayerIndex("Injured");
        _upperBodyLayerIndex = _animator.GetLayerIndex("UpperBody");
        _reactionsLayerIndex = _animator.GetLayerIndex("Reactions");
    }

    private void Start()
    {
        _previousPosition = transform.position;
        _animator.SetFloat(CycleOffsetParam, Random.value);
    }

    private void OnEnable()
    {
        _health.OnDamaged += HandleDamageVisuals;
        _health.OnDeath += HandleDeathVisuals;
    }

    private void OnDisable()
    {
        _health.OnDamaged -= HandleDamageVisuals;
        _health.OnDeath -= HandleDeathVisuals;
    }

    private void Update()
    {
        if (_isDead) return;
        CalculateLocomotion();
    }

    public void SetMoving(bool isMoving)
    {
        _serverSaysMoving = isMoving;
    }

    public void ToggleAttackState(bool isAttacking, int attackID, float speedMultiplier)
    {
        if (_isDead) return;
        _isAttacking = isAttacking;

        if (isAttacking)
        {
            _animator.SetBool(IsMirroredParam, Random.value > 0.5f);
            _animator.SetFloat(AttackIdParam, attackID);
            _animator.SetFloat(AttackSpeedParam, speedMultiplier);
        }

        _animator.SetBool(IsAttackingParam, isAttacking);
    }

    private void CalculateLocomotion()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;

        Vector3 displacement = transform.position - _previousPosition;
        Vector3 localVelocity = transform.InverseTransformDirection(displacement) / deltaTime;

        float targetSpeed = _serverSaysMoving ? 1.0f : 0.0f;
        _currentSmoothedSpeed = Mathf.Lerp(_currentSmoothedSpeed, targetSpeed, deltaTime * smoothingSpeed);

        float targetForward = 0f;
        if (_serverSaysMoving)
        {
            if (localVelocity.magnitude > 0.1f)
                targetForward = Mathf.Clamp(localVelocity.z, -1f, 1f);
            else
                targetForward = 1f; // Fallback assumption
        }
        _currentForwardBlend = Mathf.Lerp(_currentForwardBlend, targetForward, deltaTime * smoothingSpeed);

        _animator.SetFloat(VelocityZParam, _currentForwardBlend);

        float multiplier = 1.0f;
        if (_currentSmoothedSpeed > 0.1f)
        {
            float estimatedRealSpeed = displacement.magnitude / deltaTime;
            multiplier = Mathf.Clamp(estimatedRealSpeed / baseRunSpeed, 0.5f, 2.0f);
        }

        float currentMultiplier = _animator.GetFloat(AnimationSpeedMultiplierParam);
        _animator.SetFloat(AnimationSpeedMultiplierParam, Mathf.Lerp(currentMultiplier, multiplier, deltaTime * 5f));

        _previousPosition = transform.position;
    }

    // --- Event Handlers ---

    private void HandleDamageVisuals(float damage)
    {
        if (_isDead) return;

        // Update Injury Layer
        float healthPercent = _health.CurrentHealth.Value / 100f; // Assuming 100 max for visual scaling
        float weight = 0f;
        if (healthPercent < injuryStartThreshold)
        {
            weight = Mathf.InverseLerp(injuryStartThreshold, injuryMaxThreshold, healthPercent);
        }
        if (_injuredLayerIndex != -1) _animator.SetLayerWeight(_injuredLayerIndex, weight);

        // Play Hit Animation (only if not attacking to prevent weird blends)
        if (!_isAttacking)
        {
            int hitID = Random.Range(0, 2);
            _animator.SetBool(IsMirroredParam, Random.value > 0.5f);
            _animator.SetFloat(HitIdParam, hitID);
            _animator.SetTrigger(HitTriggerParam);
        }
    }

    private void HandleDeathVisuals()
    {
        _isDead = true;

        // Disable physics locally for visuals
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

        // Fade out layers
        if (_upperBodyLayerIndex != -1) _animator.SetLayerWeight(_upperBodyLayerIndex, 0f);
        if (_injuredLayerIndex != -1) _animator.SetLayerWeight(_injuredLayerIndex, 0f);
        if (_reactionsLayerIndex != -1) _animator.SetLayerWeight(_reactionsLayerIndex, 0f);

        int deathID = Random.Range(0, 2);
        _animator.SetBool(IsMirroredParam, Random.value > 0.5f);
        _animator.SetFloat(DeathIdParam, deathID);
        _animator.SetTrigger(DieParam);
    }
}