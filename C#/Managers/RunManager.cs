using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    // Singleton pattern for easy access
    private static RunManager _instance;
    public static RunManager Instance { get { return _instance; } }
    public List<NodeData> allNodes = new List<NodeData>();
    public string currentNodeId;
    
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
    
    public void GenerateMap()
    {
        // Clear existing nodes
        allNodes.Clear();
        
        // Generate map structure
        // This should create nodes and connections
        // Example basic implementation:
        
        // Create a simple map with 3 levels
        for (int level = 0; level < 3; level++)
        {
            // Create 2-3 nodes per level
            int nodesInThisLevel = Random.Range(2, 4);
            
            for (int i = 0; i < nodesInThisLevel; i++)
            {
                NodeData newNode = new NodeData();
                newNode.id = $"node_{level}_{i}";
                newNode.level = level;
                newNode.position = new Vector2(level * 200, i * 150);
                
                // Randomly assign node type
                int typeRand = Random.Range(0, 3);
                if (level == 2) // Last level is always boss
                {
                    newNode.nodeType = NodeType.Boss;
                }
                else if (typeRand == 0)
                {
                    newNode.nodeType = NodeType.Combat;
                }
                else if (typeRand == 1)
                {
                    newNode.nodeType = NodeType.Shop;
                }
                else
                {
                    newNode.nodeType = NodeType.Event;
                }
                
                allNodes.Add(newNode);
            }
        }
        
        // Connect nodes between levels
        for (int level = 0; level < 2; level++) // Stop at second-to-last level
        {
            List<NodeData> currentLevelNodes = allNodes.FindAll(node => node.level == level);
            List<NodeData> nextLevelNodes = allNodes.FindAll(node => node.level == level + 1);
            
            // Connect each node in current level to at least one node in next level
            foreach (NodeData currentNode in currentLevelNodes)
            {
                int connectionsCount = Random.Range(1, nextLevelNodes.Count + 1);
                for (int i = 0; i < connectionsCount; i++)
                {
                    if (i < nextLevelNodes.Count)
                    {
                        currentNode.connectedNodeIds.Add(nextLevelNodes[i].id);
                    }
                }
            }
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
    
    // This method marks the first node as the current node
    public void SetInitialNodeAsCurrent()
    {
        // Assuming the first node in level 0 is the starting point
        NodeData startNode = allNodes.Find(node => node.level == 0);
        if (startNode != null)
        {
            currentNodeId = startNode.id;
            startNode.isVisited = false; // Not visited yet
            startNode.isAccessible = true;
        
            // Force this to be a combat node for the first encounter
            startNode.nodeType = NodeType.Combat;
        }
    }

// After a node is completed, mark it as visited and update accessible nodes
    public void CompleteCurrentNode()
    {
        NodeData currentNode = GetNodeById(currentNodeId);
        if (currentNode != null)
        {
            currentNode.isVisited = true;
        
            // Mark connected nodes as accessible
            foreach (string connectedId in currentNode.connectedNodeIds)
            {
                NodeData connectedNode = GetNodeById(connectedId);
                if (connectedNode != null)
                {
                    connectedNode.isAccessible = true;
                }
            }
        }
    }
    
    // Get available next nodes that are accessible but not visited
    public List<NodeData> GetAvailableNextNodes()
    {
        List<NodeData> availableNodes = new List<NodeData>();
    
        NodeData currentNode = GetNodeById(currentNodeId);
        if (currentNode != null)
        {
            // Get connected nodes that are accessible and not visited
            foreach (string connectedId in currentNode.connectedNodeIds)
            {
                NodeData connectedNode = GetNodeById(connectedId);
                if (connectedNode != null && connectedNode.isAccessible && !connectedNode.isVisited)
                {
                    availableNodes.Add(connectedNode);
                }
            }
        }
    
        return availableNodes;
    }

// Set a node as the current node
    public void SetCurrentNode(string nodeId)
    {
        currentNodeId = nodeId;
    }

// Get a node by ID
    public NodeData GetNodeById(string id)
    {
        return allNodes.Find(node => node.id == id);
    }
}