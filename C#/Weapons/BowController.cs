using UnityEngine;
using System.Collections;

public class BowController : MonoBehaviour
{
    [Header("Bow Settings")]
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float maxDrawTime = 2f;
    [SerializeField] private float maxPower = 30f;
    [SerializeField] private float aimZoomFOV = 40f;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomSpeed = 10f;
    
    [Header("Animation")]
    [SerializeField] private Transform bowStringTransform;
    [SerializeField] private Transform bowHandleTransform;
    [SerializeField] private float maxDrawDistance = 0.3f;
    
    [Header("Damage Settings")]
    [SerializeField] private float minDamage = 5f;  // Damage with no draw
    [SerializeField] private float maxDamage = 30f; // Damage with full draw
    
    private UIManager uiManager;
    private bool isDrawing = false;
    private float currentDrawTime = 0f;
    private GameObject player;
    private Transform weaponHolder;
    private Camera playerCamera;
    private Vector3 originalStringPosition;
    private Vector3 normalPosition;
    private Vector3 aimPosition;
    private Quaternion normalRotation;
    private Quaternion aimRotation;
    
    private float bobTimer = 0f;
    private float bobAmount = 0.01f;
    
    // Called by WeaponManager when bow is equipped
    public void Initialize(GameObject playerObj, Transform weaponHolder)
    {
        this.player = playerObj;
        this.weaponHolder = weaponHolder;
        
        // Find player camera
        playerCamera = player.GetComponentInChildren<Camera>();
        
        // Initial positions
        normalPosition = new Vector3(0.2f, -0.12f, 0.15f);
        aimPosition = new Vector3(0.15f, -0.08f, 0.15f); // Slightly more centered but not blocking view

        normalRotation = Quaternion.Euler(-10f, 95f, 5f);
        aimRotation = Quaternion.Euler(-5f, 93f, 3f);
        
        // Store original string position
        if (bowStringTransform != null)
        {
            originalStringPosition = bowStringTransform.localPosition;
        }
        
        // Create arrow spawn point if it doesn't exist
        if (arrowSpawnPoint == null)
        {
            GameObject spawnPoint = new GameObject("ArrowSpawnPoint");
            spawnPoint.transform.SetParent(transform);
            spawnPoint.transform.localPosition = new Vector3(0, 0, 0.5f);
            spawnPoint.transform.localRotation = Quaternion.identity;
            
            arrowSpawnPoint = spawnPoint.transform;
        }
        
        // Get UI Manager reference
        uiManager = FindObjectOfType<UIManager>();
        
        // Show bow crosshair when bow is equipped
        if (CrosshairController.Instance != null) {
            CrosshairController.Instance.ShowBowCrosshair(true);
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Start drawing bow
        if (Input.GetMouseButtonDown(0) && !isDrawing)
        {
            isDrawing = true;
            currentDrawTime = 0f;
        }
        
        // Continue drawing bow
        if (Input.GetMouseButton(0) && isDrawing)
        {
            currentDrawTime += Time.deltaTime;
            currentDrawTime = Mathf.Clamp(currentDrawTime, 0f, maxDrawTime);
        
            // Calculate power percentage
            float powerPercentage = currentDrawTime / maxDrawTime;
        
            // Update crosshair spread
            if (CrosshairController.Instance != null)
            {
                CrosshairController.Instance.UpdateSpread(powerPercentage);
            }
            
            // Zoom in when aiming
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, aimZoomFOV, Time.deltaTime * zoomSpeed);
            }
            
            // Animate bow string pulling back
            AnimateBowDraw(powerPercentage);
            
            // Move to aim position while drawing
            transform.localPosition = Vector3.Lerp(transform.localPosition, aimPosition, Time.deltaTime * 8f);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, aimRotation, Time.deltaTime * 8f);
        }
        
        // Release arrow
        if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            float powerPercent = currentDrawTime / maxDrawTime;
            FireArrow(powerPercent);
            
            isDrawing = false;
            currentDrawTime = 0f;
            
            // Reset zoom
            if (playerCamera != null)
            {
                StartCoroutine(ResetZoom());
            }
            
            // Reset bow animation
            AnimateBowDraw(0);
        }
        
        // Right click to aim without drawing
        if (Input.GetMouseButton(1) && !isDrawing)
        {
            // Zoom in when aiming
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, aimZoomFOV, Time.deltaTime * zoomSpeed);
            }
            
            // Move to aim position
            transform.localPosition = Vector3.Lerp(transform.localPosition, aimPosition, Time.deltaTime * 8f);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, aimRotation, Time.deltaTime * 8f);
        }
        else if (!isDrawing && !Input.GetMouseButton(1))
        {
            // Reset zoom when not aiming or drawing
            if (playerCamera != null && playerCamera.fieldOfView != normalFOV)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, normalFOV, Time.deltaTime * zoomSpeed);
            }
            
            // Return to normal position when not aiming
            transform.localPosition = Vector3.Lerp(transform.localPosition, normalPosition, Time.deltaTime * 5f);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, normalRotation, Time.deltaTime * 5f);
            
            // Simple bob animation
            bobTimer += Time.deltaTime * 4f;
            float verticalBob = Mathf.Sin(bobTimer) * bobAmount;
            float horizontalBob = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f;
    
            Vector3 bobPosition = normalPosition + new Vector3(horizontalBob, verticalBob, 0);
            transform.localPosition = Vector3.Lerp(transform.localPosition, bobPosition, Time.deltaTime * 5f);
        }
    }
    
    private void FireArrow(float powerPercent)
    {
        if (arrowPrefab == null || playerCamera == null) return;

        // Calculate power based on draw time
        float power = powerPercent * maxPower;
        float arrowDamage = Mathf.Lerp(minDamage, maxDamage, powerPercent);

        // Create ray from camera center (this defines the trajectory)
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
    
        // Get a point slightly to the right and below the camera center
        // Adjust these values to position closer to the bow
        float rightOffset = 0.2f;  // Right offset (X)
        float downOffset = -0.1f;  // Down offset (Y)
        Vector3 offsetDirection = playerCamera.transform.right * rightOffset + playerCamera.transform.up * downOffset;
    
        // Compute spawn position: center + offset + a bit forward
        Vector3 spawnPosition = playerCamera.transform.position + offsetDirection + playerCamera.transform.forward * 0.5f;
    
        // Instantiate arrow at this offset position
        GameObject arrow = Instantiate(arrowPrefab, spawnPosition, Quaternion.identity);
    
        // Orient arrow to face where camera is looking (keep accuracy)
        arrow.transform.forward = ray.direction;
    
        // Set up the arrow with correct direction and power
        ArrowController arrowController = arrow.GetComponent<ArrowController>();
        if (arrowController != null)
        {
            arrowController.damage = arrowDamage;
            arrowController.Initialize(power, player);
        }
    }
    
    private void AnimateBowDraw(float drawPercent)
    {
        if (bowStringTransform == null) return;
    
        // Calculate new string position (pulled back based on draw percentage)
        Vector3 newStringPos = originalStringPosition;
        newStringPos.z -= drawPercent * maxDrawDistance * 0.7f; // Reduce the visual draw distance
    
        // Apply new position
        bowStringTransform.localPosition = newStringPos;
    }
    
    private IEnumerator ResetZoom()
    {
        float time = 0;
        float startFOV = playerCamera.fieldOfView;
        
        while (time < 1)
        {
            time += Time.deltaTime * zoomSpeed * 0.5f;
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, normalFOV, time);
            yield return null;
        }
    }
}