using UnityEngine;

public class CrosshairController : MonoBehaviour
{
    [SerializeField] private RectTransform topReticle;
    [SerializeField] private RectTransform rightReticle;
    [SerializeField] private RectTransform bottomReticle;
    [SerializeField] private RectTransform leftReticle;
    [SerializeField] private RectTransform centerDot;  // New center dot reference
    [SerializeField] private float maxSpread = 20f;
    [SerializeField] private float minSpread = 5f;
    
    private static CrosshairController _instance;
    public static CrosshairController Instance { get { return _instance; } }

    private bool bowMode = false;
    
    void Awake()
    {
        _instance = this;
        Debug.Log("CrosshairController initialized");
        SetDefaultCrosshair(); // Set default crosshair on start
    }
    
    public void ShowBowCrosshair(bool enable)
    {
        Debug.Log("ShowBowCrosshair called with: " + enable);
        bowMode = enable;
        gameObject.SetActive(true); // Always show some form of crosshair
        
        // Show reticles only in bow mode
        if (topReticle) topReticle.gameObject.SetActive(enable);
        if (rightReticle) rightReticle.gameObject.SetActive(enable);
        if (bottomReticle) bottomReticle.gameObject.SetActive(enable);
        if (leftReticle) leftReticle.gameObject.SetActive(enable);
        
        // Center dot is always visible
        if (centerDot) centerDot.gameObject.SetActive(!enable);
    }
    
    public void SetDefaultCrosshair()
    {
        // Show only center dot for default/melee weapons
        bowMode = false;
        gameObject.SetActive(true);
        
        if (topReticle) topReticle.gameObject.SetActive(false);
        if (rightReticle) rightReticle.gameObject.SetActive(false);
        if (bottomReticle) bottomReticle.gameObject.SetActive(false);
        if (leftReticle) leftReticle.gameObject.SetActive(false);
        
        if (centerDot) centerDot.gameObject.SetActive(true);
    }
    
    public void UpdateSpread(float powerPercentage)
    {
        // Only update spread if in bow mode
        if (!bowMode) return;
        
        if (topReticle == null || rightReticle == null || 
            bottomReticle == null || leftReticle == null)
        {
            Debug.LogWarning("CrosshairController: Reticle references missing");
            return;
        }
            
        // Calculate current spread based on power percentage (0-1)
        float currentSpread = Mathf.Lerp(maxSpread, minSpread, powerPercentage);
    
        // Update reticle positions
        topReticle.anchoredPosition = new Vector2(0, currentSpread);
        rightReticle.anchoredPosition = new Vector2(currentSpread, 0);
        bottomReticle.anchoredPosition = new Vector2(0, -currentSpread);
        leftReticle.anchoredPosition = new Vector2(-currentSpread, 0);
    }
}