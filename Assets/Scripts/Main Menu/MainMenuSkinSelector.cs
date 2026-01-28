using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Manages the character preview in the Main Menu Lobby.
/// Handles mesh swapping and synchronizing the selection to the NetStore.
/// </summary>
public class MainMenuSkinSelector : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CharacterSkinRegistry skinRegistry;

    [Header("Visuals")]
    [Tooltip("The SkinnedMeshRenderer of the dummy character in the scene.")]
    [SerializeField] private SkinnedMeshRenderer previewRenderer;
    [Tooltip("The root GameObject of the dummy character (to toggle visibility).")]
    [SerializeField] private GameObject previewRoot;

    [Header("UI")]
    [SerializeField] private Button switchSkinButton;

    private int _currentIndex = 0;

    private void Start()
    {
        if (switchSkinButton != null)
        {
            switchSkinButton.onClick.AddListener(OnSwitchClicked);
        }

        if (NetStore.Instance != null)
        {
            NetStore.Instance.playerData.OnListChanged += HandlePlayerDataChanged;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
        }

        UpdatePreview();
        UpdateVisibility();
    }

    private void OnDestroy()
    {
        if (NetStore.Instance != null)
        {
            NetStore.Instance.playerData.OnListChanged -= HandlePlayerDataChanged;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
        }
    }

    private void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        // Update visibility when we connect or disconnect
        UpdateVisibility();
    }

    private void HandlePlayerDataChanged(NetworkListEvent<NetPlayerData> changeEvent)
    {
        UpdateVisibility();
    }

    /// <summary>
    /// Checks the local player's role. If God, hide the survivor preview.
    /// Also hides if not connected to a lobby.
    /// </summary>
    private void UpdateVisibility()
    {
        // 1. Default to hidden
        bool shouldShow = false;

        // 2. Only check logic if we are actually connected
        if (NetStore.Instance != null &&
            NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            ulong myId = NetworkManager.Singleton.LocalClientId;

            // Assume we show it, unless we find out we are God
            // However, we must ensure we are actually IN the list first.
            bool foundSelf = false;
            bool isGod = false;

            foreach (var data in NetStore.Instance.playerData)
            {
                if (data.clientID == myId)
                {
                    foundSelf = true;
                    if (data.role == PlayerRole.God) isGod = true;
                    break;
                }
            }

            // Show if we are in the list and NOT God
            if (foundSelf && !isGod)
            {
                shouldShow = true;
            }
        }

        // 3. Apply state
        if (previewRoot != null) previewRoot.SetActive(shouldShow);
        if (switchSkinButton != null) switchSkinButton.gameObject.SetActive(shouldShow);
    }

    private void OnSwitchClicked()
    {
        if (skinRegistry == null || skinRegistry.skins.Count == 0) return;

        _currentIndex = (_currentIndex + 1) % skinRegistry.skins.Count;

        UpdatePreview();
        SyncSelection();
    }

    private void UpdatePreview()
    {
        if (previewRenderer != null && skinRegistry != null && skinRegistry.skins.Count > 0)
        {
            // Ensure index is safe
            _currentIndex = Mathf.Clamp(_currentIndex, 0, skinRegistry.skins.Count - 1);
            previewRenderer.sharedMesh = skinRegistry.skins[_currentIndex];
        }
    }

    private void SyncSelection()
    {
        // Only sync if we are connected (Lobby state)
        if (NetworkManager.Singleton.IsListening && NetStore.Instance != null)
        {
            ulong myId = NetworkManager.Singleton.LocalClientId;
            NetStore.Instance.UpdatePlayerSkinServerRpc(myId, _currentIndex);
        }
    }
}