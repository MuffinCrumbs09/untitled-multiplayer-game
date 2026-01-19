using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;

public class CurseInteractionManager : NetworkBehaviour
{
    [System.Serializable]
    public struct SpawnOption
    {
        public string name;
        public GameObject prefab;
        public float energyCost;
    }

    [Header("Configuration")]
    [SerializeField] private SpawnOption[] spawnOptions;
    [SerializeField] private LayerMask _groundLayer;

    private Camera _mainCamera;
    private int _currentSelectionIndex = 0;
    private CurseEnergySystem _energySystem;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _energySystem = GetComponent<CurseEnergySystem>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Refresh camera reference if lost (e.g. scene load)
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        // Prevent clicking through UI
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0))
        {
            TrySpawnEnemy();
        }
    }

    /// <summary>
    /// Selects an enemy by its defined name in the Inspector.
    /// Case-sensitive.
    /// </summary>
    public void SelectEnemy(string enemyName)
    {
        for (int i = 0; i < spawnOptions.Length; i++)
        {
            if (spawnOptions[i].name == enemyName)
            {
                _currentSelectionIndex = i;
                Debug.Log($"Selected: {enemyName} " +
                          $"(Cost: {spawnOptions[i].energyCost})");
                return;
            }
        }

        Debug.LogWarning($"Enemy type '{enemyName}' not found in " +
                         "Spawn Options!");
    }

    private void TrySpawnEnemy()
    {
        if (_mainCamera == null) return;

        // 1. Client-Side Prediction Check
        if (_energySystem != null)
        {
            // Safety check for index bounds
            if (_currentSelectionIndex < 0 ||
                _currentSelectionIndex >= spawnOptions.Length) return;

            float cost = spawnOptions[_currentSelectionIndex].energyCost;
            if (_energySystem.CurrentEnergy.Value < cost)
            {
                Debug.Log("Not enough energy!");
                return;
            }
        }

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _groundLayer))
        {
            SpawnEnemyServerRpc(hit.point, _currentSelectionIndex);
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnEnemyServerRpc(Vector3 position, int index)
    {
        if (index < 0 || index >= spawnOptions.Length) return;

        SpawnOption option = spawnOptions[index];

        // 2. Server-Side Validation (The Real Check)
        if (_energySystem.TryConsumeEnergy(option.energyCost))
        {
            Vector3 spawnPosition = position + (Vector3.up * 0.1f);

            GameObject enemy = Instantiate(
                option.prefab,
                spawnPosition,
                Quaternion.identity);

            enemy.GetComponent<NetworkObject>().Spawn();
        }
    }
}