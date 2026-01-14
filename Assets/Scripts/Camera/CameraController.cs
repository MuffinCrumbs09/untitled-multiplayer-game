using UnityEngine;

/// <summary>
/// Controls the main camera.
/// multiplayer - Target is assigned dynamically by the local player.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The transform that the camera should follow. Can be set at runtime.")]
    [SerializeField] private Transform target;

    [Header("Positioning")]
    [Tooltip("How quickly the camera follows the target.")]
    [SerializeField][Range(0.01f, 1f)] private float smoothSpeed = 0.125f;

    [Header("Rotation")]
    [SerializeField][Min(0)] private float rotationSpeed = 120f;
    [SerializeField] private Vector2 pitchLimits = new Vector2(10f, 85f);

    [Header("Zoom")]
    [SerializeField][Min(0)] private float zoomSpeed = 20f;
    [SerializeField] private Vector2 zoomLimits = new Vector2(5f, 25f);

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField][Min(0)] private float collisionPadding = 0.2f;

    private bool _isRotating = false;
    private float _currentDistance;
    private float _yaw = 0f;
    private float _pitch = 45f;
    private Vector2 _rotationInput;

    private void Awake()
    {
        _currentDistance = (zoomLimits.x + zoomLimits.y) / 2f;

        if (target != null)
        {
            transform.LookAt(target);
        }
    }

    private void OnEnable()
    {
        InputProcessor.onCameraRotateToggleStarted += StartRotation;
        InputProcessor.onCameraRotateToggleCanceled += StopRotation;
        InputProcessor.onCameraRotate += HandleRotationInput;
        InputProcessor.onCameraZoom += HandleZoom;
    }

    private void OnDisable()
    {
        InputProcessor.onCameraRotateToggleStarted -= StartRotation;
        InputProcessor.onCameraRotateToggleCanceled -= StopRotation;
        InputProcessor.onCameraRotate -= HandleRotationInput;
        InputProcessor.onCameraZoom -= HandleZoom;
    }

    /// <summary>
    /// Call this from the Local Player's OnNetworkSpawn to make the camera follow them.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            // Snap to target immediately to prevent flying camera
            transform.position = target.position - (transform.forward * _currentDistance);
        }
    }

    private void LateUpdate()
    {
        // Do nothing if we don't have a player to follow yet
        if (target == null) return;

        UpdateRotation();

        // 1. Calculate the desired position based on rotation and zoom.
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
        Vector3 desiredOffset = rotation * new Vector3(0, 0, -_currentDistance);
        Vector3 desiredPosition = target.position + desiredOffset;

        // 2. Check for collisions.
        Vector3 direction = desiredPosition - target.position;
        float distance = _currentDistance;
        RaycastHit hit;

        // Simple sphere cast to prevent clipping through walls
        if (Physics.SphereCast(target.position, collisionPadding,
            direction.normalized, out hit, distance, collisionLayers))
        {
            distance = hit.distance;
        }

        // 3. Calculate the final, collision-aware position.
        Vector3 finalPosition = target.position + direction.normalized * distance;

        // 4. Apply smoothing.
        transform.position = Vector3.Lerp(transform.position, finalPosition, smoothSpeed);
        transform.LookAt(target.position);
    }

    private void StartRotation() => _isRotating = true;
    private void StopRotation() => _isRotating = false;

    private void HandleRotationInput(Vector2 delta)
    {
        if (_isRotating)
        {
            _rotationInput = delta;
        }
    }

    private void UpdateRotation()
    {
        if (!_isRotating || _rotationInput == Vector2.zero) return;

        float deltaTime = Time.deltaTime;
        _yaw += _rotationInput.x * rotationSpeed * deltaTime;
        _pitch -= _rotationInput.y * rotationSpeed * deltaTime;
        _pitch = Mathf.Clamp(_pitch, pitchLimits.x, pitchLimits.y);

        _rotationInput = Vector2.zero;
    }

    private void HandleZoom(Vector2 scroll)
    {
        // Scroll y is usually 120 or -120, normalize it slightly or just use as is
        float zoomAmount = scroll.y * zoomSpeed * Time.deltaTime * -0.1f;

        _currentDistance = Mathf.Clamp(
            _currentDistance + zoomAmount,
            zoomLimits.x,
            zoomLimits.y);
    }
}