using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the player's weapon and tracks attack state.
/// </summary>
public class PlayerWeaponHandler : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private GameObject defaultWeaponPrefab;

    private GameObject _currentWeaponInstance;
    private Animator _animator;

    private HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
    private bool _wasAttackingLastFrame = false;

    private static readonly int AttackStateHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (weaponSocket == null)
        {
            Debug.LogError("WeaponSocket is not assigned!", this);
            return;
        }

        if (defaultWeaponPrefab != null)
        {
            EquipWeapon(defaultWeaponPrefab);
        }
    }

    private void Update()
    {
        bool isAttacking = IsAttacking();

        if (isAttacking && !_wasAttackingLastFrame)
        {
            _hitTargets.Clear();
        }

        _wasAttackingLastFrame = isAttacking;
    }

    public bool IsAttacking()
    {
        if (_animator == null) return false;
        return _animator.GetCurrentAnimatorStateInfo(0).tagHash == AttackStateHash;
    }

    public bool CanDamageTarget(IDamageable target)
    {
        if (!IsAttacking()) return false;

        if (!_hitTargets.Contains(target))
        {
            _hitTargets.Add(target);
            return true;
        }

        return false;
    }

    public void EquipWeapon(GameObject weaponPrefab)
    {
        if (_currentWeaponInstance != null) Destroy(_currentWeaponInstance);
        if (weaponPrefab == null) return;

        _currentWeaponInstance = Instantiate(weaponPrefab, weaponSocket);

        // Check if the weapon has the damage script (which now holds the offsets)
        var weaponScript = _currentWeaponInstance.GetComponent<MeleeWeaponDamage>();
        if (weaponScript != null)
        {
            // Apply the specific offsets defined on this weapon prefab
            _currentWeaponInstance.transform.localPosition = weaponScript.gripPositionOffset;
            _currentWeaponInstance.transform.localRotation = Quaternion.Euler(weaponScript.gripRotationOffset);

            weaponScript.Initialize(this);
        }
        else
        {
            // Fallback if script is missing: reset to zero
            _currentWeaponInstance.transform.localPosition = Vector3.zero;
            _currentWeaponInstance.transform.localRotation = Quaternion.identity;
        }
    }
}