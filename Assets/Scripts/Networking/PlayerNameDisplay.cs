using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;

/// <summary>
/// Manages the networked name display for a player character.
/// </summary>
public class PlayerNameDisplay : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text nameText;

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);

    // This variable automatically syncs the name to everyone
    public NetworkVariable<FixedString64Bytes> networkName = new NetworkVariable<FixedString64Bytes>();

    private Camera _mainCamera;

    public override void OnNetworkSpawn()
    {
        // 1. Find Camera for billboarding
        _mainCamera = Camera.main;

        // 2. If Server, set the name
        if (IsServer)
        {
            // Use 1-based indexing for the fallback name to match RelayManager.
            string foundName = $"Player {OwnerClientId + 1}";

            if (NetStore.Instance != null)
            {
                foreach (var data in NetStore.Instance.playerData)
                {
                    if (data.clientID == OwnerClientId)
                    {
                        foundName = data.username.ToString();
                        break;
                    }
                }
            }

            networkName.Value = foundName;
        }

        // 3. Update UI immediately
        nameText.text = networkName.Value.ToString();

        // 4. Listen for changes (in case name changes later)
        networkName.OnValueChanged += OnNameChanged;
    }

    public override void OnNetworkDespawn()
    {
        networkName.OnValueChanged -= OnNameChanged;
    }

    private void OnNameChanged(FixedString64Bytes oldName, FixedString64Bytes newName)
    {
        nameText.text = newName.ToString();
    }

    private void LateUpdate()
    {
        // Face Camera (Billboarding)
        if (_mainCamera != null)
        {
            // Look at the camera, but flip it so text isn't backwards
            transform.rotation = Quaternion.LookRotation(transform.position - _mainCamera.transform.position);
        }
    }
}