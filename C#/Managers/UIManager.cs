using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private Image healthBar;
    [SerializeField] private Image shieldBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI experienceText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI waveCountText;
    [SerializeField] private TextMeshProUGUI zombieCountText;
    [SerializeField] private Image towerHealthBar;
    [SerializeField] private Image experienceBar;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI interactionText;
    
    [Header("Game Screens")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject upgradeMenu;
    [SerializeField] private GameObject buildMenu;
    
    [Header("Wave Information")]
    [SerializeField] private GameObject waveStartBanner;
    [SerializeField] private TextMeshProUGUI waveStartText;
    [SerializeField] private GameObject waveCompleteBanner;
    [SerializeField] private TextMeshProUGUI waveCompleteText;
    [SerializeField] private TextMeshProUGUI waveCountdownText;
    
    [Header("Upgrade Menu")]
    [SerializeField] private Transform playerUpgradeContainer;
    [SerializeField] private Transform towerUpgradeContainer;
    [SerializeField] private GameObject upgradeButtonPrefab;
    
    [Header("Build Menu")]
    [SerializeField] private Transform buildButtonContainer;
    [SerializeField] private GameObject buildButtonPrefab;
    
    [Header("Bow Crosshair")]
    [SerializeField] private GameObject bowCrosshair;  // Keep this, but leave unassigned
    private RectTransform topReticle;
    private RectTransform rightReticle;
    private RectTransform bottomReticle; 
    private RectTransform leftReticle;
    [SerializeField] private float maxSpread = 20f;  // Maximum distance from center
    [SerializeField] private float minSpread = 5f;   // Minimum distance when fully drawn
    
    // References
    private GameManager gameManager;
    private PlayerHealth playerHealth;
    private TowerController towerController;
    private WaveManager waveManager;
    private UpgradeManager upgradeManager;
    private BuildingSystem buildingSystem;
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        playerHealth = FindObjectOfType<PlayerHealth>();
        towerController = FindObjectOfType<TowerController>();
        waveManager = FindObjectOfType<WaveManager>();
        upgradeManager = FindObjectOfType<UpgradeManager>();
        buildingSystem = FindObjectOfType<BuildingSystem>();
        
        // Initialize UI
        InitializeUI();
        
        // Subscribe to events
        SubscribeToEvents();
        
        Debug.Log("UIManager started. Looking for BowCrosshair...");
        FindCrosshairReferences();
        if (bowCrosshair != null)
        {
            Debug.Log("Found BowCrosshair. Setting active: false");
            bowCrosshair.SetActive(false);  // Force it off on start
        }
    }
    
    private void InitializeUI()
    {
        // Hide game screens
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (upgradeMenu != null) upgradeMenu.SetActive(false);
        if (buildMenu != null) buildMenu.SetActive(false);
        
        // Hide wave banners
        if (waveStartBanner != null) waveStartBanner.SetActive(false);
        if (waveCompleteBanner != null) waveCompleteBanner.SetActive(false);
        
        // Hide interaction prompt
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        
        // Initialize upgrade buttons
        if (upgradeManager != null)
        {
            InitializeUpgradeButtons();
        }
        
        // Initialize build buttons
        if (buildingSystem != null)
        {
            InitializeBuildButtons();
        }
        
        // Update initial UI values
        UpdateResourcesUI(0, 0, 1); // Default values
    }
    
    private void SubscribeToEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnResourcesChanged += UpdateResourcesUI;
            gameManager.OnGamePaused += ShowPauseMenu;
            gameManager.OnGameResumed += HidePauseMenu;
            gameManager.OnGameOver += ShowGameOverScreen;
            gameManager.OnVictory += ShowVictoryScreen;
        }
        
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthUI;
            playerHealth.OnShieldChanged += UpdateShieldUI;
        }
        
        if (towerController != null)
        {
            towerController.OnTowerHealthChanged += UpdateTowerHealthUI;
        }
        
        if (waveManager != null)
        {
            waveManager.OnWaveStart += ShowWaveStartBanner;
            waveManager.OnWaveEnd += ShowWaveCompleteBanner;
            waveManager.OnWaveCountdownChanged += UpdateWaveCountdown;
            waveManager.OnZombieCountChanged += UpdateZombieCount;
        }
    }
    
    // UI Update Methods
    
    private void UpdateResourcesUI(int gold, int experience, int level)
    {
        if (goldText != null) goldText.text = gold.ToString();
        if (levelText != null) levelText.text = "Level " + level.ToString();
        
        if (experienceText != null && gameManager != null)
        {
            experienceText.text = experience + " / " + gameManager.GetExperienceToNextLevel();
        }
        
        if (experienceBar != null && gameManager != null)
        {
            float expPercent = (float)experience / gameManager.GetExperienceToNextLevel();
            experienceBar.fillAmount = expPercent;
        }
    }
    
    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = currentHealth / maxHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = Mathf.Ceil(currentHealth) + " / " + Mathf.Ceil(maxHealth);
        }
    }
    
    private void UpdateShieldUI(float currentShield, float maxShield)
    {
        if (shieldBar != null)
        {
            shieldBar.fillAmount = maxShield > 0 ? currentShield / maxShield : 0;
            shieldBar.gameObject.SetActive(maxShield > 0);
        }
    }
    
    private void UpdateTowerHealthUI(float currentHealth, float maxHealth)
    {
        if (towerHealthBar != null)
        {
            towerHealthBar.fillAmount = currentHealth / maxHealth;
        }
    }
    
    private void UpdateWaveCountdown(float remainingTime)
    {
        if (waveCountdownText != null)
        {
            waveCountdownText.text = "Next Wave: " + Mathf.Ceil(remainingTime).ToString();
            waveCountdownText.gameObject.SetActive(true);
        }
    }
    
    private void UpdateZombieCount(int remaining, int total)
    {
        if (zombieCountText != null)
        {
            zombieCountText.text = "Zombies: " + remaining + " / " + total;
        }
    }
    
    private void ShowWaveStartBanner(int waveNumber, int totalWaves)
    {
        if (waveStartBanner != null && waveStartText != null)
        {
            waveStartText.text = "WAVE " + waveNumber + " STARTING";
            waveStartBanner.SetActive(true);
            
            // Hide after a delay
            StartCoroutine(HideBannerAfterDelay(waveStartBanner, 3f));
        }
        
        if (waveCountText != null)
        {
            waveCountText.text = "Wave: " + waveNumber + " / " + totalWaves;
        }
        
        // Hide countdown during wave
        if (waveCountdownText != null)
        {
            waveCountdownText.gameObject.SetActive(false);
        }
    }
    
    private void ShowWaveCompleteBanner(int waveNumber, int totalWaves)
    {
        if (waveCompleteBanner != null && waveCompleteText != null)
        {
            waveCompleteText.text = "WAVE " + waveNumber + " COMPLETE";
            waveCompleteBanner.SetActive(true);
            
            // Hide after a delay
            StartCoroutine(HideBannerAfterDelay(waveCompleteBanner, 3f));
        }
        
        // Show upgrade menu
        if (upgradeMenu != null)
        {
            upgradeMenu.SetActive(true);
            
            // Refresh upgrade buttons (costs might have changed)
            InitializeUpgradeButtons();
        }
    }
    
    private IEnumerator HideBannerAfterDelay(GameObject banner, float delay)
    {
        yield return new WaitForSeconds(delay);
        banner.SetActive(false);
    }
    
    // Menu Methods
    
    private void ShowPauseMenu()
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }
    }
    
    private void HidePauseMenu()
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
    }
    
    private void ShowGameOverScreen()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
        }
    }
    
    private void ShowVictoryScreen()
    {
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(true);
        }
    }
    
    // Button handlers (called from UI)
    
    public void ResumeGame()
    {
        if (gameManager != null)
        {
            gameManager.TogglePause();
        }
    }
    
    public void RestartGame()
    {
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
    }
    
    public void QuitToMainMenu()
    {
        if (gameManager != null)
        {
            gameManager.QuitToMainMenu();
        }
    }
    
    public void StartNextWave()
    {
        if (waveManager != null)
        {
            waveManager.StartNextWave();
        }
        
        // Hide upgrade menu
        if (upgradeMenu != null)
        {
            upgradeMenu.SetActive(false);
        }
    }
    
    public void ToggleBuildMenu()
    {
        if (buildMenu != null)
        {
            bool isActive = buildMenu.activeSelf;
            buildMenu.SetActive(!isActive);
        }
    }
    
    // Upgrade Menu
    
    private void InitializeUpgradeButtons()
    {
        if (upgradeManager == null || playerUpgradeContainer == null || towerUpgradeContainer == null) return;
        
        // Clear existing buttons
        foreach (Transform child in playerUpgradeContainer)
        {
            Destroy(child.gameObject);
        }
        
        foreach (Transform child in towerUpgradeContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create player upgrade buttons
        List<UpgradeManager.Upgrade> playerUpgrades = upgradeManager.GetPlayerUpgrades();
        for (int i = 0; i < playerUpgrades.Count; i++)
        {
            CreateUpgradeButton(playerUpgrades[i], i, playerUpgradeContainer, false);
        }
        
        // Create tower upgrade buttons
        List<UpgradeManager.Upgrade> towerUpgrades = upgradeManager.GetTowerUpgrades();
        for (int i = 0; i < towerUpgrades.Count; i++)
        {
            CreateUpgradeButton(towerUpgrades[i], i, towerUpgradeContainer, true);
        }
    }
    
    private void CreateUpgradeButton(UpgradeManager.Upgrade upgrade, int index, Transform container, bool isTowerUpgrade)
    {
        if (upgradeButtonPrefab == null) return;
        
        GameObject buttonObj = Instantiate(upgradeButtonPrefab, container);
        
        // Set button text
        TextMeshProUGUI nameText = buttonObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = upgrade.name;
        }
        
        TextMeshProUGUI descText = buttonObj.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
        if (descText != null)
        {
            string valueText = upgradeManager.GetUpgradeValue(upgrade).ToString("F1");
            string nextValueText = upgradeManager.GetNextUpgradeValue(upgrade).ToString("F1");
            descText.text = upgrade.description + "\nCurrent: " + valueText + " → Next: " + nextValueText;
        }
        
        TextMeshProUGUI costText = buttonObj.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
        if (costText != null)
        {
            int cost = upgradeManager.GetUpgradeCost(upgrade);
            if (cost > 0)
            {
                costText.text = cost + " Gold";
            }
            else
            {
                costText.text = "MAX LEVEL";
            }
        }
        
        TextMeshProUGUI levelText = buttonObj.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        if (levelText != null)
        {
            levelText.text = "Level " + upgrade.currentLevel + "/" + upgrade.maxLevel;
        }
        
        // Set button icon
        Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && upgrade.icon != null)
        {
            iconImage.sprite = upgrade.icon;
        }
        
        // Set button click handler
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            int buttonIndex = index; // Capture for lambda
            bool buttonIsTowerUpgrade = isTowerUpgrade;
            
            button.onClick.AddListener(() => {
                if (upgradeManager.PurchaseUpgrade(upgrade))
                {
                    // Refresh all upgrade buttons after purchase
                    InitializeUpgradeButtons();
                }
            });
            
            // Disable button if at max level or not enough gold
            if (upgrade.currentLevel >= upgrade.maxLevel || 
                (gameManager != null && gameManager.GetGold() < upgradeManager.GetUpgradeCost(upgrade)))
            {
                button.interactable = false;
            }
        }
    }
    
    // Build Menu
    
    private void InitializeBuildButtons()
    {
        if (buildingSystem == null || buildButtonContainer == null) return;
        
        // Clear existing buttons
        foreach (Transform child in buildButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create building buttons
        List<BuildingSystem.BuildingType> buildings = buildingSystem.GetAvailableBuildings();
        for (int i = 0; i < buildings.Count; i++)
        {
            CreateBuildButton(buildings[i], i, buildButtonContainer);
        }
    }
    
    private void CreateBuildButton(BuildingSystem.BuildingType building, int index, Transform container)
    {
        if (buildButtonPrefab == null) return;
        
        GameObject buttonObj = Instantiate(buildButtonPrefab, container);
        
        // Set button text
        TextMeshProUGUI nameText = buttonObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = building.name;
        }
        
        TextMeshProUGUI descText = buttonObj.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
        if (descText != null)
        {
            descText.text = building.description;
        }
        
        TextMeshProUGUI costText = buttonObj.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
        if (costText != null)
        {
            costText.text = building.cost + " Gold";
        }
        
        // Set button icon
        Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && building.icon != null)
        {
            iconImage.sprite = building.icon;
        }
        
        // Set button click handler
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            int buttonIndex = index; // Capture for lambda
            
            button.onClick.AddListener(() => {
                buildingSystem.SelectBuildingByIndex(buttonIndex);
                
                // Hide build menu after selection
                if (buildMenu != null)
                {
                    buildMenu.SetActive(false);
                }
            });
            
            // Disable button if not enough gold
            if (gameManager != null && gameManager.GetGold() < building.cost)
            {
                button.interactable = false;
            }
        }
    }
    
    // Interaction UI
    
    public void ShowInteractionPrompt(string promptText)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
            
            if (interactionText != null)
            {
                interactionText.text = promptText;
            }
        }
    }
    
    public void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    private void FindCrosshairReferences()
    {
        if (bowCrosshair == null)
        {
            bowCrosshair = GameObject.FindGameObjectWithTag("BowCrosshair");
        
            if (bowCrosshair != null)
            {
                // Find the reticle parts by name or tag
                bowCrosshair.SetActive(false);
                
                Transform crosshairTransform = bowCrosshair.transform;
                topReticle = crosshairTransform.Find("TopReticle")?.GetComponent<RectTransform>();
                rightReticle = crosshairTransform.Find("RightReticle")?.GetComponent<RectTransform>();
                bottomReticle = crosshairTransform.Find("BottomReticle")?.GetComponent<RectTransform>();
                leftReticle = crosshairTransform.Find("LeftReticle")?.GetComponent<RectTransform>();
            }
        }
    }
    
    // Add a method to show/hide the crosshair
    public void ShowBowCrosshair(bool show)
    {
        FindCrosshairReferences();
        if (bowCrosshair != null)
        {
            bowCrosshair.SetActive(show);
        }
    }
    
    public void UpdateCrosshairSpread(float powerPercentage)
    {
        FindCrosshairReferences();
        if (topReticle == null || rightReticle == null || 
            bottomReticle == null || leftReticle == null)
            return;
    
        // Calculate current spread based on power percentage (0-1)
        float currentSpread = Mathf.Lerp(maxSpread, minSpread, powerPercentage);
    
        // Update reticle positions
        topReticle.anchoredPosition = new Vector2(0, currentSpread);
        rightReticle.anchoredPosition = new Vector2(currentSpread, 0);
        bottomReticle.anchoredPosition = new Vector2(0, -currentSpread);
        leftReticle.anchoredPosition = new Vector2(-currentSpread, 0);
    }
}