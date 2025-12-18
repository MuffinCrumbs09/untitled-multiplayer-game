using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class UIDisplay : MonoBehaviour
{
    [Header("In Lobby")]
    [SerializeField] private TMP_Text usernameText;

    private void Start()
    {
        NetStore.Instance.playerData.OnListChanged += UpdateNames;
    }

    private void OnDestroy()
    {
        NetStore.Instance.playerData.OnListChanged -= UpdateNames;
    }

    public void UpdateNames(NetworkListEvent<NetPlayerData> changeEvent)
    {
        List<string> name = new();

        foreach (NetPlayerData data in NetStore.Instance.playerData)
        {
            name.Add(data.username);
            Debug.Log(data.username + " " + data.isSurvivor);
        }

        string finalString = string.Join("\n", name);
        usernameText.text = finalString;
    }
}
