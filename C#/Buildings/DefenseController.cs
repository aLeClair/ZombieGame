using UnityEngine;

public class DefenseController : MonoBehaviour
{
    [Header("Defense Settings")]
    [SerializeField] private DefenseType defenseType;
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private float currentHealth;
    [SerializeField] private GameObject damagedStatePrefab;
    [SerializeField] private GameObject destroyedEffectPrefab;
    
    [Header("Upgrade Settings")]
    [SerializeField] private int upgradeLevel = 1;
    [SerializeField] private float healthIncreasePerLevel = 100f;
    
    private bool isDamaged = false;
    private Material material;
    private Color originalColor;
    
    // Reference
    private GameManager gameManager;
    
    public enum DefenseType
    {
        Wall,
        Barricade,
        Trap
    }
    
    void Start()
    {
        currentHealth = maxHealth;
        gameManager = FindObjectOfType<GameManager>();
        
        // Get material for visual feedback
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            material = rend.material;
            originalColor = material.color;
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        // Visual feedback - flash red
        if (material != null)
        {
            material.color = Color.red;
            Invoke("ResetColor", 0.1f);
        }
        
        // Check if damaged state should be triggered
        if (!isDamaged && currentHealth <= maxHealth * 0.5f)
        {
            ShowDamagedState();
        }
        
        if (currentHealth <= 0)
        {
            DestroyDefense();
        }
    }
    
    private void ResetColor()
    {
        if (material != null)
        {
            material.color = originalColor;
        }
    }
    
    private void ShowDamagedState()
    {
        isDamaged = true;
        
        // Show visual damaged state
        if (damagedStatePrefab != null)
        {
            Instantiate(damagedStatePrefab, transform.position, transform.rotation, transform);
        }
        
        // Could also play a sound or particle effect
    }
    
    private void DestroyDefense()
    {
        // Spawn destruction effect
        if (destroyedEffectPrefab != null)
        {
            Instantiate(destroyedEffectPrefab, transform.position, transform.rotation);
        }
        
        // Destroy the defense
        Destroy(gameObject);
    }
    
    private void DropResources()
    {
        // Give the player some gold from destroyed defense
        if (gameManager != null)
        {
            gameManager.AddGold(5 * upgradeLevel);
        }
    }
    
    public void Upgrade()
    {
        upgradeLevel++;
        maxHealth += healthIncreasePerLevel;
        currentHealth += healthIncreasePerLevel;
        
        // Visual feedback for upgrade
        transform.localScale *= 1.1f; // Slight size increase
        
        // Reset damaged state if was damaged
        if (isDamaged && currentHealth > maxHealth * 0.5f)
        {
            isDamaged = false;
            // Remove damaged visual state
            // Implementation depends on how damage is visually represented
        }
    }
    
    public DefenseType GetDefenseType()
    {
        return defenseType;
    }
    
    public int GetUpgradeLevel()
    {
        return upgradeLevel;
    }
    
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
}