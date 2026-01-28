using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Synchronizes and applies the selected character mesh to the player object.
/// </summary>
public class PlayerSkinController : NetworkBehaviour
{
    [Header("Configuration")]
    [SerializeField] private CharacterSkinRegistry skinRegistry;
    [SerializeField] private SkinnedMeshRenderer targetRenderer;

    /// <summary>
    /// The index of the skin to display. Synced from Server to all Clients.
    /// </summary>
    public NetworkVariable<int> netSkinIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // Apply the initial skin immediately upon spawning
        ApplySkin(netSkinIndex.Value);

        // Listen for future changes (e.g. if we add dynamic swapping later)
        netSkinIndex.OnValueChanged += OnSkinChanged;
    }

    public override void OnNetworkDespawn()
    {
        netSkinIndex.OnValueChanged -= OnSkinChanged;
    }

    /// <summary>
    /// Called on the Server by the GameConnectionManager immediately after spawning.
    /// </summary>
    public void SetSkin(int index)
    {
        if (IsServer)
        {
            netSkinIndex.Value = index;
        }
    }

    private void OnSkinChanged(int previous, int current)
    {
        ApplySkin(current);
    }

    private void ApplySkin(int index)
    {
        if (skinRegistry == null || targetRenderer == null) return;
        if (skinRegistry.skins.Count == 0) return;

        // Safety clamp
        int safeIndex = Mathf.Clamp(index, 0, skinRegistry.skins.Count - 1);

        targetRenderer.sharedMesh = skinRegistry.skins[safeIndex];
    }
}