using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class InteractUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private TextMeshProUGUI buttonText;


    private Camera mainCamera;
    private void Awake()
    {
        Hide();
    }

    private void SearchForPlayerCam()
    {
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            if (player.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.Singleton.LocalClientId)
                mainCamera = player.GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (mainCamera == null) SearchForPlayerCam();

        if (canvas.enabled && mainCamera != null)
        {
            LookAtCamera();
        }
    }

    private void LookAtCamera()
    {
        Vector3 directionToCamera = mainCamera.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(-directionToCamera);
    }


    public void Show()
    {
        if (canvas == null)
        {
            Debug.LogError("InteractUI: Canvas reference is not assigned!", this);
            return;
        }
        canvas.enabled = true;
    }

    public void Hide()
    {
        if(canvas == null) return;
        
        canvas.enabled = false;
    }
}