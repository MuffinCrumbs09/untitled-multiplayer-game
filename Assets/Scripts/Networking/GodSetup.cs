using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles the initialization of the God player, including camera 
/// takeover and UI visibility management.
/// </summary>
public class GodSetup : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    [Tooltip("The UI canvas containing God-specific controls.")]
    private GameObject godCanvas;

    public override void OnNetworkSpawn()
    {
        // Disable UI for all clients except the owner.
        if (!IsOwner)
        {
            if (godCanvas != null)
            {
                godCanvas.SetActive(false);
            }
            return;
        }

        // Ensure UI is visible for the God player.
        if (godCanvas != null)
        {
            godCanvas.SetActive(true);
        }

        SetupGodCamera();
    }

    private void SetupGodCamera()
    {
        Camera mainCam = Camera.main;

        if (mainCam != null)
        {
            // Disable the standard follow camera logic.
            if (mainCam.TryGetComponent<CameraController>(out var sc))
            {
                sc.enabled = false;
            }

            // Enable the pre-existing RTS-style flight controller.
            if (mainCam.TryGetComponent<GodCameraController>(out var gc))
            {
                gc.enabled = true;
            }

            // Position the camera for a strategic overview.
            mainCam.transform.position = new Vector3(0, 50, -50);
            mainCam.transform.rotation = Quaternion.Euler(45, 0, 0);
        }
    }
}