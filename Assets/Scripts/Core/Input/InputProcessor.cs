using System;
using UnityEngine;

/// <summary>
/// Singleton that processes raw input events from InputManager into game-ready actions.
/// Designed to be destroyed on scene unload to ensure a clean state reset.
/// </summary>
public class InputProcessor : MonoBehaviour
{
    public static InputProcessor Instance { get; private set; }

    public static event Action<Vector2> onMove;
    public static event Action<Vector2> onAim;
    public static event Action onAttackStarted;
    public static event Action onAttackCanceled;
    public static event Action onDodgeStarted;
    public static event Action onInteractStarted;
    public static event Action onToggleHandViewStarted;
    public static event Action<Vector2> onCameraRotate;
    public static event Action onCameraRotateToggleStarted;
    public static event Action onCameraRotateToggleCanceled;
    public static event Action<Vector2> onCameraZoom;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        InputManager.onMove += (v) => onMove?.Invoke(v);
        InputManager.onAim += (v) => onAim?.Invoke(v);
        InputManager.onAttackStarted += () => onAttackStarted?.Invoke();
        InputManager.onAttackCanceled += () => onAttackCanceled?.Invoke();
        InputManager.onDodgeStarted += () => onDodgeStarted?.Invoke();
        InputManager.onInteractStarted += () => onInteractStarted?.Invoke();
        InputManager.onToggleHandViewStarted += () => onToggleHandViewStarted?.Invoke();
        InputManager.onCameraRotate += (v) => onCameraRotate?.Invoke(v);
        InputManager.onCameraRotateToggleStarted += () => onCameraRotateToggleStarted?.Invoke();
        InputManager.onCameraRotateToggleCanceled += () => onCameraRotateToggleCanceled?.Invoke();
        InputManager.onCameraZoom += (v) => onCameraZoom?.Invoke(v);
    }

    private void OnDisable()
    {
        ClearAllEvents();
    }

    private void ClearAllEvents()
    {
        onMove = null;
        onAim = null;
        onAttackStarted = null;
        onAttackCanceled = null;
        onDodgeStarted = null;
        onInteractStarted = null;
        onToggleHandViewStarted = null;
        onCameraRotate = null;
        onCameraRotateToggleStarted = null;
        onCameraRotateToggleCanceled = null;
        onCameraZoom = null;
    }
}