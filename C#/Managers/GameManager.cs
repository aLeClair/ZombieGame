using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private int experience = 0;
    [SerializeField] private int gold = 100;
    [SerializeField] private int experienceToNextLevel = 100;
    [SerializeField] private float experienceScaling = 1.5f;
    
    [Header("Game State")]
    [SerializeField] private bool isPaused = false;
    [SerializeField] private bool isGameOver = false;
    [SerializeField] private bool isPlayerDead = false;
    [SerializeField] private bool isTowerDestroyed = false;
    [SerializeField] private bool isVictory = false;
    [SerializeField] private int roundsCompleted = 0;
    
    [Header("UI References")]
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject upgradeMenu;
    
    // References
    private WaveManager waveManager;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private TowerController towerController;
    
    // Events
    public delegate void ResourcesChangedHandler(int gold, int experience, int level);
    public event ResourcesChangedHandler OnResourcesChanged;
    
    public delegate void GameStateChangedHandler();
    public event GameStateChangedHandler OnGamePaused;
    public event GameStateChangedHandler OnGameResumed;
    public event GameStateChangedHandler OnGameOver;
    public event GameStateChangedHandler OnVictory;
    
    // Singleton instance
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        waveManager = FindObjectOfType<WaveManager>();
        playerController = FindObjectOfType<PlayerController>();
        playerHealth = FindObjectOfType<PlayerHealth>();
        towerController = FindObjectOfType<TowerController>();
        
        // Hide UI screens
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (upgradeMenu != null) upgradeMenu.SetActive(false);
        
        // Update resources UI
        UpdateResourcesUI();
        
        SceneManager.LoadScene("MapScene");
    }
    
    void Update()
    {
        // Pause/Resume
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver && !isVictory)
        {
            TogglePause();
        }
    }
    
    public void AddGold(int amount)
    {
        gold += amount;
        UpdateResourcesUI();
    }
    
    public bool SpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            UpdateResourcesUI();
            return true;
        }
        return false;
    }
    
    public void AddExperience(int amount)
    {
        experience += amount;
        
        // Check for level up
        while (experience >= experienceToNextLevel)
        {
            LevelUp();
        }
        
        UpdateResourcesUI();
    }
    
    private void LevelUp()
    {
        experience -= experienceToNextLevel;
        playerLevel++;
        
        // Increase experience requirement for next level
        experienceToNextLevel = Mathf.RoundToInt(experienceToNextLevel * experienceScaling);
        
        // Show level up notification
        Debug.Log("Level Up! Now level " + playerLevel);
        
        // Optionally auto-upgrade something
        // e.g., IncrementPlayerHealth();
    }
    
    private void UpdateResourcesUI()
    {
        // Trigger event for UI updates
        if (OnResourcesChanged != null)
        {
            OnResourcesChanged(gold, experience, playerLevel);
        }
    }
    
    public void PlayerDied()
    {
        isPlayerDead = true;
        GameOver();
    }
    
    public void TowerDestroyed()
    {
        isTowerDestroyed = true;
        GameOver();
    }
    
    public void GameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        
        // Show game over screen
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
        }
        
        // Pause game time
        Time.timeScale = 0;
        
        // Trigger event
        if (OnGameOver != null)
        {
            OnGameOver();
        }
    }
    
    public void GameWon()
    {
        if (isVictory) return;
        
        isVictory = true;
        
        // Show victory screen
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(true);
        }
        
        // Pause game time
        Time.timeScale = 0;
        
        // Trigger event
        if (OnVictory != null)
        {
            OnVictory();
        }
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        
        if (isPaused)
        {
            // Pause game
            Time.timeScale = 0;
            
            // Show pause menu
            if (pauseMenu != null)
            {
                pauseMenu.SetActive(true);
            }
            
            // Trigger event
            if (OnGamePaused != null)
            {
                OnGamePaused();
            }
        }
        else
        {
            // Resume game
            Time.timeScale = 1;
            
            // Hide pause menu
            if (pauseMenu != null)
            {
                pauseMenu.SetActive(false);
            }
            
            // Hide upgrade menu if open
            if (upgradeMenu != null)
            {
                upgradeMenu.SetActive(false);
            }
            
            // Trigger event
            if (OnGameResumed != null)
            {
                OnGameResumed();
            }
        }
    }
    
    public void RestartGame()
    {
        // Reset time scale
        Time.timeScale = 1;
        
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void QuitToMainMenu()
    {
        // Reset time scale
        Time.timeScale = 1;
        
        // Load the main menu scene
        SceneManager.LoadScene("MainMenu");
    }
    
    public void RoundComplete(int roundNumber)
    {
        roundsCompleted = roundNumber;
        
        // Show upgrade menu
        if (upgradeMenu != null)
        {
            upgradeMenu.SetActive(true);
        }
    }
    
    public void ReturnToMap()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.LoadMapScene();
        }
        else
        {
            // Fallback if RunManager not available
            SceneManager.LoadScene("MapScene");
        }
    }
    
    public bool IsPlayerDead()
    {
        return isPlayerDead;
    }
    
    public bool IsTowerDestroyed()
    {
        return isTowerDestroyed;
    }
    
    public int GetPlayerLevel()
    {
        return playerLevel;
    }
    
    public int GetExperience()
    {
        return experience;
    }
    
    public int GetExperienceToNextLevel()
    {
        return experienceToNextLevel;
    }
    
    public int GetGold()
    {
        return gold;
    }
    
    public int GetRoundsCompleted()
    {
        return roundsCompleted;
    }
    
    public bool IsGameOver()
    {
        return isGameOver;
    }
    
    public bool IsVictory()
    {
        return isVictory;
    }
    
    public bool IsPaused()
    {
        return isPaused;
    }
}