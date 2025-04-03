using UnityEngine;
using System.Collections;
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
    
    private GameManager gameManager;
    private RunManager runManager;
    private GameObject playerInstance;
    private Vector3 centerPosition = Vector3.zero;
    
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
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        
        if (runManager == null)
            runManager = FindObjectOfType<RunManager>();
        
        // Set center position (where the tower and player spawn)
        centerPosition = new Vector3(0, 0, 0); // Adjust as needed for your level
    
        // Generate the combat level
        GenerateLevel();
    
        // Place player and tower at the center
        SpawnPlayerAndTower();
    
        // Start the combat with a delay
        StartCoroutine(StartCombatWithDelay(3.0f));
    }
    
    private IEnumerator StartCombatWithDelay(float delay)
    {
        // Display "Prepare for combat" message if you want
        // uiManager.ShowMessage("Prepare for combat...");
    
        yield return new WaitForSeconds(delay);
    
        // Start the wave
        if (waveManager != null)
            waveManager.StartNextWave();
    }
    
    // This will be called when combat is complete
    public void OnCombatComplete()
    {
        // Mark the current node as completed
        runManager.CompleteCurrentNode();
    
        // Show post-combat UI
        gameManager.TransitionToState(GameState.PostCombat);
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
    
    public void ResetLevelForCombat()
    {
        // Clear any existing zombies
        GameObject[] zombies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject zombie in zombies)
        {
            Destroy(zombie);
        }
    
        // Reset player position to center
        if (playerInstance != null)
        {
            playerInstance.transform.position = centerPosition;
        }
    
        // Reset tower health if needed
    
        // Start combat with delay
        StartCoroutine(StartCombatWithDelay(3.0f));
    }
}