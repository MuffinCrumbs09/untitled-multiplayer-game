using UnityEngine;
using Unity.Netcode;

public class Plank : NetworkBehaviour, IInteractable, IPopUp
{
    public NetworkVariable<bool> Searched = new(false);
    public InteractUI interactUI;

    public bool CanInteract()
    {
        return Searched.Value == false;
    }

    public void Interact()
    {
        InteractServerRpc();
        StartCoroutine(SearchGrave());
    }

    public void ToggleUI(bool toggle)
    {
        if (toggle)
            interactUI.Show();
        else
            interactUI.Hide();
    }

    private System.Collections.IEnumerator SearchGrave()
    {
        InputManager.Instance.ToggleMovement(false);
        GameObject local = null;

        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            if (player.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.Singleton.LocalClientId)
                local = player;

        local.GetComponent<Animator>().SetTrigger("SearchLow");
        yield return new WaitForSeconds(5f);
        local.GetComponent<Animator>().SetTrigger("Stop");

        InputManager.Instance.ToggleMovement(true);

        GameLoopManager.Instance.PuzzleManager.CollectPlankServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void InteractServerRpc()
    {
        Searched.Value = true;
        transform.parent.gameObject.GetComponent<Collider>().enabled = false;
        transform.parent.gameObject.GetComponent<MeshRenderer>().enabled = false;
        InteractClientRpc();
    }

    [Rpc(SendTo.NotServer)]
    private void InteractClientRpc()
    {
        transform.parent.gameObject.GetComponent<Collider>().enabled = false;
        transform.parent.gameObject.GetComponent<MeshRenderer>().enabled = false;
    }
}