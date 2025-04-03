using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSetup : MonoBehaviour
{
    [Header("Required Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private GameObject gameManagerPrefab;
    [SerializeField] private GameObject uiManagerPrefab;
    [SerializeField] private GameObject waveManagerPrefab;
    [SerializeField] private GameObject upgradeManagerPrefab;
    [SerializeField] private GameObject lootManagerPrefab;
    [SerializeField] private GameObject zombiePrefab; // Default zombie
    
    [Header("Map Settings")]
    [SerializeField] private Vector3 towerPosition = Vector3.zero;
    [SerializeField] private Vector3 playerStartPosition = new Vector3(0, 0, -5);
    [SerializeField] private bool generateNavMesh = true;
    
    [Header("Game Settings")]
    [SerializeField] private int startingGold = 100;
    [SerializeField] private int difficultyLevel = 1;
    
    void Start()
    {
        SetupGame();
    }
    
    private void SetupGame()
    {
        // Create main game systems
        CreateGameSystems();
        
        // Create player and tower
        CreatePlayerAndTower();
        
        // Setup zombies if needed
        SetupZombies();
        
        // Generate nav mesh if needed
        if (generateNavMesh)
        {
            GenerateNavigationMesh();
        }
    }
    
    private void CreateGameSystems()
    {
        // Game Manager
        if (gameManagerPrefab != null && FindObjectOfType<GameManager>() == null)
        {
            GameObject gameManagerObj = Instantiate(gameManagerPrefab);
            gameManagerObj.name = "GameManager";
            
            // Set initial values
            GameManager gameManager = gameManagerObj.GetComponent<GameManager>();
            if (gameManager != null)
            {
                // Set starting gold, difficulty, etc.
                // This would require adding methods to GameManager to set these values
            }
        }
        
        // Wave Manager
        if (waveManagerPrefab != null && FindObjectOfType<WaveManager>() == null)
        {
            GameObject waveManagerObj = Instantiate(waveManagerPrefab);
            waveManagerObj.name = "WaveManager";
            
            // Configure waves based on difficulty
            // WaveManager waveManager = waveManagerObj.GetComponent<WaveManager>();
            // if (waveManager != null) { ... }
        }
        
        // Upgrade Manager
        if (upgradeManagerPrefab != null && FindObjectOfType<UpgradeManager>() == null)
        {
            GameObject upgradeManagerObj = Instantiate(upgradeManagerPrefab);
            upgradeManagerObj.name = "UpgradeManager";
        }
        
        // Loot Manager
        if (lootManagerPrefab != null && FindObjectOfType<LootManager>() == null)
        {
            GameObject lootManagerObj = Instantiate(lootManagerPrefab);
            lootManagerObj.name = "LootManager";
        }
        
        // UI Manager (create last so it can find all the other systems)
        if (uiManagerPrefab != null && FindObjectOfType<UIManager>() == null)
        {
            GameObject uiManagerObj = Instantiate(uiManagerPrefab);
            uiManagerObj.name = "UIManager";
        }
    }
    
    private void CreatePlayerAndTower()
    {
        // Create player if doesn't exist
        if (playerPrefab != null && GameObject.FindGameObjectWithTag("Player") == null)
        {
            GameObject playerObj = Instantiate(playerPrefab, playerStartPosition, Quaternion.identity);
            playerObj.name = "Player";
            
            // Ensure player has required components
            if (playerObj.GetComponent<PlayerController>() == null)
            {
                playerObj.AddComponent<PlayerController>();
            }
            
            if (playerObj.GetComponent<PlayerHealth>() == null)
            {
                playerObj.AddComponent<PlayerHealth>();
            }
            
            if (playerObj.GetComponent<WeaponManager>() == null)
            {
                playerObj.AddComponent<WeaponManager>();
            }
            
            if (playerObj.GetComponent<PlayerInteractionSystem>() == null)
            {
                 playerObj.AddComponent<PlayerInteractionSystem>();
            }
            
            // Add building system to player
            if (playerObj.GetComponent<BuildingSystem>() == null)
            {
                playerObj.AddComponent<BuildingSystem>();
            }
        }
        
        // Create tower if doesn't exist
        if (towerPrefab != null && GameObject.FindGameObjectWithTag("Tower") == null)
        {
            GameObject towerObj = Instantiate(towerPrefab, towerPosition, Quaternion.identity);
            towerObj.name = "Tower";
            
            // Ensure tower has required components
            if (towerObj.GetComponent<TowerController>() == null) 
            {
                 towerObj.AddComponent<TowerController>();
            }
        }
    }
    
    private void SetupZombies()
    {
        // Make sure at least one zombie prefab exists
        if (zombiePrefab == null)
        {
            Debug.LogError("No zombie prefab assigned! Game may not function correctly.");
        }
    }
    
    private void GenerateNavigationMesh()
    {
        // This would typically be handled in the Unity editor using NavMesh baking
        // For runtime generation, you might need to use NavMeshComponents package
        
        // Example placeholder of what runtime generation might look like:
        // NavMeshSurface surface = FindObjectOfType<NavMeshSurface>();
        // if (surface != null)
        // {
        //     surface.BuildNavMesh();
        // }
    }
    
    // Method called from Menu to start a new game
    public static void StartNewGame(int difficulty)
    {
        // Store difficulty in PlayerPrefs for the game setup to read
        PlayerPrefs.SetInt("GameDifficulty", difficulty);
        PlayerPrefs.Save();
        
        // Load game scene
        SceneManager.LoadScene("GameScene");
    }
}