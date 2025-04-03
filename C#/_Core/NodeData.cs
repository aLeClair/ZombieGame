using System.Collections.Generic;
using UnityEngine;

public enum NodeType
{
    Combat,
    Event,
    Shop,
    Boss
}

[System.Serializable]
public class NodeData
{
    public string id;
    public NodeType nodeType;
    public Vector2 position;
    public int level; // Distance from start (0 = start level, final level = boss)
    public List<string> connectedNodeIds = new List<string>();
    public bool isVisited;
    public bool isAccessible;
    
    // Additional data for specific node types
    public string eventId; // For event nodes
    public string shopId;  // For shop nodes
    public string combatId; // For combat scenarios
    
    public NodeData(string id, NodeType type, Vector2 position, int level)
    {
        this.id = id;
        this.nodeType = type;
        this.position = position;
        this.level = level;
        this.isVisited = false;
        this.isAccessible = false;
    }
}