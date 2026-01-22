using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TaskUI : MonoBehaviour
{
    public ItemType itemType;
    public string Task;
    public TMP_Text taskText;

    private bool playerInTrigger = false;

    private void Start()
    {
        if (itemType == ItemType.Fire)
        {
            GameLoopManager.Instance.PuzzleManager.PlanksCollected.OnValueChanged += UpdateFireTask;
        }
    }

    private void OnDestroy()
    {
        if (itemType == ItemType.Fire && GameLoopManager.Instance?.PuzzleManager != null)
        {
            GameLoopManager.Instance.PuzzleManager.PlanksCollected.OnValueChanged -= UpdateFireTask;
        }
    }

    private void UpdateFireTask(int previousValue, int newValue)
    {
        if (playerInTrigger)
        {
            taskText.text = string.Format("{0}: {1}/7", Task, newValue);

            bool finishedTask = GameLoopManager.Instance.PuzzleManager.FireStoked.Value;
            if (finishedTask)
            {
                taskText.text = "<s>" + taskText.text + "</s>";
                taskText.color = Color.green;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameObject local = null;

            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
                if (player.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.Singleton.LocalClientId)
                    local = player;

            if (other.gameObject == local)
            {
                playerInTrigger = true;
                if (itemType == ItemType.Fire)
                    taskText.text = string.Format("{0}: {1}/7", Task, GameLoopManager.Instance.PuzzleManager.PlanksCollected.Value);
                else
                    taskText.text = Task;

                bool finishedTask = false;
                switch (itemType)
                {
                    case ItemType.Bone:
                        finishedTask = GameLoopManager.Instance.PuzzleManager.BoneCollected.Value;
                        break;
                    case ItemType.Fire:
                        finishedTask = GameLoopManager.Instance.PuzzleManager.FireStoked.Value;
                        break;
                    case ItemType.Jewel:
                        finishedTask = GameLoopManager.Instance.PuzzleManager.StatuesDestroyed.Value;
                        break;
                }

                if (finishedTask)
                {
                    taskText.text = "<s>" + taskText.text + "</s>";
                    taskText.color = Color.green;
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameObject local = null;

            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
                if (player.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.Singleton.LocalClientId)
                    local = player;

            if (other.gameObject == local)
            {
                playerInTrigger = false;
                taskText.text = "";
            }
        }
    }
}
