using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MapUIController : MonoBehaviour
{
    [SerializeField] private GameObject nodeButtonPrefab;
    [SerializeField] private Transform nodeContainer;
    [SerializeField] private RectTransform mapView; // Reference to scrollable container
    
    // Node Colors
    [SerializeField] private Color combatColor = new Color(0.8f, 0.2f, 0.2f); // Red
    [SerializeField] private Color eventColor = new Color(0.8f, 0.6f, 0.2f); // Brown
    [SerializeField] private Color shopColor = new Color(0.2f, 0.8f, 0.2f); // Green
    [SerializeField] private Color bossColor = new Color(0.8f, 0.0f, 0.0f); // Deep Red
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f); // Grey
    [SerializeField] private Color highlightColor = Color.yellow; // Border highlight
    [SerializeField] private Color lineColor = new Color(0.7f, 0.7f, 0.7f, 0.5f); // Connection lines
    
    [Header("Node Icons")]
    [SerializeField] private Sprite combatIconSprite;
    [SerializeField] private Sprite eventIconSprite;
    [SerializeField] private Sprite shopIconSprite;
    [SerializeField] private Sprite bossIconSprite;
    
    // Alpha values
    [SerializeField] private float activeAlpha = 1.0f;
    [SerializeField] private float inactiveAlpha = 0.5f;
    [SerializeField] private float unreachableAlpha = 0.3f;
    
    [Header("Zoom Settings")]
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 2.0f;
    [SerializeField] private float zoomSpeed = 0.1f;
    private float currentZoom = 1.0f;
    
    private Dictionary<string, GameObject> nodeButtons = new Dictionary<string, GameObject>();
    private List<GameObject> connectionLines = new List<GameObject>();
    
    private void Start()
    {
        // Wait a frame to ensure RunManager has initialized
        Invoke("CreateMapUI", 0.1f);
    }
    
    private void Update()
    {
        // Check for mouse scroll wheel input
        float scrollDelta = Input.mouseScrollDelta.y;
        if (scrollDelta != 0)
        {
            // Adjust zoom level
            currentZoom += scrollDelta * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        
            // Apply zoom to node container
            nodeContainer.localScale = new Vector3(currentZoom, currentZoom, 1f);
        }
    }

    public void CreateMapUI()
    {
        // Add a background panel
        GameObject backgroundPanel = new GameObject("MapBackground");
        backgroundPanel.transform.SetParent(nodeContainer.parent);
        backgroundPanel.transform.SetAsFirstSibling(); // Put it behind everything
        
        // Add an image component for the background
        Image backgroundImage = backgroundPanel.AddComponent<Image>();
        backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark grey
        
        // Make it fill the parent
        RectTransform bgRect = backgroundPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // Clear existing nodes and connections
        foreach (Transform child in nodeContainer)
        {
            Destroy(child.gameObject);
        }

        nodeButtons.Clear();
        connectionLines.Clear();

        if (RunManager.Instance == null)
        {
            Debug.LogError("RunManager instance not found!");
            return;
        }

        // Get nodes from RunManager
        List<NodeData> nodes = RunManager.Instance.GetMapNodes();

        if (nodes == null || nodes.Count == 0)
        {
            Debug.LogError("No map nodes found!");
            return;
        }

        // Calculate map bounds for centering
        CalculateAndAdjustMapView(nodes);

        // Create lines to represent connections first (so they're behind nodes)
        CreateConnectionLines(nodes);

        // Create a button for each node
        foreach (NodeData node in nodes)
        {
            // Create button
            GameObject buttonObj = Instantiate(nodeButtonPrefab, nodeContainer);
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
    
            // Position node based on level (horizontal progression)
            rectTransform.anchoredPosition = node.position;
    
            // Debug positioning
            Debug.Log($"Placing node {node.id} at position: {rectTransform.anchoredPosition}");
    
            // Set node color
            Image nodeImage = buttonObj.GetComponent<Image>();
            if (nodeImage != null)
            {
                Color nodeColor = Color.white;
                switch (node.nodeType)
                {
                    case NodeType.Combat: nodeColor = new Color(0.8f, 0.2f, 0.2f); break; // Red
                    case NodeType.Event: nodeColor = new Color(0.8f, 0.6f, 0.2f); break; // Brown
                    case NodeType.Shop: nodeColor = new Color(0.2f, 0.8f, 0.2f); break; // Green
                    case NodeType.Boss: nodeColor = new Color(0.8f, 0.0f, 0.0f); break; // Deep Red
                }
                nodeImage.color = nodeColor;
            }
    
            // Set symbol text
            TextMeshProUGUI symbolText = buttonObj.transform.Find("Symbol")?.GetComponent<TextMeshProUGUI>();
            if (symbolText != null)
            {
                switch (node.nodeType)
                {
                    case NodeType.Combat: symbolText.text = "\u2694"; break;
                    case NodeType.Event: symbolText.text = "?"; break;
                    case NodeType.Shop: symbolText.text = "$"; break;
                    case NodeType.Boss: symbolText.text = "!"; break;
                    default: symbolText.text = ""; break;
                }
            }
            
            TextMeshProUGUI typeIconText = buttonObj.transform.Find("TypeIcon")?.GetComponent<TextMeshProUGUI>();
            if (typeIconText != null)
            {
                typeIconText.text = ""; // Just clear it if it's not being used properly
            }
    
            // Set up button click handler
            Button button = buttonObj.GetComponent<Button>();
            string nodeId = node.id;
            button.onClick.AddListener(() => {
                RunManager.Instance.SelectNode(nodeId);
                UpdateMapUI();
            });
            
            if (node.nodeType == NodeType.Boss)
            {
                // Make boss node larger
                rectTransform.sizeDelta = new Vector2(80, 80);
    
                // If there's a highlight, make it larger too
                Transform highlightTransform = buttonObj.transform.Find("Highlight");
                if (highlightTransform != null)
                {
                    highlightTransform.GetComponent<RectTransform>().sizeDelta = new Vector2(88, 88);
                }
            }
    
            nodeButtons[node.id] = buttonObj;
        }
    }

    private string GetNodeTypeText(NodeType nodeType)
    {
        switch (nodeType)
        {
            case NodeType.Combat: return "Combat";
            case NodeType.Event: return "Event";
            case NodeType.Shop: return "Shop";
            case NodeType.Boss: return "Boss Fight";
            default: return "";
        }
    }

    private void CalculateAndAdjustMapView(List<NodeData> nodes)
    {
        if (nodes.Count == 0 || mapView == null) return;
        
        // Find min/max positions to calculate bounds
        Vector2 minPos = nodes[0].position;
        Vector2 maxPos = nodes[0].position;
        
        foreach (NodeData node in nodes)
        {
            minPos.x = Mathf.Min(minPos.x, node.position.x);
            minPos.y = Mathf.Min(minPos.y, node.position.y);
            maxPos.x = Mathf.Max(maxPos.x, node.position.x);
            maxPos.y = Mathf.Max(maxPos.y, node.position.y);
        }
        
        // Calculate center and size
        Vector2 center = (minPos + maxPos) * 0.5f;
        Vector2 size = maxPos - minPos + new Vector2(200, 200); // Add padding
        
        // Center the map in the view
        nodeContainer.localPosition = new Vector3(-center.x, -center.y, 0);
        
        // Set container size to fit all nodes
        RectTransform containerRect = nodeContainer.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            containerRect.sizeDelta = size;
        }
    }
    
    private void CreateConnectionLines(List<NodeData> nodes)
    {
        foreach (NodeData node in nodes)
        {
            foreach (string connectedId in node.connectedNodeIds)
            {
                if (string.Compare(node.id, connectedId) < 0)
                {
                    NodeData connectedNode = nodes.Find(n => n.id == connectedId);
                    if (connectedNode != null)
                    {
                        // Pass the actual node positions directly
                        DrawConnectionLine(node.position, connectedNode.position);
                    }
                }
            }
        }
    }
    
    private GameObject DrawConnectionLine(Vector2 start, Vector2 end)
    {
        float nodeRadius = 30f; // Half of your 60x60 node size
    
        // Calculate direction vector
        Vector2 direction = (end - start).normalized;
    
        // Adjust start and end points to stop at circle edges
        Vector2 adjustedStart = start + direction * nodeRadius;
        Vector2 adjustedEnd = end - direction * nodeRadius;
    
        // Create a line GameObject
        GameObject lineObj = new GameObject("Connection");
        lineObj.transform.SetParent(nodeContainer);
    
        // Add a UI Image component to represent the line
        Image lineImage = lineObj.AddComponent<Image>();
        lineImage.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
    
        // Position and size the line
        RectTransform rectTransform = lineObj.GetComponent<RectTransform>();
    
        // Calculate line length and position
        float lineLength = Vector2.Distance(adjustedStart, adjustedEnd);
        Vector2 midPoint = (adjustedStart + adjustedEnd) / 2;
    
        // Set position and size
        rectTransform.anchoredPosition = midPoint;
        rectTransform.sizeDelta = new Vector2(lineLength, 2f);
    
        // Calculate rotation angle
        float angle = Mathf.Atan2(adjustedEnd.y - adjustedStart.y, adjustedEnd.x - adjustedStart.x) * Mathf.Rad2Deg;
        rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
    
        return lineObj;
    }
    
    public void UpdateMapUI()
    {
        if (RunManager.Instance == null) return;
    
        List<NodeData> nodes = RunManager.Instance.GetMapNodes();
    
        foreach (NodeData node in nodes)
        {
            if (nodeButtons.TryGetValue(node.id, out GameObject buttonObj))
            {
                // Get the highlight
                Transform highlightTransform = buttonObj.transform.Find("Highlight");
            
                // Get the node image
                Image nodeImage = buttonObj.GetComponent<Image>();
            
                // Get the button component
                Button button = buttonObj.GetComponent<Button>();
            
                if (node.isVisited)
                {
                    // Visited nodes - darkened
                    if (nodeImage != null)
                        nodeImage.color = GetNodeColorWithAlpha(node.nodeType, 0.5f);
                
                    if (highlightTransform != null)
                        highlightTransform.gameObject.SetActive(false);
                
                    if (button != null)
                        button.interactable = false;
                }
                else if (node.isAccessible)
                {
                    // Accessible nodes - fully colored with highlight
                    if (nodeImage != null)
                        nodeImage.color = GetNodeColor(node.nodeType);
                
                    if (highlightTransform != null)
                        highlightTransform.gameObject.SetActive(true);
                
                    if (button != null)
                        button.interactable = true;
                }
                else
                {
                    // Inaccessible nodes - faded
                    if (nodeImage != null)
                        nodeImage.color = GetNodeColorWithAlpha(node.nodeType, 0.3f);
                
                    if (highlightTransform != null)
                        highlightTransform.gameObject.SetActive(false);
                
                    if (button != null)
                        button.interactable = false;
                }
            }
        }
    }
    
    private Color GetNodeColor(NodeType nodeType)
    {
        switch (nodeType)
        {
            case NodeType.Combat: return new Color(0.8f, 0.2f, 0.2f); // Red
            case NodeType.Event: return new Color(0.8f, 0.6f, 0.2f); // Brown
            case NodeType.Shop: return new Color(0.2f, 0.8f, 0.2f); // Green
            case NodeType.Boss: return new Color(0.8f, 0.0f, 0.0f); // Deep Red
            default: return Color.gray;
        }
    }
    
    private Color GetNodeColorWithAlpha(NodeType nodeType, float alpha)
    {
        Color color = GetNodeColor(nodeType);
        color.a = alpha;
        return color;
    }
    
    private string GetNodeSymbol(NodeType nodeType)
    {
        switch (nodeType)
        {
            case NodeType.Combat: return "⚔"; // Combat symbol (crossed swords)
            case NodeType.Event: return "?"; // Event symbol (question mark)
            case NodeType.Shop: return "$"; // Shop symbol (dollar sign)
            case NodeType.Boss: return "!"; // Boss symbol (exclamation mark)
            default: return "";
        }
    }
    
    // Helper method to check if a node can eventually be reached
    private bool IsNodeEventuallyReachable(NodeData node, List<NodeData> allNodes)
    {
        // A node is eventually reachable if there's a path to it from any accessible node
        // For simplicity, we'll check if it's connected to an accessible or visited node
        foreach (string connectedId in node.connectedNodeIds)
        {
            NodeData connectedNode = allNodes.Find(n => n.id == connectedId);
            if (connectedNode != null && (connectedNode.isAccessible || connectedNode.isVisited))
            {
                return true;
            }
        }
        return false;
    }
}