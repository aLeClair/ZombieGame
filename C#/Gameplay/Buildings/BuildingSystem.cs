using UnityEngine;
using System.Collections.Generic;

public class BuildingSystem : MonoBehaviour
{
    [System.Serializable]
    public class BuildingType
    {
        public string name;
        public GameObject prefab;
        public int cost;
        public string description;
        public Sprite icon;
    }

    [Header("Building Settings")] [SerializeField]
    private List<BuildingType> availableBuildings = new List<BuildingType>();

    [SerializeField] private float placementRange = 10f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Material validPlacementMaterial;
    [SerializeField] private Material invalidPlacementMaterial;

    [Header("Input Settings")] [SerializeField]
    private KeyCode buildModeKey = KeyCode.B;

    [SerializeField] private KeyCode cancelKey = KeyCode.Escape;
    [SerializeField] private KeyCode rotateBuildingKey = KeyCode.R;

    // State
    private bool buildModeActive = false;
    private BuildingType selectedBuilding = null;
    private GameObject placementPreview = null;
    private bool canPlace = false;
    private int currentRotation = 0;

    // References
    private Camera playerCamera;
    private GameManager gameManager;
    private Material originalMaterial;

    void Start()
    {
        playerCamera = Camera.main;
        gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        // Toggle build mode
        if (Input.GetKeyDown(buildModeKey))
        {
            ToggleBuildMode();
        }

        // Cancel building
        if (buildModeActive && Input.GetKeyDown(cancelKey))
        {
            CancelBuildMode();
        }

        // Handle building placement
        if (buildModeActive && selectedBuilding != null)
        {
            UpdatePlacementPreview();

            // Rotate building
            if (Input.GetKeyDown(rotateBuildingKey))
            {
                RotatePreview();
            }

            // Place building
            if (Input.GetMouseButtonDown(0) && canPlace)
            {
                PlaceBuilding();
            }
        }
    }

    private void ToggleBuildMode()
    {
        buildModeActive = !buildModeActive;

        if (buildModeActive)
        {
            // Show building menu UI - this would be implemented elsewhere

            // Select first building by default if there are buildings available
            if (availableBuildings.Count > 0)
            {
                SelectBuilding(availableBuildings[0]);
            }
        }
        else
        {
            CancelBuildMode();
        }
    }

    public void SelectBuilding(BuildingType building)
    {
        selectedBuilding = building;

        // Remove any existing preview
        if (placementPreview != null)
        {
            Destroy(placementPreview);
        }

        // Create new preview
        if (selectedBuilding != null)
        {
            placementPreview = Instantiate(selectedBuilding.prefab);

            // Set preview material/transparency
            SetPreviewMaterials(placementPreview);

            // Reset rotation
            currentRotation = 0;
        }
    }

    private void SetPreviewMaterials(GameObject preview)
    {
        Renderer[] renderers = preview.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // Save original material if not already saved
            if (originalMaterial == null && renderer.material != null)
            {
                originalMaterial = renderer.material;
            }

            // Create material instances for the preview
            Material[] previewMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                previewMaterials[i] = invalidPlacementMaterial;
            }

            renderer.materials = previewMaterials;
        }

        // Disable any colliders in the preview
        Collider[] colliders = preview.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private void UpdatePlacementPreview()
    {
        if (placementPreview == null || playerCamera == null) return;

        // Cast ray from camera to get placement position
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, placementRange, groundLayer))
        {
            // Position the preview at the hit point
            placementPreview.transform.position = hit.point;

            // Check if we can place here (no collisions, within range, etc.)
            canPlace = CheckPlacementValidity(hit.point);

            // Update preview material based on placement validity
            UpdatePreviewMaterials(canPlace);
        }
        else
        {
            // Ray didn't hit valid ground
            canPlace = false;

            // Hide or disable preview when not over valid ground
            if (placementPreview.activeSelf)
            {
                placementPreview.SetActive(false);
            }
        }
    }

    private bool CheckPlacementValidity(Vector3 position)
    {
        if (placementPreview == null) return false;

        // Check distance from player
        float distanceToPlayer = Vector3.Distance(position, transform.position);
        if (distanceToPlayer > placementRange)
        {
            return false;
        }

        // Activate preview if it was disabled
        if (!placementPreview.activeSelf)
        {
            placementPreview.SetActive(true);
        }

        // Check for collisions with other objects
        Collider[] colliders = placementPreview.GetComponentsInChildren<Collider>();

        // Temporarily enable colliders for overlap check
        foreach (Collider collider in colliders)
        {
            collider.enabled = true;
            collider.isTrigger = true;
        }

        // Check for overlapping colliders
        bool isOverlapping = false;
        foreach (Collider collider in colliders)
        {
            Collider[] overlappingColliders = Physics.OverlapBox(
                collider.bounds.center,
                collider.bounds.extents,
                collider.transform.rotation
            );

            foreach (Collider overlappingCollider in overlappingColliders)
            {
                // Ignore colliders from the preview itself
                if (!overlappingCollider.transform.IsChildOf(placementPreview.transform) &&
                    overlappingCollider.gameObject != placementPreview)
                {
                    isOverlapping = true;
                    break;
                }
            }

            if (isOverlapping) break;
        }

        // Disable colliders again
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        // Can place if not overlapping and player has enough gold
        return !isOverlapping && gameManager != null &&
               gameManager.GetGold() >= selectedBuilding.cost;
    }

    private void UpdatePreviewMaterials(bool isValid)
    {
        if (placementPreview == null) return;

        Renderer[] renderers = placementPreview.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            Material[] mats = new Material[renderer.materials.Length];
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                mats[i] = isValid ? validPlacementMaterial : invalidPlacementMaterial;
            }

            renderer.materials = mats;
        }
    }

    private void RotatePreview()
    {
        if (placementPreview == null) return;

        // Rotate in 90-degree increments
        currentRotation = (currentRotation + 90) % 360;
        placementPreview.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
    }

    private void PlaceBuilding()
    {
        if (placementPreview == null || !canPlace || gameManager == null) return;

        // Check if player has enough gold
        if (gameManager.SpendGold(selectedBuilding.cost))
        {
            // Create the actual building
            GameObject newBuilding = Instantiate(
                selectedBuilding.prefab,
                placementPreview.transform.position,
                placementPreview.transform.rotation
            );

            // Reset materials to original (non-preview)
            Renderer[] renderers = newBuilding.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                // Restore original materials if available
                if (originalMaterial != null)
                {
                    Material[] originalMaterials = new Material[renderer.materials.Length];
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        originalMaterials[i] = originalMaterial;
                    }

                    renderer.materials = originalMaterials;
                }
            }

            // Exit build mode or continue placing
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // Continue placing same building type (shift held)
                // Just create a new preview to replace the one that was just placed
                SelectBuilding(selectedBuilding);
            }
            else
            {
                // Exit build mode after placing
                CancelBuildMode();
            }
        }
    }

    private void CancelBuildMode()
    {
        buildModeActive = false;

        // Destroy preview if exists
        if (placementPreview != null)
        {
            Destroy(placementPreview);
            placementPreview = null;
        }

        selectedBuilding = null;

        // Hide building UI
        // This would be implemented elsewhere
    }

    // Public method to allow UI to select buildings
    public void SelectBuildingByIndex(int index)
    {
        if (index >= 0 && index < availableBuildings.Count)
        {
            SelectBuilding(availableBuildings[index]);
        }
    }

    public List<BuildingType> GetAvailableBuildings()
    {
        return availableBuildings;
    }

    public bool IsBuildModeActive()
    {
        return buildModeActive;
    }
}