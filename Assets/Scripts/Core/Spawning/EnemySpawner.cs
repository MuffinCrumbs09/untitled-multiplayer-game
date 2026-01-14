using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Random = UnityEngine.Random;

[RequireComponent(typeof(BoxCollider))]
public class EnemySpawner : NetworkBehaviour
{
    public event Action OnAllWavesCleared;

    [Serializable]
    public struct SpawnEntry
    {
        public GameObject enemyPrefab;
        [Min(1)] public int count;
    }

    [Serializable]
    public struct Wave
    {
        public List<SpawnEntry> enemies;
        [Min(0)] public float delayBeforeWave;
    }

    [Header("Configuration")]
    [SerializeField] private List<Wave> waves;

    [Header("Spawning Area")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField][Min(0)] private float minSpawnDistanceFromPlayer = 5f;

    private BoxCollider _spawnArea;
    private Transform _playerTransform;
    private int _enemiesAlive;
    private bool _encounterIsActive = false;

    private void Awake()
    {
        _spawnArea = GetComponent<BoxCollider>();
        _spawnArea.isTrigger = true;
    }

    public void BeginEncounter(Transform player)
    {
        // Only Server runs encounters
        if (!IsServer) return;
        if (_encounterIsActive) return;

        _playerTransform = player;
        _encounterIsActive = true;
        StartCoroutine(SpawnRoutine());
    }

    public void OnEnemyDied()
    {
        _enemiesAlive--;
    }

    private IEnumerator SpawnRoutine()
    {
        foreach (var wave in waves)
        {
            yield return new WaitForSeconds(wave.delayBeforeWave);

            SpawnWave(wave);

            yield return new WaitUntil(() => _enemiesAlive <= 0);
        }

        _encounterIsActive = false;
        OnAllWavesCleared?.Invoke();
    }

    private void SpawnWave(Wave wave)
    {
        int totalInWave = 0;
        foreach (var entry in wave.enemies) totalInWave += entry.count;
        _enemiesAlive = totalInWave;

        foreach (var entry in wave.enemies)
        {
            for (int i = 0; i < entry.count; i++)
            {
                if (TryGetValidSpawnPoint(out Vector3 spawnPoint))
                {
                    // 1. Instantiate on Server
                    GameObject enemyObj = Instantiate(entry.enemyPrefab, spawnPoint, Quaternion.identity);

                    // 2. Spawn on Network
                    enemyObj.GetComponent<NetworkObject>().Spawn();

                    // 3. Initialize AI
                    if (enemyObj.TryGetComponent<EnemyController>(out var controller))
                    {
                        controller.Initialize(this, _playerTransform);
                    }
                }
                else
                {
                    _enemiesAlive--;
                }
            }
        }
    }

    private bool TryGetValidSpawnPoint(out Vector3 point)
    {
        for (int i = 0; i < 10; i++)
        {
            Bounds bounds = _spawnArea.bounds;
            float randX = Random.Range(bounds.min.x, bounds.max.x);
            float randZ = Random.Range(bounds.min.z, bounds.max.z);
            Vector3 randomPoint = new Vector3(randX, bounds.center.y, randZ);

            // Safety check for player distance (if player exists)
            if (_playerTransform != null && Vector3.Distance(randomPoint, _playerTransform.position) < minSpawnDistanceFromPlayer)
            {
                continue;
            }

            if (Physics.Raycast(randomPoint + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundMask))
            {
                point = hit.point;
                return true;
            }
        }
        point = Vector3.zero;
        return false;
    }
}