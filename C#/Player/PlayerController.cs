using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float upperLookLimit = 80f;
    [SerializeField] private float lowerLookLimit = 80f;

    [Header("Crouch Settings")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private Transform cameraHolder;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    private CharacterController controller;
    private Camera playerCamera;
    private Vector3 velocity;
    private bool isGrounded;
    private float currentSpeed;
    private float xRotation = 0f;
    private bool isCrouching = false;
    private float originalCameraY;
    private Vector3 originalCenterPoint;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        originalCenterPoint = controller.center;
        isCrouching = false;
        controller.height = standingHeight;
        
        // Get or create the camera and camera holder
        if (cameraHolder == null)
        {
            // Look for existing camera holder
            cameraHolder = transform.Find("CameraHolder");
            
            // Create if not found
            if (cameraHolder == null)
            {
                GameObject holderObj = new GameObject("CameraHolder");
                holderObj.transform.SetParent(transform);
                holderObj.transform.localPosition = new Vector3(0, 0.7f, 0);
                cameraHolder = holderObj.transform;
            }
        }
        
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            // Create camera if not found
            GameObject cameraObj = new GameObject("PlayerCamera");
            cameraObj.transform.SetParent(cameraHolder);
            cameraObj.transform.localPosition = Vector3.zero;
            playerCamera = cameraObj.AddComponent<Camera>();
        }
        
        originalCameraY = cameraHolder.localPosition.y;
        
        // Create ground check if it doesn't exist
        if (groundCheck == null)
        {
            GameObject check = new GameObject("GroundCheck");
            check.transform.SetParent(transform);
            check.transform.localPosition = new Vector3(0, -0.95f, 0);
            groundCheck = check.transform;
        }
        
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        currentSpeed = moveSpeed;
    }

    void Update()
    {
        HandleGroundCheck();
        HandleLook();
        HandleMovement();
        HandleCrouch();
        HandleGravity();
        
        bool canStand = !Physics.Raycast(transform.position, Vector3.up, standingHeight);
    }

    void HandleGroundCheck()
    {
        // Check if player is grounded
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Reset vertical velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to ensure grounding
        }
    }

    void HandleMovement()
    {
        // Get input axes
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        // Create direction vector relative to camera orientation
        Vector3 moveDirection = Vector3.zero;
        
        // Calculate forward and right vectors based on camera's forward
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        
        // Keep vectors horizontal by zeroing Y component and normalizing
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        // Combine directions
        moveDirection = forward * vertical + right * horizontal;
        
        // Handle speed changes
        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else
        {
            currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        }

        // Move the player if there's input
        if (moveDirection.magnitude >= 0.1f)
        {
            // Normalize direction if moving diagonally to prevent faster movement
            controller.Move(moveDirection.normalized * currentSpeed * Time.deltaTime);
        }
        
        
        // Jump when grounded and space is pressed (only when not crouching)
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
    
    void HandleLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Calculate vertical rotation for camera (up/down)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -upperLookLimit, lowerLookLimit);
        
        // Apply vertical rotation to camera only
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        // Apply horizontal rotation to player body (left/right)
        transform.Rotate(Vector3.up * mouseX);
    }
    
    void HandleCrouch()
    {
        // Toggle crouch state with Left Control
        if (Input.GetKeyDown(KeyCode.LeftControl) && isGrounded)
        {
            isCrouching = !isCrouching;
        }
        
        // Apply crouch effects
        if (isCrouching)
        {
            // Smoothly transition to crouch height
            float targetHeight = crouchHeight;
            controller.height = Mathf.Lerp(controller.height, crouchHeight, crouchTransitionSpeed * Time.deltaTime);
            
            // Adjust center point to keep grounded
            Vector3 targetCenter = new Vector3(0, originalCenterPoint.y - ((standingHeight - crouchHeight) / 2), 0);
            controller.center = Vector3.Lerp(controller.center, targetCenter, crouchTransitionSpeed * Time.deltaTime);
            
            // Adjust camera position
            Vector3 currentCamPos = cameraHolder.localPosition;
            Vector3 targetCamPos = new Vector3(currentCamPos.x, originalCameraY - ((standingHeight - crouchHeight) / 2), currentCamPos.z);
            cameraHolder.localPosition = Vector3.Lerp(currentCamPos, targetCamPos, crouchTransitionSpeed * Time.deltaTime);
        }
        else
        {
            Vector3 checkOrigin = transform.position + Vector3.up * controller.height;
            float checkDistance = standingHeight - controller.height;
            
            // Check if there's enough room to stand up
            bool canStand = !Physics.Raycast(checkOrigin, Vector3.up, checkDistance);
            
            if (canStand)
            {
                // Smoothly transition back to standing height
                float targetHeight = standingHeight;
                controller.height = Mathf.Lerp(controller.height, standingHeight, crouchTransitionSpeed * Time.deltaTime);
                
                // Reset center point
                controller.center = Vector3.Lerp(controller.center, originalCenterPoint, crouchTransitionSpeed * Time.deltaTime);
                
                // Reset camera position
                Vector3 currentCamPos = cameraHolder.localPosition;
                Vector3 targetCamPos = new Vector3(currentCamPos.x, originalCameraY, currentCamPos.z);
                cameraHolder.localPosition = Vector3.Lerp(currentCamPos, targetCamPos, crouchTransitionSpeed * Time.deltaTime);
            }
            else
            {
                // Can't stand up due to obstacle, stay crouched
                isCrouching = true;
            }
        }
    }

    void HandleGravity()
    {
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}