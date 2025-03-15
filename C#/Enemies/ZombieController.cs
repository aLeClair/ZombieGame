using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ZombieController : MonoBehaviour
{
    [Header("Zombie Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private int goldValue = 10;
    [SerializeField] private int experienceValue = 5;
    
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float runSpeed = 3f;
    [SerializeField] private float aggroRange = 15f;
    
    [Header("Target Priorities")]
    [SerializeField] private float playerTargetPriority = 2f; // Higher priority = more likely to target
    [SerializeField] private float towerTargetPriority = 3f;
    [SerializeField] private float defensePriority = 1.5f;
    
    // References
    public delegate void ZombieDeathHandler();
    public event ZombieDeathHandler OnZombieDeath;
    private NavMeshAgent agent;
    private Animator animator;
    private Transform target;
    private AudioSource audioSource;
    private GameManager gameManager;
    private WaveManager waveManager;
    private float nextAttackTime = 0f;
    private bool isDead = false;
    
    // Targeting
    private GameObject player;
    private GameObject tower;
    private bool hasTargetInRange = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        gameManager = FindObjectOfType<GameManager>();
        waveManager = FindObjectOfType<WaveManager>();
        
        // Set agent speed
        if (agent != null)
        {
            agent.speed = walkSpeed;
        }
        
        // Find player and tower
        player = GameObject.FindGameObjectWithTag("Player");
        tower = GameObject.FindGameObjectWithTag("Tower");
        
        // Start behavior coroutines
        StartCoroutine(UpdateTarget());
        StartCoroutine(CheckTargetInRange());
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Update animations based on agent speed
        if (animator != null && agent != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
        
        // Attack if target is in range
        if (hasTargetInRange && Time.time >= nextAttackTime)
        {
            Attack();
        }
    }
    
    private IEnumerator UpdateTarget()
    {
        while (!isDead)
        {
            DetermineTarget();
            yield return new WaitForSeconds(1f);
        }
    }
    
    private void DetermineTarget()
    {
        // Check if player is in range (prioritize player if nearby)
        if (player != null)
        {
            float playerDistance = Vector3.Distance(transform.position, player.transform.position);
            
            // If player is within aggro range, target them with highest priority
            if (playerDistance < aggroRange)
            {
                SetTarget(player.transform);
                return;
            }
        }
        
        // Find all potential obstacles (defenses)
        GameObject[] defenses = GameObject.FindGameObjectsWithTag("Defense");
        
        // Check if tower exists
        if (tower != null)
        {
            // Calculate path to tower
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(tower.transform.position, path))
            {
                // Check if defenses are blocking the path to tower
                foreach (GameObject defense in defenses)
                {
                    // Check if this defense is blocking the path
                    if (IsDefenseBlockingPath(defense, path))
                    {
                        // Target the blocking defense
                        SetTarget(defense.transform);
                        return;
                    }
                }
                
                // If no blocking obstacles, go for the tower
                SetTarget(tower.transform);
            }
            else
            {
                // Can't find path to tower, try to find nearest defense
                GameObject nearestDefense = FindNearestDefense(defenses);
                if (nearestDefense != null)
                {
                    SetTarget(nearestDefense.transform);
                }
                else
                {
                    // Last resort - try to go to tower directly even if no path
                    SetTarget(tower.transform);
                }
            }
        }
        else
        {
            // No tower, go for player regardless of distance
            if (player != null)
            {
                SetTarget(player.transform);
            }
        }
    }

    // Helper method to determine if a defense is blocking the path to tower
    private bool IsDefenseBlockingPath(GameObject defense, NavMeshPath path)
    {
        if (path.corners.Length < 2) return false;
        
        // Check each segment of the path
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Vector3 start = path.corners[i];
            Vector3 end = path.corners[i + 1];
            
            // Create a ray along this path segment
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            
            // Cast a ray to see if defense is hit
            RaycastHit hit;
            if (Physics.Raycast(start, direction, out hit, distance))
            {
                if (hit.collider.gameObject == defense)
                {
                    return true;
                }
            }
            
            // Also check if the defense is near the path
            if (Vector3.Distance(defense.transform.position, start) < 3f ||
                Vector3.Distance(defense.transform.position, end) < 3f)
            {
                return true;
            }
        }
        
        return false;
    }

    // Helper method to find nearest defense
    private GameObject FindNearestDefense(GameObject[] defenses)
    {
        GameObject nearest = null;
        float nearestDist = float.MaxValue;
        
        foreach (GameObject defense in defenses)
        {
            float dist = Vector3.Distance(transform.position, defense.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = defense;
            }
        }
        
        return nearest;
    }
    
    private void SetTarget(Transform newTarget)
    {
        target = newTarget;
        
        if (target != null && agent != null)
        {
            agent.SetDestination(target.position);
            
            // Set speed based on target type
            if (target.CompareTag("Player"))
            {
                agent.speed = runSpeed; // Run faster when chasing player
            }
            else
            {
                agent.speed = walkSpeed;
            }
        }
    }
    
    private IEnumerator CheckTargetInRange()
    {
        while (!isDead)
        {
            hasTargetInRange = false;
            
            if (target != null)
            {
                float distance = Vector3.Distance(transform.position, target.position);
                hasTargetInRange = distance <= attackRange;
                
                // Stop moving if in attack range
                if (hasTargetInRange && agent != null)
                {
                    agent.isStopped = true;
                    // Look at target
                    Vector3 direction = (target.position - transform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(direction);
                    }
                }
                else if (agent != null)
                {
                    agent.isStopped = false;
                }
            }
            
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    private void Attack()
    {
        // Set next attack time
        nextAttackTime = Time.time + 1f / attackRate;
        
        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // Delay actual damage to sync with animation (called via animation event or coroutine)
        StartCoroutine(ApplyDamageAfterDelay(0.5f));
    }
    
    private IEnumerator ApplyDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
    
        if (target != null && !isDead)
        {
            // Apply damage based on target type
            if (target.CompareTag("Player"))
            {
                PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
            }
            else if (target.CompareTag("Tower"))
            {
                TowerController towerController = target.GetComponent<TowerController>();
                if (towerController != null)
                {
                    towerController.TakeDamage(damage);
                }
            }
            else if (target.CompareTag("Defense"))
            {
                DefenseController defense = target.GetComponent<DefenseController>();
                if (defense != null)
                {
                    defense.TakeDamage(damage);
                }
            }
        
            // Play attack sound
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        // Play hurt animation
        if (animator != null)
        {
            animator.SetTrigger("Hurt");
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        isDead = true;
    
        // Stop agent
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
    
        // Play death animation
        if (animator != null)
        {
            animator.SetBool("IsDead", true);
        }
    
        // Disable collider
        Collider zombieCollider = GetComponent<Collider>();
        if (zombieCollider != null)
        {
            zombieCollider.enabled = false;
        }
    
        // Award gold and XP
        if (gameManager != null)
        {
            gameManager.AddGold(goldValue);
            gameManager.AddExperience(experienceValue);
        }
    
        // Notify wave manager
        if (waveManager != null)
        {
            waveManager.ZombieKilled();
        }
    
        // Trigger death event
        OnZombieDeath?.Invoke();
    
        // Chance to drop loot
        TryDropLoot();
    
        // Destroy after delay
        Destroy(gameObject, 5f);
    }
    
    private void TryDropLoot()
    {
        // Roll for loot drop (20% chance)
        if (Random.value < 0.2f)
        {
            LootManager lootManager = FindObjectOfType<LootManager>();
            if (lootManager != null)
            {
                lootManager.SpawnLoot(transform.position);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }
}