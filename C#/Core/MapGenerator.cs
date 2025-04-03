using System.Collections.Generic;
using UnityEngine;

public static class MapGenerator
{
    public static MapData GenerateMap(int numLevels, int minPathsPerLevel, int maxPathsPerLevel, float seed)
    {
        MapData mapData = new MapData();
        mapData.mapSeed = seed.ToString();
        
        // Use the seed for randomization
        Random.InitState(Mathf.FloorToInt(seed * 1000));
        
        // Visual spacing constants
        float horizontalSpacing = 200f; // Space between levels
        float horizontalOffset = -(numLevels * horizontalSpacing) / 2;
        
        float verticalSpacing = 100f;   // Space between paths in a level
        float verticalVariance = 40f;   // Random variance in vertical position
        
        // Track nodes by level for easier connection management
        List<List<NodeData>> nodesByLevel = new List<List<NodeData>>();
        
        // Initialize levels list
        for (int i = 0; i <= numLevels; i++)
        {
            nodesByLevel.Add(new List<NodeData>());
        }
        
        // Create starting node (level 0)
        NodeData startNode = new NodeData("node_0_0", NodeType.Combat, new Vector2(horizontalOffset, 0), 0);
        mapData.nodes.Add(startNode);
        nodesByLevel[0].Add(startNode);
        
        // Create intermediate level nodes
        for (int level = 1; level < numLevels; level++)
        {
            // Determine number of paths in this level
            int numPaths = Random.Range(minPathsPerLevel, maxPathsPerLevel + 1);
        
            // Create nodes for this level
            for (int i = 0; i < numPaths; i++)
            {
                // Determine node type based on level
                NodeType nodeType = DetermineNodeType(level, numLevels);
            
                // Calculate position - horizontal progression
                float yPos = (i - ((float)numPaths - 1) / 2) * 120f;
                float yVariance = Random.Range(-20f, 20f);
                Vector2 nodePos = new Vector2(horizontalOffset + (level * horizontalSpacing), yPos + yVariance);
            
                // Create the node
                string nodeId = $"node_{level}_{i}";
                NodeData node = new NodeData(nodeId, nodeType, nodePos, level);
            
                // Add to collections
                mapData.nodes.Add(node);
                nodesByLevel[level].Add(node);

            }
        }
        
        // Create boss node (final level)
        NodeData bossNode = new NodeData($"node_{numLevels}_0", NodeType.Boss, 
            new Vector2(horizontalOffset + (numLevels * horizontalSpacing), 0), numLevels);
        mapData.nodes.Add(bossNode);
        nodesByLevel[numLevels].Add(bossNode);
        
        // Connect nodes between adjacent levels
        ConnectNodes(nodesByLevel);
        
        // Mark starting node as accessible
        startNode.isAccessible = true;
        
        return mapData;
    }
    
    private static NodeType DetermineNodeType(int level, int maxLevel)
    {
        // Make first level always combat
        if (level == 1)
            return NodeType.Combat;
            
        // Weighted randomization for node types
        float rand = Random.value;
        
        if (rand < 0.6f)
            return NodeType.Combat;
        else if (rand < 0.85f)
            return NodeType.Event;
        else
            return NodeType.Shop;
    }
    
    private static void AssignNodeContent(NodeData node)
    {
        // Assign content IDs based on node type
        // These would correspond to specific events, shop setups, or combat scenarios
        switch (node.nodeType)
        {
            case NodeType.Combat:
                int combatId = Random.Range(1, 4); // 3 different combat scenarios
                node.combatId = $"combat_{combatId}";
                break;
                
            case NodeType.Event:
                int eventId = Random.Range(1, 6); // 5 different events
                node.eventId = $"event_{eventId}";
                break;
                
            case NodeType.Shop:
                node.shopId = "shop_standard"; // For now just one shop type
                break;
        }
    }
    
    private static void ConnectNodes(List<List<NodeData>> nodesByLevel)
    {
        for (int level = 0; level < nodesByLevel.Count - 1; level++)
        {
            List<NodeData> currentLevelNodes = nodesByLevel[level];
            List<NodeData> nextLevelNodes = nodesByLevel[level + 1];
            
            if (currentLevelNodes.Count == 1 && nextLevelNodes.Count == 1)
            {
                // Simple case: one-to-one connection
                ConnectTwoNodes(currentLevelNodes[0], nextLevelNodes[0]);
            }
            else if (currentLevelNodes.Count == 1)
            {
                // One node connects to many nodes
                foreach (NodeData nextNode in nextLevelNodes)
                {
                    ConnectTwoNodes(currentLevelNodes[0], nextNode);
                }
            }
            else if (nextLevelNodes.Count == 1)
            {
                // Many nodes connect to one node
                foreach (NodeData currentNode in currentLevelNodes)
                {
                    ConnectTwoNodes(currentNode, nextLevelNodes[0]);
                }
            }
            else
            {
                // Many-to-many connection with some randomness
                int maxConnections = Mathf.Min(3, nextLevelNodes.Count);
                
                foreach (NodeData currentNode in currentLevelNodes)
                {
                    // Each node connects to 1-3 nodes in next level
                    int connections = Random.Range(1, maxConnections + 1);
                    List<NodeData> availableTargets = new List<NodeData>(nextLevelNodes);
                    
                    for (int i = 0; i < connections && availableTargets.Count > 0; i++)
                    {
                        // Pick a random node from next level
                        int targetIndex = Random.Range(0, availableTargets.Count);
                        NodeData targetNode = availableTargets[targetIndex];
                        
                        ConnectTwoNodes(currentNode, targetNode);
                        
                        // Remove to avoid duplicate connections
                        availableTargets.RemoveAt(targetIndex);
                    }
                }
                
                // Ensure all nodes in next level have at least one connection
                foreach (NodeData nextNode in nextLevelNodes)
                {
                    if (nextNode.connectedNodeIds.Count == 0)
                    {
                        // Connect to a random node in current level
                        NodeData sourceNode = currentLevelNodes[Random.Range(0, currentLevelNodes.Count)];
                        ConnectTwoNodes(sourceNode, nextNode);
                    }
                }
            }
        }
    }
    
    private static void ConnectTwoNodes(NodeData sourceNode, NodeData targetNode)
    {
        sourceNode.connectedNodeIds.Add(targetNode.id);
        targetNode.connectedNodeIds.Add(sourceNode.id);
    }
}