using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using System;

public class UIDisplay : MonoBehaviour
{
    [Header("In Lobby")]
    [SerializeField] private TMP_Text usernameText;

    private void Start()
    {
        NetStore.Instance.usernames.OnListChanged += UpdateNames;
    }

    private void OnDestroy()
    {
        NetStore.Instance.usernames.OnListChanged -= UpdateNames;
    }

    public void UpdateNames(NetworkListEvent<NetString> changeEvent)
    {
        List<string> name = new();

        foreach (string s in NetStore.Instance.usernames)
        {
            name.Add(s);
        }

        string finalString = string.Join("\n", name);
        usernameText.text = finalString;
    }
}
