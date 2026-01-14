using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;

/// <summary>
/// Displays the player's name above their head in world space.
/// </summary>
public class PlayerNametag : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.2f, 0);

    private Camera _mainCamera;

    public override void OnNetworkSpawn()
    {
        _mainCamera = Camera.main;

        // 1. Try to set the name immediately
        UpdateName();

        // 2. If the name list updates later, try again (Fixes race conditions)
        if (NetStore.Instance != null)
        {
            NetStore.Instance.playerData.OnListChanged += HandleListChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetStore.Instance != null)
        {
            NetStore.Instance.playerData.OnListChanged -= HandleListChanged;
        }
    }

    private void HandleListChanged(NetworkListEvent<NetPlayerData> changeEvent)
    {
        UpdateName();
    }

    private void UpdateName()
    {
        if (NetStore.Instance == null) return;

        // Use 1-based indexing for the fallback name to match RelayManager.
        string finalName = $"Player {OwnerClientId + 1}";

        // Search for matching ID
        foreach (var data in NetStore.Instance.playerData)
        {
            if (data.clientID == OwnerClientId)
            {
                finalName = data.username.ToString();
                break;
            }
        }

        nameText.text = finalName;
    }

    private void LateUpdate()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        transform.position = transform.parent.position + offset;
        transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                         _mainCamera.transform.rotation * Vector3.up);
    }
}