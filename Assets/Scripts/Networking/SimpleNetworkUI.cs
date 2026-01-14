using UnityEngine;
using Unity.Netcode;

public class SimpleNetworkUI : MonoBehaviour
{
    private void OnGUI()
    {
        // Safety check: Don't draw UI if NetworkManager isn't ready
        if (NetworkManager.Singleton == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Start Host (God)", GUILayout.Height(40)))
            {
                NetworkManager.Singleton.StartHost();
            }

            if (GUILayout.Button("Start Client (Survivor)", GUILayout.Height(40)))
            {
                NetworkManager.Singleton.StartClient();
            }
        }
        else
        {
            GUILayout.Label($"Mode: {(NetworkManager.Singleton.IsHost ? "Host" : "Client")}");

            if (GUILayout.Button("Disconnect", GUILayout.Height(40)))
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        GUILayout.EndArea();
    }
}