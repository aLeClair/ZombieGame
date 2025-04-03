using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NodeChoiceButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    
    private string nodeId;
    private PostCombatUI parentUI;
    
    public void Setup(NodeData node, PostCombatUI ui)
    {
        nodeId = node.id;
        parentUI = ui;
        
        // Set title based on node type
        switch (node.nodeType)
        {
            case NodeType.Combat:
                titleText.text = "Combat";
                descriptionText.text = "A wave of zombies approaches...";
                break;
                
            case NodeType.Shop:
                titleText.text = "Shop";
                descriptionText.text = "Purchase supplies and upgrades";
                break;
                
            case NodeType.Event:
                titleText.text = "Event";
                descriptionText.text = "Something unusual awaits...";
                break;
                
            // Add other node types as needed
        }
    }
    
    public void OnButtonClick()
    {
        // Call parent UI to handle selection
        parentUI.SelectNode(nodeId);
    }
}