using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles a temporary area that deals damage over time to survivors.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PoisonArea : NetworkBehaviour
{
    [Header("Stats")]
    [Tooltip("Damage applied per tick.")]
    [SerializeField] private float damagePerTick = 5f;

    [Tooltip("Time in seconds between damage ticks.")]
    [SerializeField] private float tickInterval = 1.0f;

    [Tooltip("How long the poison cloud lasts before disappearing.")]
    [SerializeField] private float lifetime = 10f;

    [Tooltip("The radius of the damage area.")]
    [SerializeField] private float radius = 3f;

    [Header("Targeting")]
    [Tooltip("Layers to check for damageable entities (usually Player).")]
    [SerializeField] private LayerMask targetLayers;

    public override void OnNetworkSpawn()
    {
        // Damage logic only runs on the server
        if (IsServer)
        {
            StartCoroutine(PoisonRoutine());
        }
    }

    private IEnumerator PoisonRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < lifetime)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsedTime += tickInterval;

            PulseDamage();
        }

        // Destroy the object across the network
        GetComponent<NetworkObject>().Despawn();
    }

    private void PulseDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayers);

        foreach (Collider hit in hits)
        {
            // Check for IDamageable interface (implemented by HealthComponent)
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                // HealthComponent handles the RPCs, so we just call TakeDamage on the server
                damageable.TakeDamage(damagePerTick);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }
}