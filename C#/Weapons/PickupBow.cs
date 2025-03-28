using UnityEngine;

public class PickupBow : MonoBehaviour, IInteractable
{
    [Header("Bow Settings")]
    [SerializeField] private GameObject bowPrefab;
    [SerializeField] private float respawnTime = 30f;
    
    private bool canBePickedUp = true;
    private float respawnTimer = 0f;
    
    void Update()
    {
        // Handle respawn timer if bow has been picked up
        if (!canBePickedUp)
        {
            respawnTimer += Time.deltaTime;
            
            if (respawnTimer >= respawnTime)
            {
                Respawn();
            }
        }
    }
    
    public void Interact(GameObject interactor)
    {
        Debug.Log("Interact called on PickupBow");
        if (!canBePickedUp) return;
        
        // Look for weapon manager on the interactor
        WeaponManager weaponManager = interactor.GetComponent<WeaponManager>();
        
        if (weaponManager != null)
        {
            // Try to equip the bow
            bool equipped = weaponManager.EquipBow(bowPrefab);
            
            if (equipped)
            {
                // Hide pickup and start respawn timer
                canBePickedUp = false;
                respawnTimer = 0f;
                
                // Hide the bow pickup
                // Either disable renderer or whole object depending on your needs
                GetComponent<Renderer>().enabled = false;
                GetComponent<Collider>().enabled = false;
            }
        }
    }
    
    public string GetInteractionPrompt()
    {
        return "Press E to pick up Bow";
    }
    
    void Respawn()
    {
        canBePickedUp = true;
        GetComponent<Renderer>().enabled = true;
        GetComponent<Collider>().enabled = true;
    }
}