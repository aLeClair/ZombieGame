using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TowerController : MonoBehaviour
{
    [Header("Tower Stats")]
    [SerializeField] private float maxHealth = 1000f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackSpeed = 1.5f; // Attacks per second
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject projectilePrefab;
    
    [Header("Tower Upgrades")]
    [SerializeField] private int towerLevel = 1;
    [SerializeField] private float healthUpgradeAmount = 250f;
    [SerializeField] private float damageUpgradeAmount = 10f;
    [SerializeField] private float rangeUpgradeAmount = 3f;
    [SerializeField] private float attackSpeedUpgradeAmount = 0.2f;
    
    [Header("Detection")]
    [SerializeField] private LayerMask enemyLayers;
    
    // References
    private List<Transform> targetsInRange = new List<Transform>();
    private float attackCooldown = 0f;
    private AudioSource audioSource;
    private GameManager gameManager;
    
    // Events
    public delegate void TowerHealthChangedHandler(float currentHealth, float maxHealth);
    public event TowerHealthChangedHandler OnTowerHealthChanged;
    
    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        gameManager = FindObjectOfType<GameManager>();
        
        if (attackPoint == null)
        {
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.SetParent(transform);
            attackPointObj.transform.localPosition = new Vector3(0, 5f, 0); // Top of tower
            attackPoint = attackPointObj.transform;
        }
        
        // Start checking for enemies
        StartCoroutine(FindEnemiesInRange());
    }
    
    void Update()
    {
        // Decrement attack cooldown
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }
        
        // Attack if enemies in range and cooldown expired
        if (targetsInRange.Count > 0 && attackCooldown <= 0)
        {
            AttackNearestEnemy();
        }
    }
    
    private IEnumerator FindEnemiesInRange()
    {
        while (true)
        {
            // Clear invalid targets
            targetsInRange.RemoveAll(t => t == null);
            
            // Find all enemies in range
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayers);
            
            // Add any new enemies to our target list
            foreach (var hitCollider in hitColliders)
            {
                Transform enemy = hitCollider.transform;
                if (!targetsInRange.Contains(enemy))
                {
                    targetsInRange.Add(enemy);
                }
            }
            
            // Wait a bit before checking again
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    private void AttackNearestEnemy()
    {
        if (targetsInRange.Count == 0) return;
        
        // Find nearest enemy
        Transform nearestEnemy = null;
        float closestDistance = float.MaxValue;
        
        foreach (Transform enemy in targetsInRange)
        {
            if (enemy == null) continue;
            
            float distance = Vector3.Distance(transform.position, enemy.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestEnemy = enemy;
            }
        }
        
        if (nearestEnemy != null)
        {
            
            // Fire projectile at enemy
            if (projectilePrefab != null)
            {
                Vector3 direction = (nearestEnemy.position - attackPoint.position).normalized;
                GameObject projectile = Instantiate(projectilePrefab, attackPoint.position, Quaternion.LookRotation(direction));
                
                // Get tower projectile component and initialize it
                TowerProjectile towerProjectile = projectile.GetComponent<TowerProjectile>();
                if (towerProjectile != null)
                {
                    towerProjectile.Initialize(attackDamage, direction, this.gameObject);
                }
            }
            
            // Reset attack cooldown
            attackCooldown = 1f / attackSpeed;
            
            // Play attack sound
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        // Trigger health changed event
        if (OnTowerHealthChanged != null)
        {
            OnTowerHealthChanged(currentHealth, maxHealth);
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        // Tower destroyed, trigger game over
        if (gameManager != null)
        {
            gameManager.TowerDestroyed();
        }
        
        // Optional: Play destruction animation/particles
    }
    
    public void UpgradeTower(TowerUpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case TowerUpgradeType.Health:
                maxHealth += healthUpgradeAmount;
                currentHealth += healthUpgradeAmount;
                break;
            case TowerUpgradeType.Damage:
                attackDamage += damageUpgradeAmount;
                break;
            case TowerUpgradeType.Range:
                attackRange += rangeUpgradeAmount;
                break;
            case TowerUpgradeType.AttackSpeed:
                attackSpeed += attackSpeedUpgradeAmount;
                break;
        }
        
        // Increase tower level
        towerLevel++;
        
        // Update UI if needed
        if (OnTowerHealthChanged != null)
        {
            OnTowerHealthChanged(currentHealth, maxHealth);
        }
    }
    
    public int GetTowerLevel()
    {
        return towerLevel;
    }
    
    // Draw attack range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
