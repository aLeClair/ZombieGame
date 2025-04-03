using UnityEngine;
using System.Collections;

public class AcidProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private float splashRadius = 2f;
    [SerializeField] private GameObject acidSplashPrefab;
    [SerializeField] private float arcHeight = 2f;
    
    private float damage;
    private Transform target;
    private GameObject owner;
    private Vector3 startPosition;
    private float journeyTime;
    private float startTime;
    
    public void Initialize(float damage, Transform target, GameObject owner)
    {
        this.damage = damage;
        this.target = target;
        this.owner = owner;
        
        startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, target.position);
        journeyTime = distance / speed;
        startTime = Time.time;
        
        // Destroy after lifetime (in case it never hits)
        Destroy(gameObject, maxLifetime);
    }
    
    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // Calculate current projectile position with arc trajectory
        float timeProgress = (Time.time - startTime) / journeyTime;
        
        if (timeProgress >= 1.0f)
        {
            // Reached end of journey - impact
            Impact(target.position);
            return;
        }
        
        // Create arc trajectory
        Vector3 currentPos = Vector3.Lerp(startPosition, target.position, timeProgress);
        float arcFactor = Mathf.Sin(timeProgress * Mathf.PI);
        currentPos.y += arcHeight * arcFactor;
        
        // Update position
        transform.position = currentPos;
        
        // Update rotation to face movement direction
        if (timeProgress < 1.0f)
        {
            Vector3 nextPos = Vector3.Lerp(startPosition, target.position, Mathf.Min(1.0f, timeProgress + 0.01f));
            nextPos.y += arcHeight * Mathf.Sin(Mathf.Min(1.0f, timeProgress + 0.01f) * Mathf.PI);
            
            Vector3 direction = (nextPos - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Ignore collision with owner
        if (other.gameObject == owner) return;
        
        // Check if we hit something
        Impact(transform.position);
    }
    
    private void Impact(Vector3 position)
    {
        // Apply splash damage
        Collider[] hitColliders = Physics.OverlapSphere(position, splashRadius);
        foreach (Collider hitCollider in hitColliders)
        {
            // Damage player
            if (hitCollider.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
            }
            
            // Damage tower
            if (hitCollider.CompareTag("Tower"))
            {
                TowerController tower = hitCollider.GetComponent<TowerController>();
                if (tower != null)
                {
                    tower.TakeDamage(damage);
                }
            }
            
            // Damage defenses
            if (hitCollider.CompareTag("Defense"))
            {
                DefenseController defense = hitCollider.GetComponent<DefenseController>();
                if (defense != null)
                {
                    defense.TakeDamage(damage);
                }
            }
        }
        
        // Spawn acid splash effect
        if (acidSplashPrefab != null)
        {
            GameObject splash = Instantiate(acidSplashPrefab, position, Quaternion.identity);
            
            // Set up acid puddle to stay for a while and do damage over time
            AcidPuddle puddle = splash.GetComponent<AcidPuddle>();
            if (puddle != null)
            {
                puddle.Initialize(damage * 0.2f, 5f); // 20% damage per second for 5 seconds
            }
            else
            {
                // If no puddle component, just destroy after delay
                Destroy(splash, 5f);
            }
        }
        
        // Destroy projectile
        Destroy(gameObject);
    }
}