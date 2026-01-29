using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Controls a world-space health bar that faces the camera.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    [Tooltip("Reference to the health component to track.")]
    private HealthComponent healthComponent;

    [SerializeField]
    [Tooltip("The slider UI element representing the health bar.")]
    private Slider healthSlider;

    private Camera _mainCamera;

    private void Start()
    {
        if (healthComponent == null)
            healthComponent = GetComponentInParent<HealthComponent>();

        if (healthComponent != null)
        {
            // Set initial values
            healthSlider.maxValue = healthComponent.MaxHealth;
            healthSlider.value = healthComponent.CurrentHealth.Value;

            // Subscribe to updates
            healthComponent.CurrentHealth.OnValueChanged += HandleHealthChanged;
        }

        _mainCamera = Camera.main;
    }

    private void OnDestroy()
    {
        if (healthComponent != null)
        {
            healthComponent.CurrentHealth.OnValueChanged -= HandleHealthChanged;
        }
    }

    private void LateUpdate()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            return;
        }

        // Billboarding: Face the camera while maintaining upright orientation
        transform.rotation = _mainCamera.transform.rotation;
    }

    private void HandleHealthChanged(float previous, float current)
    {
        healthSlider.value = current;
    }
}