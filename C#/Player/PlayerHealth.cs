using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float healthRegenRate = 5f; // Health per second when regenerating
    [SerializeField] private float regenDelay = 5f; // Seconds after taking damage before regen starts
    
    [Header("Shield Settings")]
    [SerializeField] private float maxShield = 50f;
    [SerializeField] private float currentShield = 0f;
    [SerializeField] private float shieldRechargeRate = 10f; // Shield per second when recharging
    [SerializeField] private float shieldRechargeDelay = 3f; // Seconds after taking damage before recharge
    
    [Header("Effects")]
    [SerializeField] private GameObject damageEffect;
    [SerializeField] private GameObject healEffect;
    
    // State tracking
    private float lastDamageTime = -999f;
    private bool isDead = false;
    
    // References
    private GameManager gameManager;
    private PlayerController playerController;
    
    // Events
    public delegate void HealthChangedHandler(float currentHealth, float maxHealth);
    public event HealthChangedHandler OnHealthChanged;
    
    public delegate void ShieldChangedHandler(float currentShield, float maxShield);
    public event ShieldChangedHandler OnShieldChanged;
    
    void Start()
    {
        currentHealth = maxHealth;
        gameManager = FindObjectOfType<GameManager>();
        playerController = GetComponent<PlayerController>();
        
        // Trigger initial health/shield update
        if (OnHealthChanged != null)
        {
            OnHealthChanged(currentHealth, maxHealth);
        }
        
        if (OnShieldChanged != null)
        {
            OnShieldChanged(currentShield, maxShield);
        }
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Health regeneration
        if (Time.time > lastDamageTime + regenDelay && currentHealth < maxHealth)
        {
            RegenerateHealth(healthRegenRate * Time.deltaTime);
        }
        
        // Shield recharge
        if (Time.time > lastDamageTime + shieldRechargeDelay && currentShield < maxShield)
        {
            RechargeShield(shieldRechargeRate * Time.deltaTime);
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        // Update damage time for regen/recharge delays
        lastDamageTime = Time.time;
        
        // Damage shield first
        float remainingDamage = damage;
        if (currentShield > 0)
        {
            if (currentShield >= remainingDamage)
            {
                currentShield -= remainingDamage;
                remainingDamage = 0;
            }
            else
            {
                remainingDamage -= currentShield;
                currentShield = 0;
            }
            
            // Trigger shield changed event
            if (OnShieldChanged != null)
            {
                OnShieldChanged(currentShield, maxShield);
            }
        }
        
        // Apply any remaining damage to health
        if (remainingDamage > 0)
        {
            currentHealth -= remainingDamage;
            
            // Spawn damage effect
            if (damageEffect != null)
            {
                Instantiate(damageEffect, transform.position, Quaternion.identity);
            }
            
            // Trigger health changed event
            if (OnHealthChanged != null)
            {
                OnHealthChanged(currentHealth, maxHealth);
            }
            
            // Check for death
            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }
    
    public void HealHealth(float amount)
    {
        if (isDead) return;
        
        // Apply healing
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        // Spawn heal effect
        if (healEffect != null)
        {
            Instantiate(healEffect, transform.position, Quaternion.identity);
        }
        
        // Trigger health changed event
        if (OnHealthChanged != null)
        {
            OnHealthChanged(currentHealth, maxHealth);
        }
    }
    
    private void RegenerateHealth(float amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        // Trigger health changed event (don't trigger too often for performance)
        if (Time.frameCount % 10 == 0 && OnHealthChanged != null)
        {
            OnHealthChanged(currentHealth, maxHealth);
        }
    }
    
    public void AddShield(float amount)
    {
        if (isDead) return;
        
        // Apply shield
        currentShield = Mathf.Min(currentShield + amount, maxShield);
        
        // Trigger shield changed event
        if (OnShieldChanged != null)
        {
            OnShieldChanged(currentShield, maxShield);
        }
    }
    
    private void RechargeShield(float amount)
    {
        if (isDead) return;
        
        currentShield = Mathf.Min(currentShield + amount, maxShield);
        
        // Trigger shield changed event (don't trigger too often for performance)
        if (Time.frameCount % 10 == 0 && OnShieldChanged != null)
        {
            OnShieldChanged(currentShield, maxShield);
        }
    }
    
    private void Die()
    {
        isDead = true;
        
        // Disable controller
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Disable weapon manager
        WeaponManager weaponManager = GetComponent<WeaponManager>();
        if (weaponManager != null)
        {
            weaponManager.enabled = false;
        }
        
        // Trigger game over
        if (gameManager != null)
        {
            gameManager.PlayerDied();
        }
    }
    
    public void UpgradeMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount; // Also heal by the upgrade amount
        
        // Trigger health changed event
        if (OnHealthChanged != null)
        {
            OnHealthChanged(currentHealth, maxHealth);
        }
    }
    
    public void UpgradeMaxShield(float amount)
    {
        maxShield += amount;
        
        // Trigger shield changed event
        if (OnShieldChanged != null)
        {
            OnShieldChanged(currentShield, maxShield);
        }
    }
    
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
    
    public float GetShieldPercent()
    {
        return maxShield > 0 ? currentShield / maxShield : 0;
    }
    
    public bool IsDead()
    {
        return isDead;
    }
}