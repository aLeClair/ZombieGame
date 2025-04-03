using UnityEngine;

// Interface for interactable objects
public interface IInteractable
{
    void Interact(GameObject interactor);
    string GetInteractionPrompt();
}