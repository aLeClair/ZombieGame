using UnityEngine;
using System.Collections.Generic;

public class PlayerInteractionSystem : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactionMask;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private Transform cameraTransform;
    
    [Header("UI References")]
    [SerializeField] private GameObject interactionPrompt;
    
    private IInteractable currentTarget;
    
    // Update is called once per frame
    void Update()
    {
        // Look for interactable objects
        CheckForInteractable();
        
        // Handle interaction input
        if (Input.GetKeyDown(interactionKey) && currentTarget != null)
        {
            currentTarget.Interact(this.gameObject);
        }
    }
    
    void CheckForInteractable()
    {
        RaycastHit hit;
        
        // Cast ray from camera
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, interactionRange, interactionMask))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            
            if (interactable != null)
            {
                // New interactable found
                if (currentTarget != interactable)
                {
                    currentTarget = interactable;
                    ShowInteractionPrompt(true, interactable.GetInteractionPrompt());
                }
            }
            else
            {
                // Not looking at an interactable anymore
                if (currentTarget != null)
                {
                    currentTarget = null;
                    ShowInteractionPrompt(false);
                }
            }
        }
        else
        {
            // Nothing hit, clear current target
            if (currentTarget != null)
            {
                currentTarget = null;
                ShowInteractionPrompt(false);
            }
        }
    }
    
    void ShowInteractionPrompt(bool show, string text = "")
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(show);
            
            // You would need to add code here to update UI text element
            // with the provided text parameter if you have a text component
            
            // Example:
            // interactionPrompt.GetComponentInChildren<Text>().text = text;
        }
    }
}