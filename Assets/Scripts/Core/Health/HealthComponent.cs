using System;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Manages networked health state, damage processing, and death events.
/// </summary>
public class HealthComponent : NetworkBehaviour, IDamageable
{
    public event Action OnDeath;
    public event Action<float> OnDamaged;

    [Header("Configuration")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("The maximum health capacity for this entity.")]
    private float maxHealth = 100f;

    /// <summary>
    /// Returns the configured maximum health.
    /// </summary>
    public float MaxHealth => maxHealth;

    public NetworkVariable<float> CurrentHealth =
        new NetworkVariable<float>(100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public bool IsDead => CurrentHealth.Value <= 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHealth.Value = maxHealth;
        }

        CurrentHealth.OnValueChanged += HandleHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float previousValue, float newValue)
    {
        if (newValue < previousValue)
        {
            float damageTaken = previousValue - newValue;
            OnDamaged?.Invoke(damageTaken);
        }

        if (newValue <= 0 && previousValue > 0)
        {
            OnDeath?.Invoke();
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsServer)
        {
            ApplyDamage(amount);
        }
        else
        {
            SubmitDamageRequestServerRpc(amount);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitDamageRequestServerRpc(float amount)
    {
        ApplyDamage(amount);
    }

    private void ApplyDamage(float amount)
    {
        if (IsDead) return;
        CurrentHealth.Value = Mathf.Max(CurrentHealth.Value - amount, 0);
    }
}