using Unity.Netcode;
using UnityEngine;

public enum ItemType
{
    Bone,
    Fire,
    Jewel,
    Maze,
    Alter
}

public class Alter : NetworkBehaviour, IInteractable, IPopUp
{
    [Header("Settings")]
    public NetworkVariable<bool> Activated = new(false);
    public InteractUI interactUI;
    public GameObject itemToPlace;
    public ItemType Itemt;

    private void Update()
    {
        if(Activated.Value && itemToPlace.activeSelf == false)
        {
            itemToPlace.SetActive(true);
        }
    }

    public bool CanInteract()
    {
        bool hasItem = false;

        switch(Itemt)
        {
            case ItemType.Bone:
                hasItem = GameLoopManager.Instance.PuzzleManager.BoneCollected.Value;
                break;
            case ItemType.Fire:
                hasItem = GameLoopManager.Instance.PuzzleManager.FireStoked.Value;
                break;
            case ItemType.Jewel:
                hasItem = GameLoopManager.Instance.PuzzleManager.StatuesDestroyed.Value;
                break;
            case ItemType.Maze:
                hasItem = GameLoopManager.Instance.PuzzleManager.MazeComplete.Value;
                break;
        }

        return Activated.Value == false && hasItem;
    }

    public void Interact()
    {
        StartCoroutine(PlaceItem());
    }

    public void ToggleUI(bool toggle)
    {
        if (toggle)
            interactUI.Show();
        else
            interactUI.Hide();
    }

    private System.Collections.IEnumerator PlaceItem()
    {
        InputManager.Instance.ToggleMovement(false);
        GameObject local = null;

        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            if (player.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.Singleton.LocalClientId)
                local = player;

        local.GetComponent<Animator>().SetTrigger("SearchLow");
        yield return new WaitForSeconds(1.5f);
        local.GetComponent<Animator>().SetTrigger("Stop");

        InputManager.Instance.ToggleMovement(true);

        InteractServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void InteractServerRpc()
    {
        Activated.Value = true;
    }
}
