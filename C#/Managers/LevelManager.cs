using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Level Generation")]
    [SerializeField] private LevelGenerator levelGenerator;
    
    [Header("References")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform towerSpawnPoint;
    [SerializeField] private Transform[] zombieSpawnPoints;
    [SerializeField] private WaveManager waveManager;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject towerPrefab;
    
    // Singleton instance
    private static LevelManager _instance;
    public static LevelManager Instance { get { return _instance; } }
    
    private void Awake()
    {
        // Singleton setup
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
        
        // Find references if not set
        if (levelGenerator == null)
        {
            levelGenerator = FindObjectOfType<LevelGenerator>();
            if (levelGenerator == null)
            {
                Debug.LogError("LevelGenerator not found! Adding one now.");
                levelGenerator = gameObject.AddComponent<LevelGenerator>();
            }
        }
        
        if (waveManager == null)
        {
            waveManager = FindObjectOfType<WaveManager>();
        }
    }
    
    private void Start()
    {
        // Don't auto-generate if this is a pre-made scene
        if (IsPreMadeScene())
        {
            Debug.Log("Using pre-made scene layout.");
            return;
        }
        
        // Generate level
        GenerateLevel();
    }
    
    public void GenerateLevel()
    {
        if (levelGenerator != null)
        {
            // Generate the level
            levelGenerator.GenerateLevel();
            
            // Get spawn points from generator
            List<Vector3> spawnPoints = levelGenerator.GetSpawnPoints();
            
            // Set up player and tower
            SpawnPlayerAndTower();
            
            // Set up zombie spawn points
            SetupZombieSpawnPoints(spawnPoints);
            
            // Notify the wave manager of new spawn points
            if (waveManager != null)
            {
                waveManager.UpdateSpawnPoints(zombieSpawnPoints);
            }
        }
        else
        {
            Debug.LogError("Level generator not assigned!");
        }
    }
    
    private bool IsPreMadeScene()
    {
        // Check if we already have structures that indicate a pre-made scene
        GameObject[] existingWalls = GameObject.FindGameObjectsWithTag("Wall");
        GameObject[] existingSpawnPoints = GameObject.FindGameObjectsWithTag("ZombieSpawn");
        
        return (existingWalls.Length > 10 || existingSpawnPoints.Length > 0);
    }
    
    private void SpawnPlayerAndTower()
    {
        // Spawn tower at center
        if (towerPrefab != null && towerSpawnPoint != null)
        {
            GameObject tower = Instantiate(towerPrefab, towerSpawnPoint.position, Quaternion.identity);
            tower.name = "Tower";
        }
        else if (towerPrefab != null)
        {
            GameObject tower = Instantiate(towerPrefab, Vector3.zero, Quaternion.identity);
            tower.name = "Tower";
        }
        
        // Spawn player near tower
        if (playerPrefab != null && playerSpawnPoint != null)
        {
            GameObject player = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
            player.name = "Player";
        }
        else if (playerPrefab != null)
        {
            Vector3 playerPos = new Vector3(0, 0, -5); // Default position: 5 units south of tower
            GameObject player = Instantiate(playerPrefab, playerPos, Quaternion.identity);
            player.name = "Player";
        }
    }
    
    private void SetupZombieSpawnPoints(List<Vector3> spawnPositions)
    {
        // Create or update zombie spawn points
        if (spawnPositions != null && spawnPositions.Count > 0)
        {
            zombieSpawnPoints = new Transform[spawnPositions.Count];
            
            for (int i = 0; i < spawnPositions.Count; i++)
            {
                GameObject spawnPoint = new GameObject("ZombieSpawnPoint_" + i);
                spawnPoint.transform.position = spawnPositions[i];
                spawnPoint.tag = "ZombieSpawn";
                zombieSpawnPoints[i] = spawnPoint.transform;
            }
            
            Debug.Log("Created " + zombieSpawnPoints.Length + " zombie spawn points");
        }
        else
        {
            Debug.LogWarning("No spawn positions provided by level generator!");
        }
    }
    
    public Transform[] GetZombieSpawnPoints()
    {
        if (zombieSpawnPoints == null || zombieSpawnPoints.Length == 0)
        {
            // Find spawn points in scene if not set
            GameObject[] spawnPointObjs = GameObject.FindGameObjectsWithTag("ZombieSpawn");
            zombieSpawnPoints = new Transform[spawnPointObjs.Length];
            
            for (int i = 0; i < spawnPointObjs.Length; i++)
            {
                zombieSpawnPoints[i] = spawnPointObjs[i].transform;
            }
        }
        
        return zombieSpawnPoints;
    }
}