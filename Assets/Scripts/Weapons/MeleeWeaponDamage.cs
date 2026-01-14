using UnityEngine;

/// <summary>
/// Handles collision detection for the weapon and stores grip offsets.
/// </summary>
[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class MeleeWeaponDamage : MonoBehaviour
{
    [Header("Grip Adjustment")]
    [Tooltip("Local position offset for this specific weapon.")]
    public Vector3 gripPositionOffset;
    [Tooltip("Local rotation offset for this specific weapon.")]
    public Vector3 gripRotationOffset;

    [Header("Combat Stats")]
    [SerializeField] private float damage = 35f;
    [SerializeField] private string ignoreTag = "Player";

    private PlayerWeaponHandler _handler;

    public void Initialize(PlayerWeaponHandler handler)
    {
        _handler = handler;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_handler == null) return;
        if (other.CompareTag(ignoreTag) || other.isTrigger) return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            if (_handler.CanDamageTarget(damageable))
            {
                damageable.TakeDamage(damage);
                Debug.Log($"Hit {other.name} for {damage} damage!");
            }
        }
    }
}