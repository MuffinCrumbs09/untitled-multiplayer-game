using System;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Manages networked health state, damage processing, and death events.
/// </summary>
public class HealthComponent : NetworkBehaviour, IDamageable
{
    /// <summary>
    /// Fired when the health value reaches zero.
    /// </summary>
    public event Action OnDeath;

    /// <summary>
    /// Fired when damage is applied, providing the amount taken.
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

    [Rpc(SendTo.Server,
         InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitDamageRequestServerRpc(float amount)
    {
        ApplyDamage(amount);
    }

    private void ApplyDamage(float amount)
    {
        if (IsDead) return;

        CurrentHealth.Value -= amount;

        // Notify local listeners for UI or VFX feedback.
        OnDamaged?.Invoke(amount);

        if (CurrentHealth.Value <= 0)
        {
            CurrentHealth.Value = 0;
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();
    }
}