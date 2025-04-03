using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] public float damage = 10f;
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private float gravityMultiplier = 1f;
    [SerializeField] private LayerMask collisionMask;
    
    private Rigidbody rb;
    private Collider arrowCollider;
    private GameObject owner;
    private bool hasHit = false;
    private float timer = 0f;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        arrowCollider = GetComponent<Collider>();
    }
    
    public void Initialize(float force, GameObject owner)
    {
        this.owner = owner;
    
        // Ignore collision with owner
        if (owner != null && arrowCollider != null)
        {
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            foreach (Collider col in ownerColliders)
            {
                Physics.IgnoreCollision(arrowCollider, col);
            }
        }
    
        // Apply initial force - use transform.forward which was set to camera direction
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        
            rb.AddForce(transform.forward * force, ForceMode.Impulse);
        
            // Disable rotation from physics system - we'll handle it manually
            rb.freezeRotation = true;
        }
    }
    
    void Update()
    {
        // Track lifetime to destroy after set time
        timer += Time.deltaTime;
        if (timer > lifetime)
        {
            Destroy(gameObject);
            return;
        }
        
        if (!hasHit && rb.velocity.magnitude > 0.1f)
        {
        
            // Force orientation to match velocity - with smoothing for better visual
            Vector3 velocity = rb.velocity;
        
            // Important: Only create a rotation if velocity has meaningful direction
            if (velocity != Vector3.zero)
            {
                // Calculate target rotation based on velocity
                Quaternion targetRotation = Quaternion.LookRotation(velocity);
            
                // Apply rotation immediately - no interpolation for arrows
                transform.rotation = targetRotation;
            }
        
            // Apply custom gravity for better trajectory
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
    
        // Ignore collisions with owner
        if (collision.gameObject == owner) return;
    
        hasHit = true;
    
        // Deal damage to zombie if applicable
        ZombieController zombie = collision.transform.GetComponentInParent<ZombieController>();
        if (zombie != null)
        {
            zombie.TakeDamage(damage);
        }
    
        // Get hit point
        Vector3 hitPoint = collision.contacts[0].point;
        Vector3 hitNormal = collision.contacts[0].normal;
    
        // Create a visual-only arrow at the hit point
        GameObject visualArrow = Instantiate(gameObject, hitPoint, Quaternion.LookRotation(-hitNormal));
        visualArrow.transform.SetParent(collision.transform);
    
        // Remove components from visual arrow that we don't need
        Destroy(visualArrow.GetComponent<Rigidbody>());
        Destroy(visualArrow.GetComponent<Collider>());
        Destroy(visualArrow.GetComponent<ArrowController>());
    
        // Destroy the original arrow
        Destroy(gameObject);
    }
}