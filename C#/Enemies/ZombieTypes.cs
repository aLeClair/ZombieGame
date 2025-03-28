using UnityEngine;

// Base zombie type implementations - add this file to extend the core ZombieController

// Shambler - Standard zombie
public class ShamblerZombie : MonoBehaviour
{
    private ZombieController zombieController;
    
    private void Awake()
    {
        zombieController = GetComponent<ZombieController>();
        if (zombieController != null)
        {
            // Set shambler-specific stats
            zombieController.SetZombieType(ZombieController.ZombieType.Shambler);
            zombieController.SetStats(100f, 10f, 1.5f, 1.5f, 15f);
        }
    }
}

// Bruiser - Tough, slow-moving zombie with charge attack
public class BruiserZombie : MonoBehaviour
{
    private ZombieController zombieController;
    [SerializeField] private float chargeSpeed = 6f;
    [SerializeField] private float chargeCooldown = 8f;
    [SerializeField] private float chargeDistance = 15f;
    [SerializeField] private float chargeDuration = 2f;
    [SerializeField] private float chargeDetectionRange = 20f;
    
    private bool isCharging = false;
    private float nextChargeTime = 0f;
    private Vector3 chargeTarget;
    private float chargeEndTime = 0f;
    
    private void Awake()
    {
        zombieController = GetComponent<ZombieController>();
        if (zombieController != null)
        {
            // Set bruiser-specific stats
            zombieController.SetZombieType(ZombieController.ZombieType.Bruiser);
            zombieController.SetStats(250f, 20f, 1.0f, 1.0f, 20f);
            
            // Subscribe to the target update event
            zombieController.OnTargetUpdated += CheckForChargeOpportunity;
        }
    }
    
    private void Update()
    {
        if (isCharging)
        {
            // Continue the charge
            PerformCharge();
            
            // Check if charge time is over
            if (Time.time >= chargeEndTime)
            {
                StopCharge();
            }
        }
    }
    
    private void CheckForChargeOpportunity(Transform target)
    {
        if (target == null || isCharging || Time.time < nextChargeTime)
            return;
            
        // Check if target is within charge detection range
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget <= chargeDetectionRange && distanceToTarget > zombieController.GetAttackRange())
        {
            // Check if we have a clear path to target
            RaycastHit hit;
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            if (Physics.Raycast(transform.position, directionToTarget, out hit, chargeDistance))
            {
                if (hit.transform == target)
                {
                    // We have a clear line of sight to target, start charging
                    StartCharge(target.position);
                }
            }
        }
    }
    
    private void StartCharge(Vector3 targetPosition)
    {
        isCharging = true;
        chargeTarget = targetPosition;
        chargeEndTime = Time.time + chargeDuration;
        
        // Override the zombie controller movement temporarily
        zombieController.SetOverrideMovement(true);
        
        // Play charge animation/sound
        // zombieController.PlayAnimation("Charge");
    }
    
    private void PerformCharge()
    {
        // Move towards the charge target at charge speed
        Vector3 direction = (chargeTarget - transform.position).normalized;
        transform.position += direction * chargeSpeed * Time.deltaTime;
        
        // Rotate to face direction
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        // Check for collisions during charge
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.0f);
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                // Hit player - apply damage and stop charge
                PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(zombieController.GetDamage() * 1.5f); // Extra damage on charge hit
                }
                StopCharge();
                break;
            }
            else if (hitCollider.CompareTag("Wall") || hitCollider.CompareTag("Defense"))
            {
                // Hit wall or defense - stop charge
                StopCharge();
                break;
            }
        }
    }
    
    private void StopCharge()
    {
        isCharging = false;
        nextChargeTime = Time.time + chargeCooldown;
        
        // Give back control to zombie controller
        zombieController.SetOverrideMovement(false);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (zombieController != null)
        {
            zombieController.OnTargetUpdated -= CheckForChargeOpportunity;
        }
    }
}

// Jumper - Can jump over obstacles
public class JumperZombie : MonoBehaviour
{
    private ZombieController zombieController;
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float jumpCooldown = 5f;
    [SerializeField] private float jumpDetectionDistance = 2f;
    [SerializeField] private float jumpDistance = 5f;
    [SerializeField] private LayerMask obstacleLayer;
    
    private bool isJumping = false;
    private float nextJumpTime = 0f;
    private Vector3 jumpTarget;
    private float jumpProgress = 0f;
    private float jumpDuration = 1f;
    private Vector3 jumpStartPosition;
    
    private void Awake()
    {
        zombieController = GetComponent<ZombieController>();
        if (zombieController != null)
        {
            // Set jumper-specific stats
            zombieController.SetZombieType(ZombieController.ZombieType.Jumper);
            zombieController.SetStats(80f, 15f, 2.5f, 1.2f, 15f);
        }
    }
    
    private void Update()
    {
        if (!isJumping && Time.time > nextJumpTime)
        {
            CheckForObstacles();
        }
        
        if (isJumping)
        {
            PerformJump();
        }
    }
    
    private void CheckForObstacles()
    {
        if (zombieController.GetTarget() == null)
            return;
            
        // Cast ray forward to detect obstacles
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, jumpDetectionDistance, obstacleLayer))
        {
            // Found an obstacle - check if we can jump over it
            if (hit.collider.bounds.size.y < jumpHeight)
            {
                // Calculate jump target (beyond obstacle)
                Vector3 direction = (zombieController.GetTarget().position - transform.position).normalized;
                jumpTarget = transform.position + direction * jumpDistance;
                
                // Adjust jump target Y to match ground level
                RaycastHit groundHit;
                if (Physics.Raycast(new Vector3(jumpTarget.x, jumpTarget.y + 5f, jumpTarget.z), Vector3.down, out groundHit, 10f))
                {
                    jumpTarget.y = groundHit.point.y;
                }
                else
                {
                    jumpTarget.y = transform.position.y;
                }
                
                StartJump();
            }
        }
    }
    
    private void StartJump()
    {
        isJumping = true;
        jumpProgress = 0f;
        jumpStartPosition = transform.position;
        
        // Override the zombie controller movement temporarily
        zombieController.SetOverrideMovement(true);
        
        // Play jump animation/sound
        // zombieController.PlayAnimation("Jump");
    }
    
    private void PerformJump()
    {
        // Increment jump progress
        jumpProgress += Time.deltaTime / jumpDuration;
        
        if (jumpProgress >= 1.0f)
        {
            // Jump completed
            transform.position = jumpTarget;
            FinishJump();
            return;
        }
        
        // Calculate new position
        Vector3 currentPos = Vector3.Lerp(jumpStartPosition, jumpTarget, jumpProgress);
        
        // Add arc to jump
        float arcHeight = Mathf.Sin(jumpProgress * Mathf.PI) * jumpHeight;
        currentPos.y = currentPos.y + arcHeight;
        
        // Update position
        transform.position = currentPos;
        
        // Update rotation to face jump target
        Vector3 direction = (jumpTarget - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    private void FinishJump()
    {
        isJumping = false;
        nextJumpTime = Time.time + jumpCooldown;
        
        // Give back control to zombie controller
        zombieController.SetOverrideMovement(false);
    }
}

// Sneaker - Prefers to attack from behind or go for the tower
public class SneakerZombie : MonoBehaviour
{
    private ZombieController zombieController;
    
    private void Awake()
    {
        zombieController = GetComponent<ZombieController>();
        if (zombieController != null)
        {
            // Set sneaker-specific stats
            zombieController.SetZombieType(ZombieController.ZombieType.Sneaker);
            zombieController.SetStats(80f, 20f, 2.8f, 1.0f, 20f);
            
            // Adjust target priorities to prefer tower or sneaking behind player
            zombieController.SetTargetPriorities(1.0f, 3.0f, 1.0f);
            
            // Subscribe to the pre-targeting event to choose optimal paths
            zombieController.OnPreTargeting += ChooseSneakTarget;
        }
    }
    
    private void ChooseSneakTarget()
    {
        // This method runs before the normal targeting logic
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        GameObject tower = GameObject.FindGameObjectWithTag("Tower");
        
        if (player == null || tower == null)
            return;
            
        // Calculate distances
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float distanceToTower = Vector3.Distance(transform.position, tower.transform.position);
        
        // Get player's forward direction
        Vector3 playerForward = player.transform.forward;
        
        // Check if we can get behind the player
        Vector3 directionToPlayer = (transform.position - player.transform.position).normalized;
        float dotProduct = Vector3.Dot(playerForward, directionToPlayer);
        
        bool canSneakBehindPlayer = (dotProduct > 0.5f); // We're behind player
        
        // Decision making logic
        if (distanceToTower < 10f || distanceToPlayer > 25f)
        {
            // Close to tower or player is far away - go for tower
            zombieController.ForceTarget(tower.transform);
        }
        else if (canSneakBehindPlayer && distanceToPlayer < 15f)
        {
            // We can sneak behind player and are close enough
            zombieController.ForceTarget(player.transform);
        }
        else if (distanceToPlayer < distanceToTower * 0.5f)
        {
            // Player is much closer than tower - go for player
            zombieController.ForceTarget(player.transform);
        }
        else
        {
            // Default - go for tower
            zombieController.ForceTarget(tower.transform);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (zombieController != null)
        {
            zombieController.OnPreTargeting -= ChooseSneakTarget;
        }
    }
}

// Spitter - Ranged zombie that avoids getting too close
public class SpitterZombie : MonoBehaviour
{
    private ZombieController zombieController;
    [SerializeField] private float spitRange = 15f;
    [SerializeField] private float spitCooldown = 3f;
    [SerializeField] private float preferredDistance = 10f;
    [SerializeField] private GameObject acidProjectilePrefab;
    
    private float nextSpitTime = 0f;
    
    private void Awake()
    {
        zombieController = GetComponent<ZombieController>();
        if (zombieController != null)
        {
            // Set spitter-specific stats
            zombieController.SetZombieType(ZombieController.ZombieType.Spitter);
            zombieController.SetStats(70f, 8f, 1.8f, 0.5f, 18f);
            
            // Subscribe to update event
            zombieController.OnBehaviorUpdate += UpdateSpitterBehavior;
        }
    }
    
    private void UpdateSpitterBehavior()
    {
        Transform target = zombieController.GetTarget();
        if (target == null)
            return;
            
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Check if we're alone
        bool isAlone = !IsOtherZombiesNearby();
        
        if (isAlone && distanceToTarget < preferredDistance * 0.8f)
        {
            // We're alone and too close to target - back away
            Vector3 backupDirection = (transform.position - target.position).normalized;
            zombieController.SetMovementOverride(transform.position + backupDirection * 5f);
        }
        else if (distanceToTarget <= spitRange && Time.time >= nextSpitTime)
        {
            // We can spit at the target
            SpitAtTarget(target);
        }
        else if (distanceToTarget < preferredDistance)
        {
            // We're closer than preferred distance but not close enough to back away
            // Just stay put and wait for spit cooldown
            zombieController.SetMovementOverride(transform.position);
        }
        else
        {
            // We're at a good distance or too far away
            // Let the zombie controller handle movement
            zombieController.ClearMovementOverride();
        }
    }
    
    private bool IsOtherZombiesNearby()
    {
        // Check if there are other zombies nearby
        Collider[] colliders = Physics.OverlapSphere(transform.position, 10f);
        int zombieCount = 0;
        
        foreach (Collider collider in colliders)
        {
            if (collider.GetComponent<ZombieController>() != null && 
                collider.gameObject != gameObject)
            {
                zombieCount++;
                if (zombieCount >= 2) // At least 2 other zombies
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private void SpitAtTarget(Transform target)
    {
        // Set next spit time
        nextSpitTime = Time.time + spitCooldown;
        
        // Create acid projectile
        if (acidProjectilePrefab != null)
        {
            Vector3 spawnPos = transform.position + transform.forward + Vector3.up;
            GameObject projectile = Instantiate(acidProjectilePrefab, spawnPos, Quaternion.identity);
            
            // Set up projectile behavior
            AcidProjectile acid = projectile.GetComponent<AcidProjectile>();
            if (acid != null)
            {
                acid.Initialize(zombieController.GetDamage(), target, gameObject);
            }
            
            // Play spit animation/sound
            // zombieController.PlayAnimation("Spit");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (zombieController != null)
        {
            zombieController.OnBehaviorUpdate -= UpdateSpitterBehavior;
        }
    }
}