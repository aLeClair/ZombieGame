using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    // Singleton pattern for easy access
    private static RunManager _instance;
    public static RunManager Instance { get { return _instance; } }
    
    [Header("Run Settings")]
    [SerializeField] private int startingGold = 100;
    [SerializeField] private int startingTowerHealth = 1000;
    
    // Basic run state
    private int currentGold;
    private int currentTowerHealth;
    private List<NodeData> mapNodes = new List<NodeData>();
    private NodeData currentNode;
    
    private MapData currentMap;
    private bool mapInitialized = false;
    [SerializeField] private int mapLevels = 5;
    [SerializeField] private int minPathsPerLevel = 2;
    [SerializeField] private int maxPathsPerLevel = 4;
    
    private void Awake()
    {
        // Simple singleton setup
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        StartNewRun();
    }
    
    public void StartNewRun()
    {
        // Initialize run values
        currentGold = startingGold;
        currentTowerHealth = startingTowerHealth;
    
        // Create a random seed based on time
        float seed = System.DateTime.Now.Ticks % 10000 / 10000f;
    
        // Generate the map
        currentMap = MapGenerator.GenerateMap(mapLevels, minPathsPerLevel, maxPathsPerLevel, seed);
        mapInitialized = true;
    
        Debug.Log($"Map generated with {currentMap.nodes.Count} nodes");
        foreach (NodeData node in currentMap.nodes)
        {
            Debug.Log($"Node: {node.id}, Type: {node.nodeType}, Position: ({node.position.x}, {node.position.y}), Level: {node.level}");
        }
    }
    
    public void SelectNode(string nodeId)
    {
        if (!mapInitialized) return;
    
        NodeData selectedNode = currentMap.GetNodeById(nodeId);
    
        if (selectedNode != null && selectedNode.isAccessible && !selectedNode.isVisited)
        {
            // Mark this node as visited and update accessibility
            currentMap.MarkNodeVisited(nodeId);
        
            Debug.Log("Selected node: " + nodeId + " of type " + selectedNode.nodeType);
        
            // Load the appropriate scene based on node type
            switch (selectedNode.nodeType)
            {
                case NodeType.Combat:
                    // You could pass combatId to customize the combat
                    LoadCombatScene();
                    break;
                case NodeType.Event:
                    // You could pass eventId to customize the event
                    Debug.Log("Loading event... (not implemented yet)");
                    break;
                case NodeType.Shop:
                    Debug.Log("Loading shop... (not implemented yet)");
                    break;
                case NodeType.Boss:
                    LoadCombatScene(); // For now use same combat scene
                    break;
            }
        }
        else
        {
            Debug.Log("Cannot select node: " + nodeId + " - either not found, already visited, or not accessible");
        }
    }
    
    // Getters for state values
    public int GetGold() { return currentGold; }
    
    public int GetTowerHealth() { return currentTowerHealth; }
    
    public List<NodeData> GetMapNodes()
    {
        if (mapInitialized)
        {
            return currentMap.nodes;
        }
        else
        {
            Debug.LogWarning("Map not initialized yet!");
            return new List<NodeData>();
        }
    }
    
    public NodeData GetCurrentNode() { return currentNode; }
    
    // Methods to modify state
    public void AddGold(int amount)
    {
        currentGold += amount;
        Debug.Log("Added " + amount + " gold. New total: " + currentGold);
    }
    
    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            Debug.Log("Spent " + amount + " gold. Remaining: " + currentGold);
            return true;
        }
        
        Debug.Log("Not enough gold to spend " + amount);
        return false;
    }
    
    public void ChangeTowerHealth(int amount)
    {
        currentTowerHealth += amount;
        Debug.Log("Tower health changed by " + amount + ". New health: " + currentTowerHealth);
        
        if (currentTowerHealth <= 0)
        {
            Debug.Log("Tower destroyed! Game over.");
            // Later we'll add game over logic
        }
    }
    
    public void LoadCombatScene()
    {
        Debug.Log("Loading combat scene...");
        SceneManager.LoadScene("CombatScene", LoadSceneMode.Single);  // Use your actual scene name
    }

    public void LoadMapScene()
    {
        Debug.Log("Loading map scene...");
        SceneManager.LoadScene("MapScene", LoadSceneMode.Single);  // Use your actual scene name
    }
}