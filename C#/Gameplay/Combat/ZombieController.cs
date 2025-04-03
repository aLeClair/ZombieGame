using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class ZombieController : MonoBehaviour
{
    public enum ZombieType
    {
        Shambler,
        Bruiser,
        Jumper,
        Sneaker, 
        Spitter
    }

    [Header("Zombie Stats")]
    [SerializeField] private ZombieType zombieType = ZombieType.Shambler;
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
    
    // Internal references
    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private Transform targetTransform;
    private AudioSource audioSource;
    private GameManager gameManager;
    private WaveManager waveManager;
    private float nextAttackTime = 0f;
    private bool isDead = false;
    
    // References
    public delegate void ZombieDeathHandler();
    public event ZombieDeathHandler OnZombieDeath;
    
    // New event delegates for zombie type behaviors
    public delegate void TargetUpdatedHandler(Transform newTarget);
    public event TargetUpdatedHandler OnTargetUpdated;
    
    public delegate void PreTargetingHandler();
    public event PreTargetingHandler OnPreTargeting;
    
    public delegate void BehaviorUpdateHandler();
    public event BehaviorUpdateHandler OnBehaviorUpdate;
    
    // Targeting
    private GameObject player;
    private GameObject tower;
    private bool hasTargetInRange = false;
    
    // Movement override for specialized behaviors
    private bool isMovementOverridden = false;
    private Vector3? movementOverridePosition = null;
    private Transform forcedTarget = null;
    
    void Start()
    {
        currentHealth = maxHealth;
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        gameManager = FindObjectOfType<GameManager>();
        waveManager = FindObjectOfType<WaveManager>();
        
        // Set agent speed
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = walkSpeed;
        }
        
        // Find player and tower
        player = GameObject.FindGameObjectWithTag("Player");
        tower = GameObject.FindGameObjectWithTag("Tower");
        
        // Start behavior coroutines
        StartCoroutine(UpdateTarget());
        StartCoroutine(CheckTargetInRange());
        
        // Set up based on zombie type
        SetupZombieType();
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Update animations based on agent speed
        if (animator != null && navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
        }
        
        // Attack if target is in range
        if (hasTargetInRange && Time.time >= nextAttackTime)
        {
            Attack();
        }
        
        // Trigger behavior update for specialized zombies
        if (OnBehaviorUpdate != null)
        {
            OnBehaviorUpdate();
        }
        
        // Handle movement override
        HandleMovementOverride();
    }
    
    private void SetupZombieType()
    {
        // Adjust scale based on zombie type
        switch (zombieType)
        {
            case ZombieType.Bruiser:
                transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
                break;
            case ZombieType.Spitter:
                // Slightly taller and thinner
                transform.localScale = new Vector3(0.9f, 1.2f, 0.9f);
                break;
            case ZombieType.Jumper:
                // Slightly smaller and more agile-looking
                transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                break;
        }
    }
    
    private IEnumerator UpdateTarget()
    {
        while (!isDead)
        {
            // Allow special zombie types to override targeting
            if (OnPreTargeting != null)
            {
                OnPreTargeting();
            }
            
            // If targeting isn't overridden by zombie type behaviors
            if (forcedTarget == null)
            {
                DetermineTarget();
            }
            
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
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
            {
                NavMeshPath path = new NavMeshPath();
                if (navMeshAgent.CalculatePath(tower.transform.position, path))
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
                // NavMeshAgent not available, just target tower directly
                SetTarget(tower.transform);
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
        targetTransform = newTarget;
        
        if (targetTransform != null && navMeshAgent != null && navMeshAgent.isActiveAndEnabled && !isMovementOverridden)
        {
            navMeshAgent.SetDestination(targetTransform.position);
            
            // Set speed based on target type
            if (targetTransform.CompareTag("Player"))
            {
                navMeshAgent.speed = runSpeed; // Run faster when chasing player
            }
            else
            {
                navMeshAgent.speed = walkSpeed;
            }
        }
        
        // Trigger target updated event for specialized behaviors
        if (OnTargetUpdated != null)
        {
            OnTargetUpdated(targetTransform);
        }
    }
    
    private IEnumerator CheckTargetInRange()
    {
        while (!isDead)
        {
            hasTargetInRange = false;
            
            if (targetTransform != null)
            {
                float distance = Vector3.Distance(transform.position, targetTransform.position);
                hasTargetInRange = distance <= attackRange;
                
                // Stop moving if in attack range
                if (hasTargetInRange && navMeshAgent != null && navMeshAgent.isActiveAndEnabled && !isMovementOverridden)
                {
                    navMeshAgent.isStopped = true;
                    // Look at target
                    Vector3 direction = (targetTransform.position - transform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(direction);
                    }
                }
                else if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && !isMovementOverridden)
                {
                    navMeshAgent.isStopped = false;
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
    
        if (targetTransform != null && !isDead)
        {
            // Apply damage based on target type
            if (targetTransform.CompareTag("Player"))
            {
                PlayerHealth playerHealth = targetTransform.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
            }
            else if (targetTransform.CompareTag("Tower"))
            {
                TowerController towerController = targetTransform.GetComponent<TowerController>();
                if (towerController != null)
                {
                    towerController.TakeDamage(damage);
                }
            }
            else if (targetTransform.CompareTag("Defense"))
            {
                DefenseController defense = targetTransform.GetComponent<DefenseController>();
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
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false;
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
        if (OnZombieDeath != null)
        {
            OnZombieDeath();
        }
    
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
    
    // Methods for specialized zombie types to control behavior
    
    public void SetOverrideMovement(bool overrideMove)
    {
        isMovementOverridden = overrideMove;
        
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            if (overrideMove)
            {
                navMeshAgent.isStopped = true;
            }
            else if (targetTransform != null)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(targetTransform.position);
            }
        }
    }
    
    public void SetMovementOverride(Vector3 position)
    {
        if (!isMovementOverridden)
        {
            isMovementOverridden = true;
        }
        
        movementOverridePosition = position;
        
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.SetDestination(position);
            navMeshAgent.isStopped = false;
        }
    }
    
    public void ClearMovementOverride()
    {
        movementOverridePosition = null;
        
        if (isMovementOverridden)
        {
            isMovementOverridden = false;
            
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && targetTransform != null)
            {
                navMeshAgent.SetDestination(targetTransform.position);
                navMeshAgent.isStopped = false;
            }
        }
    }
    
    public void ForceTarget(Transform newTarget)
    {
        forcedTarget = newTarget;
        SetTarget(newTarget);
    }
    
    public void ClearForcedTarget()
    {
        forcedTarget = null;
    }
    
    private void HandleMovementOverride()
    {
        if (isMovementOverridden && movementOverridePosition.HasValue)
        {
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
            {
                // Check if we've reached the override position
                if (Vector3.Distance(transform.position, movementOverridePosition.Value) < 1.0f)
                {
                    // We've reached the override position
                    navMeshAgent.isStopped = true;
                }
                else
                {
                    // Update destination in case target moved
                    navMeshAgent.SetDestination(movementOverridePosition.Value);
                }
            }
        }
    }
    
    // Getter/setter methods for zombie types to use
    
    public Transform GetTarget()
    {
        return targetTransform;
    }
    
    public float GetAttackRange()
    {
        return attackRange;
    }
    
    public float GetDamage()
    {
        return damage;
    }
    
    public ZombieType GetZombieType()
    {
        return zombieType;
    }
    
    public void SetZombieType(ZombieType type)
    {
        zombieType = type;
    }
    
    public void SetStats(float health, float newDamage, float newSpeed, float newAttackRate, float newAggroRange)
    {
        maxHealth = health;
        currentHealth = health;
        damage = newDamage;
        walkSpeed = newSpeed;
        runSpeed = newSpeed * 1.5f;
        attackRate = newAttackRate;
        aggroRange = newAggroRange;
        
        // Update agent speed
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = walkSpeed;
        }
    }
    
    public void ScaleStats(float newHealth, float newDamage)
    {
        maxHealth = newHealth;
        currentHealth = newHealth;
        damage = newDamage;
    }
    
    public void SetTargetPriorities(float playerPriority, float towerPriority, float defPriority)
    {
        playerTargetPriority = playerPriority;
        towerTargetPriority = towerPriority;
        defensePriority = defPriority;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }
}