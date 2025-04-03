using UnityEngine;
using System.Collections.Generic;

public class LootManager : MonoBehaviour
{
    [System.Serializable]
    public class LootDrop
    {
        public GameObject lootPrefab;
        public float dropChance; // 0-1 chance of this item dropping
        public int minLevel; // Minimum player level for this drop
    }
    
    [Header("Loot Settings")]
    [SerializeField] private List<LootDrop> possibleLoot = new List<LootDrop>();
    [SerializeField] private GameObject goldPickupPrefab;
    [SerializeField] private GameObject healthPickupPrefab;
    
    [Header("Drop Settings")]
    [SerializeField] private float goldDropChance = 0.5f;
    [SerializeField] private float healthDropChance = 0.3f;
    [SerializeField] private int minGoldAmount = 5;
    [SerializeField] private int maxGoldAmount = 20;
    [SerializeField] private float minHealthAmount = 10f;
    [SerializeField] private float maxHealthAmount = 25f;
    
    // Reference
    private GameManager gameManager;
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }
    
    public void SpawnLoot(Vector3 position)
    {
        // Determine what type of loot to drop
        float lootRoll = Random.value;
        
        if (lootRoll < 0.2f) // 20% chance to drop a weapon/item
        {
            DropRandomItem(position);
        }
        else if (lootRoll < 0.2f + goldDropChance) // Gold drop
        {
            DropGold(position);
        }
        else if (lootRoll < 0.2f + goldDropChance + healthDropChance) // Health drop
        {
            DropHealth(position);
        }
        // Otherwise, no drop
    }
    
    private void DropRandomItem(Vector3 position)
    {
        if (possibleLoot.Count == 0 || gameManager == null) return;
        
        int playerLevel = gameManager.GetPlayerLevel();
        
        // Filter loot by player level
        List<LootDrop> availableLoot = possibleLoot.FindAll(
            item => item.minLevel <= playerLevel
        );
        
        if (availableLoot.Count == 0) return;
        
        // Calculate total weight
        float totalWeight = 0f;
        foreach (LootDrop loot in availableLoot)
        {
            totalWeight += loot.dropChance;
        }
        
        // Select random item based on weights
        float randomValue = Random.value * totalWeight;
        float accumulatedWeight = 0f;
        
        foreach (LootDrop loot in availableLoot)
        {
            accumulatedWeight += loot.dropChance;
            
            if (randomValue <= accumulatedWeight)
            {
                // Spawn the selected loot
                SpawnLootObject(loot.lootPrefab, position);
                return;
            }
        }
        
        // Fallback - spawn the first item
        if (availableLoot.Count > 0)
        {
            SpawnLootObject(availableLoot[0].lootPrefab, position);
        }
    }
    
    private void DropGold(Vector3 position)
    {
        if (goldPickupPrefab == null) return;
        
        // Create gold pickup with random amount
        GameObject goldObject = SpawnLootObject(goldPickupPrefab, position);
        
        // Set gold amount
        GoldPickup goldPickup = goldObject.GetComponent<GoldPickup>();
        if (goldPickup != null)
        {
            int goldAmount = Random.Range(minGoldAmount, maxGoldAmount + 1);
            goldPickup.SetAmount(goldAmount);
        }
    }
    
    private void DropHealth(Vector3 position)
    {
        if (healthPickupPrefab == null) return;
        
        // Create health pickup with random amount
        GameObject healthObject = SpawnLootObject(healthPickupPrefab, position);
        
        // Set health amount
        HealthPickup healthPickup = healthObject.GetComponent<HealthPickup>();
        if (healthPickup != null)
        {
            float healthAmount = Random.Range(minHealthAmount, maxHealthAmount);
            healthPickup.SetAmount(healthAmount);
        }
    }
    
    private GameObject SpawnLootObject(GameObject prefab, Vector3 position)
    {
        // Add slight randomness to position
        Vector3 randomOffset = new Vector3(
            Random.Range(-0.5f, 0.5f),
            0f,
            Random.Range(-0.5f, 0.5f)
        );
        
        // Ensure loot is above ground
        position.y = 0.5f; // Assuming your ground is at y=0
        
        // Instantiate the loot with a small bounce
        GameObject lootObject = Instantiate(prefab, position + randomOffset, Quaternion.identity);
        
        // Add small upward force if rigidbody exists
        Rigidbody rb = lootObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(Vector3.up * 2f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 1f, ForceMode.Impulse);
        }
        
        return lootObject;
    }
}

// Example pickup scripts

public class GoldPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private int goldAmount = 10;
    
    public void SetAmount(int amount)
    {
        goldAmount = amount;
    }
    
    public void Interact(GameObject interactor)
    {
        // Find game manager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            // Add gold to player
            gameManager.AddGold(goldAmount);
            
            // Play pickup effect/sound
            
            // Destroy the pickup
            Destroy(gameObject);
        }
    }
    
    public string GetInteractionPrompt()
    {
        return "Press E to collect " + goldAmount + " gold";
    }
}

public class HealthPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private float healthAmount = 25f;
    
    public void SetAmount(float amount)
    {
        healthAmount = amount;
    }
    
    public void Interact(GameObject interactor)
    {
        // Find player health component
        PlayerHealth playerHealth = interactor.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // Heal the player
            playerHealth.HealHealth(healthAmount);
            
            // Play pickup effect/sound
            
            // Destroy the pickup
            Destroy(gameObject);
        }
    }
    
    public string GetInteractionPrompt()
    {
        return "Press E to pickup Health +" + healthAmount.ToString("F0");
    }
}