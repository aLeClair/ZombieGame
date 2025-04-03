using UnityEngine;

public class TowerProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 30f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private GameObject impactEffect;
    
    private float damage;
    private Vector3 direction;
    private GameObject owner;
    
    public void Initialize(float damage, Vector3 direction, GameObject owner)
    {
        this.damage = damage;
        this.direction = direction;
        this.owner = owner;
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }
    
    void Update()
    {
        // Move in the specified direction
        transform.position += direction * speed * Time.deltaTime;
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Ignore collision with owner
        if (other.gameObject == owner) return;
        
        // Check if we hit an enemy
        ZombieController zombie = other.GetComponent<ZombieController>();
        if (zombie != null)
        {
            // Apply damage
            zombie.TakeDamage(damage);
            
            // Spawn impact effect
            if (impactEffect != null)
            {
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            }
            
            // Destroy projectile
            Destroy(gameObject);
        }
    }
}