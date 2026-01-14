using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The actual weapon prefab to equip when picked up.")]
    [SerializeField] private GameObject weaponPrefab;

    [Header("Floating Animation")]
    [Tooltip("How high off the ground the weapon hovers.")]
    [SerializeField] private float hoverHeight = 1.2f;
    [Tooltip("The distance it bobs up and down.")]
    [SerializeField] private float bobAmplitude = 0.1f;
    [Tooltip("The speed of the bobbing.")]
    [SerializeField] private float bobSpeed = 2f;
    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float rotationSpeed = 45f;

    private Vector3 _startPosition;
    private Transform _visualTransform;

    private void Start()
    {
        if (weaponPrefab == null)
        {
            Debug.LogError("WeaponPickup has no weapon prefab assigned!", this);
            Destroy(gameObject);
            return;
        }

        // 1. Setup Position (Raycast to find ground to avoid clipping)
        if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
        {
            _startPosition = hit.point + Vector3.up * hoverHeight;
        }
        else
        {
            _startPosition = transform.position;
        }
        transform.position = _startPosition;

        // 2. Instantiate Visuals
        CreateVisualCopy();
    }

    private void Update()
    {
        if (_visualTransform != null)
        {
            // Float
            float newY = _startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // Rotate
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<PlayerWeaponHandler>(out var handler))
            {
                handler.EquipWeapon(weaponPrefab);

                Destroy(gameObject);
            }
        }
    }

    private void CreateVisualCopy()
    {
        // Instantiate the weapon model as a child
        GameObject visual = Instantiate(weaponPrefab, transform);
        _visualTransform = visual.transform;

        // Reset transform to center it
        _visualTransform.localPosition = Vector3.zero;
        _visualTransform.localRotation = Quaternion.identity;

        // 1. Disable the logic script
        var damageScript = visual.GetComponent<MeleeWeaponDamage>();
        if (damageScript != null)
        {
            damageScript.enabled = false;
        }

        // 2. Disable physics by making it Kinematic
        var rb = visual.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
        }

        // 3. Disable colliders so they don't interfere with the trigger
        foreach (var c in visual.GetComponents<Collider>())
        {
            c.enabled = false;
        }
    }
}