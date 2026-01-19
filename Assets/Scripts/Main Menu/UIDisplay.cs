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
    [SerializeField]
    [Tooltip("The text component displaying the list of players.")]
    private TMP_Text usernameText;

    [Header("Interaction")]
    [SerializeField]
    [Tooltip("The button used to swap between God and Survivor roles.")]
    private Button toggleRoleButton;

    private void Start()
    {
        if (NetStore.Instance != null)
        {
            NetStore.Instance.playerData.OnListChanged += UpdateNames;
            RefreshList();
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
        if (NetworkManager.Singleton == null) return;

        ulong myId = NetworkManager.Singleton.LocalClientId;
        PlayerRole currentRole = PlayerRole.Survivor;

        foreach (var data in NetStore.Instance.playerData)
        {
            if (data.clientID == myId)
            {
                currentRole = data.role;
                break;
            }
        }

        PlayerRole newRole = (currentRole == PlayerRole.God)
            ? PlayerRole.Survivor
            : PlayerRole.God;

        NetStore.Instance.UpdatePlayerRoleServerRpc(myId, newRole);
    }

    public void UpdateNames(NetworkListEvent<NetPlayerData> changeEvent)
    {
        RefreshList();
    }

    private void RefreshList()
    {
        if (NetStore.Instance == null) return;

        List<string> lines = new();

        foreach (NetPlayerData data in NetStore.Instance.playerData)
        {
            string color = data.role == PlayerRole.God ? "red" : "green";
            string label = data.role == PlayerRole.God
                ? "(GOD)"
                : "(Survivor)";

            lines.Add($"<color={color}>{data.username} {label}</color>");
        }

        usernameText.text = string.Join("\n", lines);
    }
}