using UnityEngine;
using System.Collections.Generic;

public class UpgradeManager : MonoBehaviour
{
    [System.Serializable]
    public class Upgrade
    {
        public string name;
        public string description;
        public UpgradeType type;
        public int baseCost;
        public float costIncreaseFactor = 1.5f;
        public float baseValue;
        public float valueIncreaseFactor = 1.0f;
        public int maxLevel = 5;
        public int currentLevel = 0;
        public Sprite icon;
    }
    
    public enum UpgradeType
    {
        PlayerHealth,
        PlayerDamage,
        PlayerSpeed,
        PlayerShield,
        TowerHealth,
        TowerDamage,
        TowerRange,
        TowerAttackSpeed
    }
    
    [Header("Available Upgrades")]
    [SerializeField] private List<Upgrade> playerUpgrades = new List<Upgrade>();
    [SerializeField] private List<Upgrade> towerUpgrades = new List<Upgrade>();
    
    // References
    private GameManager gameManager;
    private PlayerHealth playerHealth;
    private PlayerController playerController;
    private TowerController towerController;
    private WeaponManager weaponManager;
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        playerHealth = FindObjectOfType<PlayerHealth>();
        playerController = FindObjectOfType<PlayerController>();
        towerController = FindObjectOfType<TowerController>();
        weaponManager = FindObjectOfType<WeaponManager>();
    }
    
    public List<Upgrade> GetPlayerUpgrades()
    {
        return playerUpgrades;
    }
    
    public List<Upgrade> GetTowerUpgrades()
    {
        return towerUpgrades;
    }
    
    public int GetUpgradeCost(Upgrade upgrade)
    {
        if (upgrade.currentLevel >= upgrade.maxLevel)
        {
            return -1; // Indicates max level reached
        }
        
        // Calculate cost based on current level
        return Mathf.RoundToInt(upgrade.baseCost * Mathf.Pow(upgrade.costIncreaseFactor, upgrade.currentLevel));
    }
    
    public float GetUpgradeValue(Upgrade upgrade)
    {
        // Calculate value based on current level
        return upgrade.baseValue * Mathf.Pow(upgrade.valueIncreaseFactor, upgrade.currentLevel);
    }
    
    public float GetNextUpgradeValue(Upgrade upgrade)
    {
        if (upgrade.currentLevel >= upgrade.maxLevel)
        {
            return GetUpgradeValue(upgrade); // Already at max
        }
        
        // Calculate value for next level
        return upgrade.baseValue * Mathf.Pow(upgrade.valueIncreaseFactor, upgrade.currentLevel + 1);
    }
    
    public bool PurchaseUpgrade(Upgrade upgrade)
    {
        if (gameManager == null) return false;
        
        int cost = GetUpgradeCost(upgrade);
        
        // Check if we can upgrade
        if (cost < 0 || upgrade.currentLevel >= upgrade.maxLevel)
        {
            Debug.Log("Upgrade already at max level");
            return false;
        }
        
        // Try to spend gold
        if (gameManager.SpendGold(cost))
        {
            // Increase level
            upgrade.currentLevel++;
            
            // Apply upgrade
            ApplyUpgrade(upgrade);
            
            return true;
        }
        
        return false;
    }
    
    private void ApplyUpgrade(Upgrade upgrade)
    {
        float value = GetUpgradeValue(upgrade);
        
        switch (upgrade.type)
        {
            case UpgradeType.PlayerHealth:
                if (playerHealth != null)
                {
                    playerHealth.UpgradeMaxHealth(value);
                }
                break;
                
            case UpgradeType.PlayerDamage:
                if (weaponManager != null)
                {
                    // Implement weapon damage upgrade
                    // weaponManager.UpgradeWeaponDamage(value);
                }
                break;
                
            case UpgradeType.PlayerSpeed:
                if (playerController != null)
                {
                    // Implement move speed upgrade
                    // playerController.UpgradeSpeed(value);
                }
                break;
                
            case UpgradeType.PlayerShield:
                if (playerHealth != null)
                {
                    playerHealth.UpgradeMaxShield(value);
                }
                break;
                
            case UpgradeType.TowerHealth:
                if (towerController != null)
                {
                    towerController.UpgradeTower(TowerUpgradeType.Health);
                }
                break;
                
            case UpgradeType.TowerDamage:
                if (towerController != null)
                {
                    towerController.UpgradeTower(TowerUpgradeType.Damage);
                }
                break;
                
            case UpgradeType.TowerRange:
                if (towerController != null)
                {
                    towerController.UpgradeTower(TowerUpgradeType.Range);
                }
                break;
                
            case UpgradeType.TowerAttackSpeed:
                if (towerController != null)
                {
                    towerController.UpgradeTower(TowerUpgradeType.AttackSpeed);
                }
                break;
        }
    }
    
    // This method would be called from UI buttons
    public void PurchaseUpgradeByIndex(int index, bool isTowerUpgrade)
    {
        List<Upgrade> upgradeList = isTowerUpgrade ? towerUpgrades : playerUpgrades;
        
        if (index >= 0 && index < upgradeList.Count)
        {
            PurchaseUpgrade(upgradeList[index]);
        }
    }
    
    // Reset all upgrades (for new game)
    public void ResetAllUpgrades()
    {
        foreach (Upgrade upgrade in playerUpgrades)
        {
            upgrade.currentLevel = 0;
        }
        
        foreach (Upgrade upgrade in towerUpgrades)
        {
            upgrade.currentLevel = 0;
        }
    }
}