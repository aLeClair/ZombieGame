using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapData
{
    public List<NodeData> nodes = new List<NodeData>();
    public string mapSeed; // For consistent regeneration if needed
    
    // Store the player's current position on the map
    public string currentNodeId;
    
    public NodeData GetNodeById(string id)
    {
        return nodes.Find(n => n.id == id);
    }
    
    public List<NodeData> GetAccessibleNodes()
    {
        return nodes.FindAll(n => n.isAccessible && !n.isVisited);
    }
    
    public void MarkNodeVisited(string nodeId)
    {
        NodeData node = GetNodeById(nodeId);
        if (node != null)
        {
            node.isVisited = true;
            currentNodeId = nodeId;
            
            // Make connected nodes accessible
            foreach (string connectedId in node.connectedNodeIds)
            {
                NodeData connectedNode = GetNodeById(connectedId);
                if (connectedNode != null)
                {
                    connectedNode.isAccessible = true;
                }
            }
        }
    }
    
    public NodeData GetStartingNode()
    {
        // Find the first node in level 0 (starting level)
        NodeData startNode = nodes.Find(n => n.level == 0);
        if (startNode != null)
        {
            startNode.isAccessible = true;
        }
        return startNode;
    }
}