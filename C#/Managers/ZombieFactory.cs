using UnityEngine;
using System.Collections.Generic;

public class ZombieFactory : MonoBehaviour
{
    [System.Serializable]
    public class ZombieTypeInfo
    {
        public ZombieController.ZombieType zombieType;
        public GameObject zombiePrefab;
        public float spawnWeight = 1f;
        public int minWaveToAppear = 1;
    }
    
    [Header("Zombie Prefabs")]
    [SerializeField] private List<ZombieTypeInfo> zombieTypes = new List<ZombieTypeInfo>();
    [SerializeField] private GameObject defaultZombiePrefab; // Fallback if no prefab found
    
    [Header("Difficulty Scaling")]
    [SerializeField] private float healthScaling = 0.1f; // +10% health per wave
    [SerializeField] private float damageScaling = 0.05f; // +5% damage per wave
    
    // Singleton instance
    private static ZombieFactory _instance;
    public static ZombieFactory Instance { get { return _instance; } }
    
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
        
        // Ensure zombie types are set up
        ValidateZombieTypes();
    }
    
    private void ValidateZombieTypes()
    {
        // Make sure at least one zombie type exists
        if (zombieTypes.Count == 0 && defaultZombiePrefab != null)
        {
            // Add default zombie
            ZombieTypeInfo defaultType = new ZombieTypeInfo();
            defaultType.zombieType = ZombieController.ZombieType.Shambler;
            defaultType.zombiePrefab = defaultZombiePrefab;
            defaultType.spawnWeight = 1f;
            defaultType.minWaveToAppear = 1;
            
            zombieTypes.Add(defaultType);
        }
    }
    
    public GameObject SpawnZombie(Vector3 position, int currentWave)
    {
        // Get a random zombie type based on weights
        ZombieTypeInfo zombieInfo = GetRandomZombieType(currentWave);
        
        if (zombieInfo == null || zombieInfo.zombiePrefab == null)
        {
            Debug.LogError("Failed to get valid zombie type or prefab!");
            return null;
        }
        
        // Spawn the zombie
        GameObject zombie = Instantiate(zombieInfo.zombiePrefab, position, Quaternion.identity);
        
        // Apply difficulty scaling
        ApplyDifficultyScaling(zombie, currentWave);
        
        return zombie;
    }
    
    private ZombieTypeInfo GetRandomZombieType(int currentWave)
    {
        // Filter zombies by what's available at current wave
        List<ZombieTypeInfo> availableTypes = new List<ZombieTypeInfo>();
        float totalWeight = 0f;
        
        foreach (ZombieTypeInfo zombieInfo in zombieTypes)
        {
            if (zombieInfo.minWaveToAppear <= currentWave)
            {
                availableTypes.Add(zombieInfo);
                totalWeight += zombieInfo.spawnWeight;
            }
        }
        
        if (availableTypes.Count == 0)
        {
            Debug.LogWarning("No zombie types available for wave " + currentWave);
            if (zombieTypes.Count > 0)
                return zombieTypes[0]; // Fallback to first zombie type
            
            return null;
        }
        
        // Select random zombie based on weights
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (ZombieTypeInfo zombieInfo in availableTypes)
        {
            currentWeight += zombieInfo.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return zombieInfo;
            }
        }
        
        // Fallback
        return availableTypes[0];
    }
    
    private void ApplyDifficultyScaling(GameObject zombie, int currentWave)
    {
        if (currentWave <= 1) return; // No scaling on first wave
        
        ZombieController zombieController = zombie.GetComponent<ZombieController>();
        if (zombieController == null) return;
        
        // Get base stats
        float baseHealth = 0f;
        float baseDamage = 0f;
        
        // Base stats by zombie type
        switch (zombieController.GetZombieType())
        {
            case ZombieController.ZombieType.Shambler:
                baseHealth = 100f;
                baseDamage = 10f;
                break;
            case ZombieController.ZombieType.Bruiser:
                baseHealth = 250f;
                baseDamage = 20f;
                break;
            case ZombieController.ZombieType.Jumper:
                baseHealth = 80f;
                baseDamage = 15f;
                break;
            case ZombieController.ZombieType.Sneaker:
                baseHealth = 80f;
                baseDamage = 20f;
                break;
            case ZombieController.ZombieType.Spitter:
                baseHealth = 70f;
                baseDamage = 8f;
                break;
        }
        
        // Calculate scaled stats
        float scaleFactor = 1f + ((currentWave - 1) * healthScaling);
        float healthScaled = baseHealth * scaleFactor;
        
        scaleFactor = 1f + ((currentWave - 1) * damageScaling);
        float damageScaled = baseDamage * scaleFactor;
        
        // Apply scaled stats
        zombieController.ScaleStats(healthScaled, damageScaled);
    }
}