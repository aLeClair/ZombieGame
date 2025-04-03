using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PostCombatUI : MonoBehaviour
{
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private GameObject nodeChoiceButtonPrefab;
    
    private RunManager runManager;
    private GameManager gameManager;
    
    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
        runManager = FindObjectOfType<RunManager>();
    }
    
    public void ShowRewards()
    {
        rewardPanel.SetActive(true);
        choicePanel.SetActive(false);
        
        // Populate rewards UI here
        // ...
        
        // Add a button to continue to choices
        // This button should call ContinueToChoices() when clicked
    }
    
    public void ContinueToChoices()
    {
        rewardPanel.SetActive(false);
        choicePanel.SetActive(true);
        
        // Clear previous choices
        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Get available nodes
        List<NodeData> availableNodes = runManager.GetAvailableNextNodes();
        
        // Create a button for each available node
        foreach (NodeData node in availableNodes)
        {
            GameObject buttonObj = Instantiate(nodeChoiceButtonPrefab, choiceContainer);
            NodeChoiceButton button = buttonObj.GetComponent<NodeChoiceButton>();
            button.Setup(node, this);
        }
    }
    
    public void SelectNode(string nodeId)
    {
        // Set the selected node as current
        runManager.SetCurrentNode(nodeId);
        
        // Handle the node based on its type
        NodeData selectedNode = runManager.GetNodeById(nodeId);
        if (selectedNode != null)
        {
            switch (selectedNode.nodeType)
            {
                case NodeType.Combat:
                    // Reset the level for combat
                    gameManager.TransitionToState(GameState.Combat);
                    FindObjectOfType<LevelManager>().ResetLevelForCombat();
                    break;
                    
                case NodeType.Shop:
                    gameManager.TransitionToState(GameState.Shop);
                    // Show shop UI
                    break;
                    
                case NodeType.Event:
                    gameManager.TransitionToState(GameState.Event);
                    // Show event UI
                    break;
                    
                // Add other node types as needed
            }
        }
    }
}