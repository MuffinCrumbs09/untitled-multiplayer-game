using UnityEngine;
using Unity.Netcode;

public class Idol : NetworkBehaviour, IInteractable, IPopUp
{
    public InteractUI interactUI;
    public bool CanInteract()
    {
        return GameLoopManager.Instance.PuzzleManager.MazeComplete.Value == false;
    }

    public void Interact()
    {
        InteractServerRpc();
    }

    public void ToggleUI(bool toggle)
    {
        if (toggle)
            interactUI.Show();
        else
            interactUI.Hide();
    }

    [Rpc(SendTo.Server)]
    private void InteractServerRpc()
    {
        GameLoopManager.Instance.PuzzleManager.MazeComplete.Value = true;
        transform.gameObject.GetComponent<Collider>().enabled = false;
        transform.gameObject.GetComponent<MeshRenderer>().enabled = false;
        InteractClientRpc();
    }

    [Rpc(SendTo.NotServer)]
    private void InteractClientRpc()
    {
        transform.gameObject.GetComponent<Collider>().enabled = false;
        transform.gameObject.GetComponent<MeshRenderer>().enabled = false;
    }

}
