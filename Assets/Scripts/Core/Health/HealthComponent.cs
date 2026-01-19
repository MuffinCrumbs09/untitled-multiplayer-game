using System;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Manages networked health state, damage processing, and death events.
/// Refactored to be reactive: Events fire on all clients based on NetworkVariable changes.
/// </summary>
public class HealthComponent : NetworkBehaviour, IDamageable
{
    /// <summary>
    /// Fired when the health value reaches zero.
    /// Runs on Server and Clients.
    /// </summary>
    public event Action OnDeath;

    /// <summary>
    /// Fired when damage is applied, providing the amount taken.
    /// Runs on Server and Clients.
    /// </summary>
    public event Action<float> OnDamaged;

    [Header("Configuration")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("The maximum health capacity for this entity.")]
    private float maxHealth = 100f;

    /// <summary>
    /// The current health value, synchronized from the server.
    /// </summary>
    public NetworkVariable<float> CurrentHealth =
        new NetworkVariable<float>(100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    /// <summary>
    /// Returns true if the current health is zero or less.
    /// </summary>
    public bool IsDead => CurrentHealth.Value <= 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHealth.Value = maxHealth;
        }

        // Subscribe to changes on both Server and Client to drive visuals/events
        CurrentHealth.OnValueChanged += HandleHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
    }

    /// <summary>
    /// Reacts to changes in the NetworkVariable.
    /// This runs on all clients when the Server modifies CurrentHealth.
    /// </summary>
    private void HandleHealthChanged(float previousValue, float newValue)
    {
        // 1. Check for Damage
        if (newValue < previousValue)
        {
            float damageTaken = previousValue - newValue;
            OnDamaged?.Invoke(damageTaken);
        }

        // 2. Check for Death (Edge trigger)
        if (newValue <= 0 && previousValue > 0)
        {
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// Processes a request to reduce health.
    /// </summary>
    /// <param name="amount">The raw damage value to apply.</param>
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

        // Modifying this value will trigger HandleHealthChanged on all clients automatically.
        CurrentHealth.Value = Mathf.Max(CurrentHealth.Value - amount, 0);
    }
}