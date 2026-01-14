using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages the Lobby UI list and Role switching interactions.
/// </summary>
public class UIDisplay : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("The text component displaying the list of players.")]
    [SerializeField] private TMP_Text usernameText;

    [Header("Interaction")]
    [Tooltip("The button used to swap between God and Survivor roles.")]
    [SerializeField] private Button toggleRoleButton;

    private void Start()
    {
        if (NetStore.Instance != null)
        {
            NetStore.Instance.playerData.OnListChanged += UpdateNames;
        }

        if (toggleRoleButton != null)
        {
            toggleRoleButton.onClick.AddListener(ToggleMyRole);
        }
    }

    private void OnDestroy()
    {
        if (NetStore.Instance != null)
        {
            NetStore.Instance.playerData.OnListChanged -= UpdateNames;
        }
    }

    private void ToggleMyRole()
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;
        PlayerRole currentRole = PlayerRole.Survivor;

        // Find current role
        foreach (var data in NetStore.Instance.playerData)
        {
            if (data.clientID == myId)
            {
                currentRole = data.role;
                break;
            }
        }

        // Swap Logic
        PlayerRole newRole = (currentRole == PlayerRole.God)
            ? PlayerRole.Survivor
            : PlayerRole.God;

        NetStore.Instance.UpdatePlayerRoleServerRpc(myId, newRole);
    }

    public void UpdateNames(NetworkListEvent<NetPlayerData> changeEvent)
    {
        List<string> lines = new();

        foreach (NetPlayerData data in NetStore.Instance.playerData)
        {
            // Simple color coding for roles
            string color = data.role == PlayerRole.God ? "red" : "green";
            string label = data.role == PlayerRole.God
                ? "(GOD)"
                : "(Survivor)";

            lines.Add($"<color={color}>{data.username} {label}</color>");
        }

        usernameText.text = string.Join("\n", lines);
    }
}