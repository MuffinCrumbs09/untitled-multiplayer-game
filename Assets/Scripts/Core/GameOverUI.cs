using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using Unity.Collections;

/// <summary>
/// Controls the Game Over overlay. Listens to the GameLoopManager state
/// to display the winner and provide a return button for the Host.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField]
    [Tooltip("The root panel containing the game over UI elements.")]
    private GameObject panel;

    [SerializeField]
    [Tooltip("Text component to display the winner message.")]
    private TMP_Text winnerText;

    [SerializeField]
    [Tooltip("Button to return to lobby. Only active for the Host.")]
    private Button returnButton;

    private void Start()
    {
        panel.SetActive(false);

        if (returnButton != null)
        {
            returnButton.onClick.AddListener(OnReturnClicked);
        }

        if (GameLoopManager.Instance != null)
        {
            SubscribeToManager();
        }
        else
        {
            // Retry if execution order caused manager to be null
            Invoke(nameof(SubscribeToManager), 0.5f);
        }
    }

    private void OnDestroy()
    {
        if (GameLoopManager.Instance != null)
        {
            GameLoopManager.Instance.IsGameOver.OnValueChanged -=
                OnGameOverChanged;
            GameLoopManager.Instance.WinnerMessage.OnValueChanged -=
                OnWinnerMessageChanged;
        }
    }

    private void SubscribeToManager()
    {
        if (GameLoopManager.Instance == null) return;

        GameLoopManager.Instance.IsGameOver.OnValueChanged +=
            OnGameOverChanged;
        GameLoopManager.Instance.WinnerMessage.OnValueChanged +=
            OnWinnerMessageChanged;

        if (GameLoopManager.Instance.IsGameOver.Value)
        {
            ShowUI(GameLoopManager.Instance.WinnerMessage.Value.ToString());
        }
    }

    private void OnGameOverChanged(bool previous, bool current)
    {
        if (current)
        {
            string msg =
                GameLoopManager.Instance.WinnerMessage.Value.ToString();
            ShowUI(msg);
        }
        else
        {
            panel.SetActive(false);
        }
    }

    private void OnWinnerMessageChanged(
        FixedString64Bytes prev,
        FixedString64Bytes curr)
    {
        if (GameLoopManager.Instance.IsGameOver.Value)
        {
            winnerText.text = curr.ToString();
        }
    }

    private void ShowUI(string message)
    {
        panel.SetActive(true);
        winnerText.text = message;

        if (NetworkManager.Singleton != null && returnButton != null)
        {
            bool isHost = NetworkManager.Singleton.IsHost;
            returnButton.interactable = isHost;

            if (!isHost)
            {
                var text = returnButton.GetComponentInChildren<TMP_Text>();
                if (text) text.text = "Waiting for Host...";
            }
        }
    }

    private void OnReturnClicked()
    {
        GameLoopManager.Instance.ReturnToLobby();
    }
}