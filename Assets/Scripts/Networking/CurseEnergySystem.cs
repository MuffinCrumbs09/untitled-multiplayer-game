using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class CurseEnergySystem : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float regenRate = 5f; // Energy per second

    [Header("UI References")]
    [SerializeField] private Slider energySlider;
    [SerializeField] private TMP_Text energyText;

    // Server-Authoritative Energy Value
    public NetworkVariable<float> CurrentEnergy = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentEnergy.Value = 50f; // Start with some energy
        }

        // Listen for changes to update UI
        CurrentEnergy.OnValueChanged += UpdateUI;

        // Initial UI set
        UpdateUI(0, CurrentEnergy.Value);
    }

    public override void OnNetworkDespawn()
    {
        CurrentEnergy.OnValueChanged -= UpdateUI;
    }

    private void Update()
    {
        // Only Server calculates Regen
        if (!IsServer) return;

        if (CurrentEnergy.Value < maxEnergy)
        {
            CurrentEnergy.Value += regenRate * Time.deltaTime;
            if (CurrentEnergy.Value > maxEnergy) CurrentEnergy.Value = maxEnergy;
        }
    }

    /// <summary>
    /// Attempts to spend energy. Returns true if successful.
    /// SERVER ONLY.
    /// </summary>
    public bool TryConsumeEnergy(float amount)
    {
        if (!IsServer) return false;

        if (CurrentEnergy.Value >= amount)
        {
            CurrentEnergy.Value -= amount;
            return true;
        }

        return false;
    }

    private void UpdateUI(float previousValue, float newValue)
    {
        // Only update UI for the owner (The God Player)
        if (!IsOwner) return;

        if (energySlider != null)
        {
            energySlider.maxValue = maxEnergy;
            energySlider.value = newValue;
        }

        if (energyText != null)
        {
            energyText.text = $"{Mathf.FloorToInt(newValue)} / {maxEnergy}";
        }
    }
}