using Unity.Netcode;
using UnityEngine;

public class RotateLaser : NetworkBehaviour, IInteractable, IPopUp
{
    [Header("Settings")]
    public Vector3[] rotationPoints;
    public InteractUI interactUI;
    public float rotationSpeed = 2f;

    private int currentPoint = 0;

    public bool CanInteract()
    {
        return GameLoopManager.Instance.PuzzleManager.StatuesDestroyed.Value == false;
    }

    public void Interact()
    {
        StartCoroutine(RotateLaserFunc());
    }

    public void ToggleUI(bool toggle)
    {
        if (toggle)
            interactUI.Show();
        else
            interactUI.Hide();
    }

    private System.Collections.IEnumerator RotateLaserFunc()
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
        currentPoint++;
        if (currentPoint >= rotationPoints.Length)
            currentPoint = 0;

        StartCoroutine(SmoothRotate(rotationPoints[currentPoint]));
    }

    private System.Collections.IEnumerator SmoothRotate(Vector3 targetRotation)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetQuaternion = Quaternion.Euler(targetRotation);
        float elapsedTime = 0f;

        while (elapsedTime < rotationSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / rotationSpeed;
            transform.rotation = Quaternion.Lerp(startRotation, targetQuaternion, t);
            yield return null;
        }

        transform.rotation = targetQuaternion;
    }
}
