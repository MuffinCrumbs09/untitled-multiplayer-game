using UnityEngine;

public class GodCameraController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 15f;
    [SerializeField] private float _shiftSpeedMultiplier = 3f;
    [Tooltip("How fast the movement accelerates/decelerates. Higher is snappier, Lower is floatier.")]
    [SerializeField] private float _movementSmoothing = 10f;

    [Header("Rotation")]
    [SerializeField] private float _rotationSensitivity = 2.5f;
    [SerializeField] private Vector2 _pitchClamp = new Vector2(-85f, 85f);
    [Tooltip("How fast the rotation catches up to the mouse. Higher is snappier, Lower is smoother.")]
    [SerializeField] private float _rotationSmoothing = 15f;

    [Header("Zoom")]
    [SerializeField] private float _zoomSpeed = 15f;
    [SerializeField] private float _zoomSmoothing = 10f;

    // Internal state variables for smoothing
    private Vector3 _currentInputVector;
    private Vector3 _smoothInputVector;

    private float _targetYaw;
    private float _targetPitch;
    private Quaternion _targetRotation;

    private float _targetZoom;

    private void OnEnable()
    {
        // Sync internal state with current transform to prevent jumping on start
        Vector3 startAngles = transform.eulerAngles;
        _targetYaw = startAngles.y;
        _targetPitch = startAngles.x;
        _targetRotation = transform.rotation;
    }

    void Update()
    {
        HandleZoom();
        HandleRotation();
        HandleMovement();
    }

    private void HandleRotation()
    {
        // 1. Calculate the Target Rotation (Only when holding Right Click)
        if (Input.GetMouseButton(1))
        {
            _targetYaw += Input.GetAxis("Mouse X") * _rotationSensitivity;
            _targetPitch -= Input.GetAxis("Mouse Y") * _rotationSensitivity;

            // Clamp pitch
            _targetPitch = Mathf.Clamp(_targetPitch, _pitchClamp.x, _pitchClamp.y);
        }

        // 2. Convert to Quaternion
        _targetRotation = Quaternion.Euler(_targetPitch, _targetYaw, 0.0f);

        // 3. Smoothly interpolate current rotation towards target rotation
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            _targetRotation,
            Time.deltaTime * _rotationSmoothing
        );
    }

    private void HandleMovement()
    {
        // 1. Get Raw Input (Only when holding Right Click)
        if (Input.GetMouseButton(1))
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            _currentInputVector = new Vector3(x, 0, z);
        }
        else
        {
            // If we let go of right click, stop input, but smoothing will handle the deceleration
            _currentInputVector = Vector3.zero;
        }

        // 2. Smoothly interpolate the input vector (Creates momentum/inertia)
        _smoothInputVector = Vector3.Lerp(
            _smoothInputVector,
            _currentInputVector,
            Time.deltaTime * _movementSmoothing
        );

        // 3. Determine Speed
        float currentSpeed = Input.GetKey(KeyCode.LeftShift)
            ? _moveSpeed * _shiftSpeedMultiplier
            : _moveSpeed;

        // 4. Apply Movement relative to camera orientation
        // We use the smoothed vector here
        Vector3 direction =
            (transform.forward * _smoothInputVector.z) + (transform.right * _smoothInputVector.x);

        transform.position += direction * currentSpeed * Time.deltaTime;
    }

    private void HandleZoom()
    {
        // 1. Get Input
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // 2. Apply directly to a target, but smooth the result? 
        // For infinite scrolling flight, it's better to just smooth the input impulse.
        // We'll treat zoom like a momentary burst of speed forward/back.

        float targetZoomSpeed = scroll * _zoomSpeed;

        // We can reuse the movement smoothing logic or use a separate value.
        // Here we just apply it directly but relying on the inherent smoothness of the scroll wheel + Lerp
        // creates a nice effect.

        Vector3 zoomDir = transform.forward * targetZoomSpeed;

        // Simple linear move, but could be smoothed if desired. 
        // Given the constraints, let's keep zoom simple but responsive.
        transform.position += zoomDir;
    }
}