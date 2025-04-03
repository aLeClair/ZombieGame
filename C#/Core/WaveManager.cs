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
        public WaveType waveType = WaveType.Standard;
        public float duration = 120f; // For time survival waves
        public int waveCount = 3; // For wave survival waves
    }

    public enum WaveType
    {
        Standard,       // Kill all zombies
        TimeSurvival,   // Survive for a period of time
        WaveSurvival    // Survive multiple waves
    }

    [Header("Wave Settings")]
    [SerializeField] private Wave[] waves;
    [SerializeField] private float timeBetweenWaves = 30f;
    [SerializeField] private bool autoStartWaves = false;
    
    [Header("Default Wave Settings")]
    [SerializeField] private int defaultWaveZombieCount = 10;
    [SerializeField] private float defaultSpawnRate = 1f;
    [SerializeField] private int waveCountIfEmpty = 3;

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minDistanceFromPlayer = 15f;
    [SerializeField] private ZombieFactory zombieFactory;
    [SerializeField] private GameObject defaultZombiePrefab; // Fallback if ZombieFactory not available

    // State
    private int currentWaveIndex = -1;
    private int zombiesRemaining = 0;
    private int zombiesSpawned = 0;
    private bool isSpawning = false;
    private bool isWaveActive = false;
    private float waveCountdown = 0f;
    private float waveTimer = 0f;
    private int subWaveCounter = 0;
    private WaveType currentWaveType = WaveType.Standard;

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
        
        // Find zombie factory if not set
        if (zombieFactory == null)
        {
            zombieFactory = FindObjectOfType<ZombieFactory>();
            if (zombieFactory == null && defaultZombiePrefab != null)
            {
                Debug.LogWarning("ZombieFactory not found. Will use default zombie prefab.");
            }
            else if (zombieFactory == null)
            {
                Debug.LogError("ZombieFactory not found and no default zombie prefab set!");
            }
        }
        
        // If no waves are defined, create some default waves
        if (waves == null || waves.Length == 0)
        {
            CreateDefaultWaves();
        }

        // Start first wave automatically if enabled
        if (autoStartWaves)
        {
            waveCountdown = 5f; // Short countdown to first wave
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
        
        // Handle time survival wave type
        if (isWaveActive && currentWaveType == WaveType.TimeSurvival)
        {
            waveTimer -= Time.deltaTime;
            
            if (waveTimer <= 0)
            {
                // Time survived - end wave
                EndWave();
            }
        }
    }

    private void CreateDefaultWaves()
    {
        waves = new Wave[waveCountIfEmpty];
        
        for (int i = 0; i < waves.Length; i++)
        {
            waves[i] = new Wave();
            waves[i].waveName = "Wave " + (i + 1);
            waves[i].zombieCount = defaultWaveZombieCount + (i * 5); // Increase by 5 per wave
            waves[i].spawnRate = defaultSpawnRate + (i * 0.2f); // Increase spawn rate
            
            // Make last wave a boss wave
            if (i == waves.Length - 1)
            {
                waves[i].zombieCount *= 2; // Double zombies for final wave
                waves[i].spawnRate *= 1.5f; // 50% faster spawning
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
            currentWaveType = wave.waveType;

            // Reset counters
            zombiesRemaining = 0; // Will be incremented as zombies spawn
            zombiesSpawned = 0;
            isWaveActive = true;
            
            // Set up wave type specific counters
            switch (currentWaveType)
            {
                case WaveType.TimeSurvival:
                    waveTimer = wave.duration;
                    break;
                case WaveType.WaveSurvival:
                    subWaveCounter = 0;
                    break;
            }

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
            if (gameManager != null)
            {
                gameManager.GameWon();
            }
        }
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        isSpawning = true;

        // Wait before first spawn
        yield return new WaitForSeconds(1.5f);
        
        // For wave survival, we spawn zombies in sub-waves
        if (wave.waveType == WaveType.WaveSurvival)
        {
            for (int subWave = 0; subWave < wave.waveCount; subWave++)
            {
                subWaveCounter = subWave + 1;
                
                // Calculate zombies for this sub-wave (increasing with each wave)
                int subWaveZombies = Mathf.RoundToInt(wave.zombieCount * (1 + subWave * 0.5f) / wave.waveCount);
                
                for (int i = 0; i < subWaveZombies; i++)
                {
                    SpawnZombie(wave);
                    yield return new WaitForSeconds(1f / wave.spawnRate);
                }
                
                // Wait for all zombies to be killed before next sub-wave
                while (FindObjectsOfType<ZombieController>().Length > 0)
                {
                    yield return new WaitForSeconds(1f);
                }
                
                // Brief pause between sub-waves
                yield return new WaitForSeconds(5f);
            }
            
            // End wave after all sub-waves complete
            EndWave();
        }
        else
        {
            // Standard or time survival - spawn zombies normally
            for (int i = 0; i < wave.zombieCount; i++)
            {
                SpawnZombie(wave);

                // Wait for next spawn
                yield return new WaitForSeconds(1f / wave.spawnRate);
                
                // For time survival, we might need to spawn more zombies if time hasn't run out
                if (wave.waveType == WaveType.TimeSurvival && i == wave.zombieCount - 1 && waveTimer > 10f)
                {
                    // Continue spawning at half the rate
                    i = wave.zombieCount / 2;
                }
            }
        }

        isSpawning = false;
        
        // Only automatically end for standard wave type
        if (wave.waveType == WaveType.Standard)
        {
            // Wait for remaining zombies to be killed
            yield return StartCoroutine(WaitForRemainingZombies());
        }
    }
    
    private IEnumerator WaitForRemainingZombies()
    {
        // Wait until all zombies are defeated
        while (zombiesRemaining > 0)
        {
            yield return new WaitForSeconds(1f);
        }
        
        // End wave when all zombies are killed
        EndWave();
    }

    private void SpawnZombie(Wave wave)
    {
        // Select a random spawn point
        Transform spawnPoint = GetSpawnPoint();

        if (spawnPoint != null)
        {
            // Create spawn position with corrected height
            Vector3 spawnPosition = spawnPoint.position;
            spawnPosition.y = 1.0f; // Higher off the ground
    
            GameObject zombie = null;
            
            // Use factory if available
            if (zombieFactory != null)
            {
                zombie = zombieFactory.SpawnZombie(spawnPosition, currentWaveIndex + 1);
            }
            // Fallback to default prefab if no factory
            else if (defaultZombiePrefab != null)
            {
                zombie = Instantiate(defaultZombiePrefab, spawnPosition, Quaternion.identity);
            }
            
            if (zombie != null)
            {
                // Register for zombie death notification
                ZombieController zombieController = zombie.GetComponent<ZombieController>();
                if (zombieController != null)
                {
                    // Use lambda to avoid event subscription issues
                    zombieController.OnZombieDeath += HandleZombieKilled;
                }
            
                zombiesRemaining++;
                zombiesSpawned++;
            
                // Update UI
                if (OnZombieCountChanged != null)
                {
                    OnZombieCountChanged(zombiesRemaining, wave.zombieCount);
                }
            
                Debug.Log($"Spawned zombie. Remaining: {zombiesRemaining}");
            }
        }
    }

    private void HandleZombieKilled()
    {
        // This method is called when a zombie dies via the event
        ZombieKilled();
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

    private Transform GetSpawnPoint()
    {
        // Choose a random spawn point
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

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

    private void CheckWaveComplete()
    {
        // Only check for completion in standard wave type
        if (currentWaveType == WaveType.Standard)
        {
            if (zombiesRemaining <= 0 && !isSpawning)
            {
                EndWave();
            }
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
            if (gameManager != null)
            {
                gameManager.GameWon();
            }
        }
        else
        {
            // Start countdown to next wave
            waveCountdown = timeBetweenWaves;

            // Let the game manager know the round is complete
            if (gameManager != null)
            {
                gameManager.RoundComplete(currentWaveIndex + 1);
            }
        }
    }
    
    public void UpdateSpawnPoints(Transform[] newSpawnPoints)
    {
        if (newSpawnPoints != null && newSpawnPoints.Length > 0)
        {
            spawnPoints = newSpawnPoints;
            Debug.Log("Updated spawn points: " + spawnPoints.Length);
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
    
    public WaveType GetCurrentWaveType()
    {
        return currentWaveType;
    }
    
    public float GetWaveTimeRemaining()
    {
        return waveTimer;
    }
    
    public int GetSubWaveCounter()
    {
        return subWaveCounter;
    }
}