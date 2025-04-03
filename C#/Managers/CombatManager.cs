using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button completeButton;
    
    [Header("Combat Settings")]
    [SerializeField] private int zombieCount = 5;
    [SerializeField] private float spawnDelay = 3f;
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private LayerMask groundLayer;
    
    private int zombiesRemaining;
    private bool combatActive = false;
    
    void Start()
    {
        // Set up the complete button
        if (completeButton != null)
        {
            completeButton.onClick.AddListener(CompleteCombat);
        }
        
        // Start the combat encounter
        StartCombat();
    }
    
    void StartCombat()
    {
        zombiesRemaining = zombieCount;
        combatActive = true;
        
        // Start spawning zombies
        InvokeRepeating("SpawnZombie", 2f, spawnDelay);
        
        Debug.Log($"Combat started: {zombieCount} zombies will spawn");
    }
    
    void SpawnZombie()
    {
        if (!combatActive || zombiesRemaining <= 0)
        {
            CancelInvoke("SpawnZombie");
            return;
        }
    
        // Select a random spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
    
        // Create spawn position with corrected height
        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.y = 1.0f; // Higher off the ground
    
        // Spawn the zombie at the corrected position
        if (zombiePrefab != null)
        {
            GameObject zombie = Instantiate(zombiePrefab, spawnPosition, spawnPoint.rotation);
        
            // Register for zombie death notification
            ZombieController zombieController = zombie.GetComponent<ZombieController>();
            if (zombieController != null)
            {
                zombieController.OnZombieDeath += () => OnZombieKilled();
            }
        
            zombiesRemaining--;
        
            Debug.Log($"Spawned zombie. Remaining to spawn: {zombiesRemaining}");
        }
    }
    
    void OnZombieKilled()
    {
        // Check if all zombies are killed
        int aliveZombies = FindObjectsOfType<ZombieController>().Length - 1; // -1 because this one is still counted
        
        Debug.Log($"Zombie killed. Alive zombies: {aliveZombies}");
        
        if (aliveZombies <= 0 && zombiesRemaining <= 0)
        {
            // All zombies are dead and no more will spawn
            VictoryAchieved();
        }
    }
    
    void VictoryAchieved()
    {
        combatActive = false;
        Debug.Log("Victory achieved! All zombies defeated.");
        
        // Enable the complete button if not already
        if (completeButton != null)
        {
            completeButton.gameObject.SetActive(true);
        }
    }
    
    public void CompleteCombat()
    {
        Debug.Log("Combat completed. Returning to map...");
        
        // Return to map scene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMap();
        }
        else
        {
            // Fallback if GameManager not found
            SceneManager.LoadScene("Map Scene");
        }
    }
}