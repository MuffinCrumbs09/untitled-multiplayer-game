using UnityEngine;
using Unity.Netcode;

public class Grave : NetworkBehaviour, IInteractable, IPopUp
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField][Range(0f, 1f)] private float enemySpawnChance = 0.3f;

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
        FinishServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void InteractServerRpc()
    {
        Searched.Value = true;
    }

    [Rpc(SendTo.Server)]
    private void FinishServerRpc()
    {
        if (GameLoopManager.Instance.PuzzleManager.IsCorrectGrave(this))
        {
            Debug.Log("Found the correct grave!");
            GameLoopManager.Instance.PuzzleManager.BoneCollected.Value = true;
        }
        else
        {
            // Chance to spawn an enemy from the grave
            if (Random.value < enemySpawnChance)
            {
                SpawnEnemyServerRpc();
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnEnemyServerRpc()
    {
        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        if (enemy.TryGetComponent<NetworkObject>(out var networkObject))
        {
            networkObject.Spawn();
        }
    }

}