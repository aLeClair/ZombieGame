using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AcidPuddle : MonoBehaviour
{
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float radius = 2f;
    [SerializeField] private float growthTime = 0.5f;
    
    private float damagePerSecond;
    private float lifetime;
    private float startTime;
    private Dictionary<GameObject, float> damagedEntities = new Dictionary<GameObject, float>();
    
    public void Initialize(float damagePerSecond, float lifetime)
    {
        this.damagePerSecond = damagePerSecond;
        this.lifetime = lifetime;
        this.startTime = Time.time;
        
        // Start damage tick routine
        StartCoroutine(DamageTick());
        
        // Grow puddle over time
        transform.localScale = Vector3.zero;
        StartCoroutine(GrowPuddle());
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }
    
    private IEnumerator GrowPuddle()
    {
        float growthStartTime = Time.time;
        Vector3 targetScale = new Vector3(radius * 2, 0.1f, radius * 2);
        
        while (Time.time < growthStartTime + growthTime)
        {
            float t = (Time.time - growthStartTime) / growthTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
    
    private IEnumerator DamageTick()
    {
        while (Time.time < startTime + lifetime)
        {
            // Find all entities in puddle
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius * transform.localScale.x / 2f);
            
            foreach (Collider hitCollider in hitColliders)
            {
                // Skip non-damageable objects
                if (!hitCollider.CompareTag("Player") && !hitCollider.CompareTag("Tower") && 
                    !hitCollider.CompareTag("Defense") && !hitCollider.CompareTag("Enemy"))
                {
                    continue;
                }
                
                // Check if we've damaged this entity recently
                if (damagedEntities.ContainsKey(hitCollider.gameObject))
                {
                    if (Time.time < damagedEntities[hitCollider.gameObject] + tickInterval)
                    {
                        continue; // Still on cooldown for this entity
                    }
                }
                
                // Apply damage based on entity type
                if (hitCollider.CompareTag("Player"))
                {
                    PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(damagePerSecond * tickInterval);
                        damagedEntities[hitCollider.gameObject] = Time.time;
                    }
                }
                else if (hitCollider.CompareTag("Tower"))
                {
                    TowerController tower = hitCollider.GetComponent<TowerController>();
                    if (tower != null)
                    {
                        tower.TakeDamage(damagePerSecond * tickInterval);
                        damagedEntities[hitCollider.gameObject] = Time.time;
                    }
                }
                else if (hitCollider.CompareTag("Defense"))
                {
                    DefenseController defense = hitCollider.GetComponent<DefenseController>();
                    if (defense != null)
                    {
                        defense.TakeDamage(damagePerSecond * tickInterval);
                        damagedEntities[hitCollider.gameObject] = Time.time;
                    }
                }
            }
            
            yield return new WaitForSeconds(tickInterval);
        }
    }
    
    private void OnDrawGizmos()
    {
        // Draw radius in editor for debugging
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}