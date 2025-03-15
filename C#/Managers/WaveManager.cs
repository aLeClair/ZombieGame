using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public int zombieCount;
        public float spawnRate;
        public List<ZombieType> zombieTypes;
    }

    [System.Serializable]
    public class ZombieType
    {
        public GameObject zombiePrefab;
        public float spawnWeight;
    }

    [Header("Wave Settings")] [SerializeField]
    private Wave[] waves;

    [SerializeField] private float timeBetweenWaves = 30f;
    [SerializeField] private bool autoStartWaves = false;

    [Header("Spawn Settings")] [SerializeField]
    private Transform[] spawnPoints;

    [SerializeField] private float minDistanceFromPlayer = 15f;

    // State
    private int currentWaveIndex = -1;
    private int zombiesRemaining = 0;
    private int zombiesSpawned = 0;
    private bool isSpawning = false;
    private bool isWaveActive = false;
    private float waveCountdown = 0f;

    // References
    private GameManager gameManager;
    private Transform playerTransform;

    // Events
    public delegate void WaveEventHandler(int waveNumber, int totalWaves);

    public event WaveEventHandler OnWaveStart;
    public event WaveEventHandler OnWaveEnd;

    public delegate void WaveCountdownHandler(float remainingTime);

    public event WaveCountdownHandler OnWaveCountdownChanged;

    public delegate void ZombieCountChangedHandler(int remaining, int total);

    public event ZombieCountChangedHandler OnZombieCountChanged;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Initialize spawn points if not set
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            InitializeDefaultSpawnPoints();
        }

        // Start first wave automatically if enabled
        if (autoStartWaves)
        {
            waveCountdown = timeBetweenWaves;
        }
    }

    void Update()
    {
        // Handle wave countdown between waves
        if (!isWaveActive && currentWaveIndex < waves.Length - 1)
        {
            if (waveCountdown > 0)
            {
                waveCountdown -= Time.deltaTime;

                // Update UI countdown
                if (OnWaveCountdownChanged != null)
                {
                    OnWaveCountdownChanged(waveCountdown);
                }

                if (waveCountdown <= 0)
                {
                    StartNextWave();
                }
            }
        }
    }

    private void InitializeDefaultSpawnPoints()
    {
        // Create spawn points in a circle around the map
        GameObject spawnPointsParent = new GameObject("SpawnPoints");
        spawnPointsParent.transform.parent = transform;

        int spawnPointCount = 8;
        spawnPoints = new Transform[spawnPointCount];

        for (int i = 0; i < spawnPointCount; i++)
        {
            float angle = i * (360f / spawnPointCount);
            float x = Mathf.Sin(angle * Mathf.Deg2Rad) * 40f;
            float z = Mathf.Cos(angle * Mathf.Deg2Rad) * 40f;

            GameObject spawnPoint = new GameObject("SpawnPoint_" + i);
            spawnPoint.transform.parent = spawnPointsParent.transform;
            spawnPoint.transform.position = new Vector3(x, 0f, z);

            spawnPoints[i] = spawnPoint.transform;
        }
    }

    public void StartWaveCountdown()
    {
        if (!isWaveActive && currentWaveIndex < waves.Length - 1)
        {
            waveCountdown = timeBetweenWaves;
        }
    }

    public void StartNextWave()
    {
        if (isWaveActive) return;

        currentWaveIndex++;

        if (currentWaveIndex < waves.Length)
        {
            Wave wave = waves[currentWaveIndex];

            // Reset counters
            zombiesRemaining = wave.zombieCount;
            zombiesSpawned = 0;
            isWaveActive = true;

            // Trigger wave start event
            if (OnWaveStart != null)
            {
                OnWaveStart(currentWaveIndex + 1, waves.Length);
            }

            // Update zombie count UI
            if (OnZombieCountChanged != null)
            {
                OnZombieCountChanged(zombiesRemaining, wave.zombieCount);
            }

            // Start spawning zombies
            StartCoroutine(SpawnWave(wave));
        }
        else
        {
            // All waves completed - victory!
            gameManager.GameWon();
        }
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        isSpawning = true;

        // Wait before first spawn
        yield return new WaitForSeconds(1.5f);

        // Spawn zombies
        while (zombiesSpawned < wave.zombieCount)
        {
            SpawnZombie(wave);

            // Wait for next spawn
            yield return new WaitForSeconds(1f / wave.spawnRate);
        }

        isSpawning = false;
    }

    private void SpawnZombie(Wave wave)
    {
        // Pick a spawn point
        Transform spawnPoint = GetSpawnPoint();

        if (spawnPoint != null)
        {
            // Select zombie type based on weights
            GameObject zombiePrefab = SelectZombieType(wave.zombieTypes);

            if (zombiePrefab != null)
            {
                // Spawn the zombie
                Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);

                zombiesSpawned++;
            }
        }
    }

    private Transform GetSpawnPoint()
    {
        // Choose a random spawn point
        if (spawnPoints.Length == 0) return null;

        // If player exists, ensure minimum distance
        if (playerTransform != null)
        {
            // Try several times to find a suitable spawn point
            for (int i = 0; i < 5; i++)
            {
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

                if (Vector3.Distance(spawnPoint.position, playerTransform.position) >= minDistanceFromPlayer)
                {
                    return spawnPoint;
                }
            }

            // If no suitable point found, just use any
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }
        else
        {
            // No player, just pick random
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }
    }

    private GameObject SelectZombieType(List<ZombieType> zombieTypes)
    {
        if (zombieTypes.Count == 0) return null;

        // Sum all weights
        float totalWeight = 0;
        foreach (ZombieType type in zombieTypes)
        {
            totalWeight += type.spawnWeight;
        }

        // Random value
        float randomValue = Random.value * totalWeight;

        // Select zombie based on weight
        float currentWeight = 0;
        foreach (ZombieType type in zombieTypes)
        {
            currentWeight += type.spawnWeight;

            if (randomValue <= currentWeight)
            {
                return type.zombiePrefab;
            }
        }

        // Fallback (should never get here)
        return zombieTypes[0].zombiePrefab;
    }

    public void ZombieKilled()
    {
        zombiesRemaining--;

        // Update UI
        if (OnZombieCountChanged != null)
        {
            OnZombieCountChanged(zombiesRemaining, waves[currentWaveIndex].zombieCount);
        }

        CheckWaveComplete();
    }

    private void CheckWaveComplete()
    {
        if (zombiesRemaining <= 0 && !isSpawning)
        {
            EndWave();
        }
    }

    private void EndWave()
    {
        isWaveActive = false;

        // Trigger wave end event
        if (OnWaveEnd != null)
        {
            OnWaveEnd(currentWaveIndex + 1, waves.Length);
        }

        // Check if all waves are completed
        if (currentWaveIndex >= waves.Length - 1)
        {
            // All waves completed - victory!
            gameManager.GameWon();
        }
        else
        {
            // Start countdown to next wave
            waveCountdown = timeBetweenWaves;

            // Let the game manager know the round is complete
            gameManager.RoundComplete(currentWaveIndex + 1);
        }
    }

    public int GetCurrentWave()
    {
        return currentWaveIndex + 1;
    }

    public int GetTotalWaves()
    {
        return waves.Length;
    }

    public float GetTimeBetweenWaves()
    {
        return timeBetweenWaves;
    }

    public bool IsWaveActive()
    {
        return isWaveActive;
    }

    public int GetZombiesRemaining()
    {
        return zombiesRemaining;
    }
}