using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] private float interactRange = 2f;

    private IInteractable curInteraction;
    private IInteractable prevInteraction;

    #region Unity Functions
    private void Start()
    {
        if (!IsOwner)
            return;

        InputManager.onInteractStarted += () =>
                {
                    curInteraction?.Interact();
                };
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        UpdateInteractions();
        HandleUI();
    }
    #endregion

    private void HandleUI()
    {
        if (curInteraction != prevInteraction)
        {
            if (prevInteraction is Grave prevGrave)
            {
                prevGrave.ToggleUI(false);
            }
            if (curInteraction is Grave grave)
            {
                grave.ToggleUI(true);
            }

            prevInteraction = curInteraction;
        }
    }

    private void UpdateInteractions()
    {
        curInteraction = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange);

        foreach (Collider hit in hits)
        {
            hit.TryGetComponent(out IInteractable interactable);
            if (interactable != null && interactable.CanInteract())
            {
                curInteraction = interactable;
                break;
            }
        }
    }
}