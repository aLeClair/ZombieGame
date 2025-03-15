using UnityEngine;
using System.Collections.Generic;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private KeyCode equipKey = KeyCode.Alpha1;
    
    [Header("Bow Settings")]
    [SerializeField] private GameObject bowPrefab;
    
    private UIManager uiManager;
    private GameObject equippedBow;
    private bool hasBow = false;
    private bool isWeaponEquipped = false;
    
    void Start()
    {
        // Create the weapon socket if it doesn't exist
        if (weaponSocket == null)
        {
            // Try to find camera for attaching the weapon socket
            Camera playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                GameObject socket = new GameObject("WeaponSocket");
                socket.transform.SetParent(playerCamera.transform);
                socket.transform.localPosition = new Vector3(0.2f, -0.15f, 0.4f);
                socket.transform.localRotation = Quaternion.Euler(0, -5f, 0);
                weaponSocket = socket.transform;
            }
            else
            {
                Debug.LogError("Could not find player camera to attach weapon socket!");
            }
        }
        
        uiManager = FindObjectOfType<UIManager>();
        
        // Make sure crosshair starts hidden
        if (uiManager != null)
        {
            uiManager.ShowBowCrosshair(false);
        }
    
        // Check initial weapon state
        isWeaponEquipped = false;
    }
    
    void Update()
    {
        // Toggle bow equipment
        if (Input.GetKeyDown(equipKey) && hasBow)
        {
            ToggleEquippedWeapon();
        }
    }
    
    public bool EquipBow(GameObject bowPrefabToUse)
    {
        // If we don't have a bow yet
        if (!hasBow)
        {
            // Store reference to this bow prefab
            bowPrefab = bowPrefabToUse;
        
            // Mark that we have a bow now
            hasBow = true;
        
            // Automatically equip it
            EquipWeapon();
        
            return true;
        }
    
        return false;
    }
    
    private void ToggleEquippedWeapon()
    {
        if (isWeaponEquipped)
        {
            UnequipWeapon();
        }
        else
        {
            EquipWeapon();
        }
    }
    
    private void EquipWeapon()
    {
        if (weaponSocket == null) return;
        
        if (hasBow && bowPrefab != null)
        {
            // Instantiate the bow at the weapon socket
            equippedBow = Instantiate(bowPrefab, weaponSocket);
            equippedBow.transform.localPosition = new Vector3(0.2f, -0.12f, 0.15f);
            equippedBow.transform.localRotation = Quaternion.Euler(-10f, 95f, 5f);
            
            // Get the bow controller component
            BowController bowController = equippedBow.GetComponent<BowController>();
            if (bowController != null)
            {
                bowController.Initialize(gameObject, weaponSocket);
            }
            
            isWeaponEquipped = true;
            
            // Show bow crosshair when bow is equipped
            if (CrosshairController.Instance != null)
            {
                CrosshairController.Instance.ShowBowCrosshair(true);
            }
        }
    }
    
    private void UnequipWeapon()
    {
        if (equippedBow != null)
        {
            Destroy(equippedBow);
            equippedBow = null;
        }
        
        isWeaponEquipped = false;
        
        // Hide crosshair when bow is unequipped
        if (CrosshairController.Instance != null)
            CrosshairController.Instance.SetDefaultCrosshair();
        }
}