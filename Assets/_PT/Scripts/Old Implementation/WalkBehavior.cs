using UnityEngine;
using System.Collections.Generic;

public class WalkBehavior : MonoBehaviour
{
    [Header("Walk Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float pauseMinTime = 0.5f;
    [SerializeField] private float pauseMaxTime = 2f;
    [SerializeField] private float targetReachDistance = 0.5f;

    [Header("Animation Settings")]
    [SerializeField] public Animator animator;
    [SerializeField] private float animationTransitionSpeed = 5f;

    // --- Composite Bounds ---
    private List<Collider> walkBoundsList = new List<Collider>();
    private Vector3 currentTargetPosition;
    private bool isWalkingEnabled = true;
    private bool isCurrentlyMoving = false;
    private float pauseTimer = 0f;
    private bool isPaused = false;

    // --- Queue System ---
    private Queue<Vector3> targetQueue = new Queue<Vector3>();

    // --- Animation ---
    private string IS_WALKING_PARAM = "isWalking";
    public string IS_VIP_PARAM = "isVIP";

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    
    /// <summary>
    /// Sets the bounds for walking and enqueues the first random target.
    /// </summary>
    public void Initialize(List<Collider> boundsList)
    {
        walkBoundsList = boundsList;
        transform.position = GetRandomPointInCompositeBounds();
        EnqueueRandomTarget();
        StartWalking();
    }

    private void Update()
    {
        if (!isWalkingEnabled || walkBoundsList.Count == 0) return;

        // 1. Handle the pause state after reaching a target    
        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                // Pause is over, get the next target from the queue
                isPaused = false;
                PrepareAndSetNextTarget();
            }
            return; // Don't do anything else while paused
        }

        if (!isCurrentlyMoving) return;
        
        // 2. Handle the movement logic (your original code)
        float distanceToTarget = Vector3.Distance(transform.position, currentTargetPosition);

        if (distanceToTarget > targetReachDistance)
        {
            // Move towards target
            Vector3 direction = (currentTargetPosition - transform.position).normalized;
            Vector3 nextPosition = transform.position + direction * (walkSpeed * Time.deltaTime);
            transform.position = ClampToCompositeBounds(nextPosition);

            if (direction != Vector3.zero)
            {
                // Rotate to face movement direction
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                Quaternion yRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, yRotation, animationTransitionSpeed * Time.deltaTime);
            }
        }
        else  // 3. Reached destination: stop moving and start the pause
        {
            isCurrentlyMoving = false;
            UpdateAnimation(false);
            
            // Check if this was a VIP rock destination
            ChatAvatar avatar = GetComponent<ChatAvatar>();
            if (avatar != null && avatar.CheckIfReachedVipRock(currentTargetPosition))
            {
                return;
            }
            // Don't start normal pause - avatar will handle VIP rock logic
            isPaused = true;
            pauseTimer = Random.Range(pauseMinTime, pauseMaxTime);
        }
    }

    // --- Public Control Methods ---

    /// <summary>
    /// Adds a specific destination to the end of the queue.
    /// Call this from other scripts to give the character a new task without interrupting it.
    /// </summary>
    
    public void EnqueueTarget(Vector3 newTargetPosition)
    {
        if (walkBoundsList.Count == 0) return;

        Collider closest = walkBoundsList[0];
        float minDist = Vector3.Distance(newTargetPosition, closest.bounds.center);

        foreach (var col in walkBoundsList)
        {
            float dist = Vector3.Distance(newTargetPosition, col.bounds.center);
            if (dist < minDist)
            {
                minDist = dist;
                closest = col;
            }
        }

        targetQueue.Enqueue(closest.ClosestPoint(newTargetPosition));
    }

    public void StartWalking()
    {
        isWalkingEnabled = true;
        if (!isCurrentlyMoving && !isPaused)
        {
            PrepareAndSetNextTarget();
        }
    }

    /// <summary>
    /// Completely stops walking and clears all pending targets
    /// </summary>
    
    public void StopWalking()
    {
        isWalkingEnabled = false;
        isCurrentlyMoving = false;
        isPaused = false;
        targetQueue.Clear();
        UpdateAnimation(false);
    }

    /// <summary>
    /// Temporarily pauses walking without clearing the queue (used for eating, etc.)
    /// </summary>
    /// <param name="duration">How long to pause in seconds</param>
    
    public void PauseForDuration(float duration)
    {
        isCurrentlyMoving = false;
        UpdateAnimation(false);
        isPaused = true;
        pauseTimer = duration;
    }

    public void PauseWalking()
    {
        isWalkingEnabled = false;
        isCurrentlyMoving = false;
        isPaused = false;
        UpdateAnimation(false);
    }

    public void ResumeWalking()
    {
        isWalkingEnabled = true;
        if (!isPaused)
        {
            PrepareAndSetNextTarget();
        }
    }

    public void SetWalkSpeed(float speed)
    {
        walkSpeed = speed;
    }

    // --- Core Queue Logic ---

    /// <summary>
    /// Prepares the system for the next target by managing the queue.
    /// </summary>
    
    private void PrepareAndSetNextTarget()
    {
        // If the queue is empty, add a new random wander target.
        // This creates the continuous wandering behavior.
        if (targetQueue.Count == 0)
        {
            EnqueueRandomTarget();
        }

        // Dequeue the next target and begin moving towards it.
        if (targetQueue.Count > 0)
        {
            currentTargetPosition = targetQueue.Dequeue();
            isCurrentlyMoving = true;
            UpdateAnimation(true);
        }
    }

    private void EnqueueRandomTarget()
    {
        targetQueue.Enqueue(GetRandomPointInCompositeBounds());
    }

    private Vector3 GetRandomPointInCompositeBounds()
    {
        if (walkBoundsList.Count == 0) return transform.position;

        Collider selected = walkBoundsList[Random.Range(0, walkBoundsList.Count)];
        Bounds bounds = selected.bounds;

        Vector3 randomPoint = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.center.y,
            Random.Range(bounds.min.z, bounds.max.z)
        );

        return selected.ClosestPoint(randomPoint);
    }

    private Vector3 ClampToCompositeBounds(Vector3 position)
    {
        if (walkBoundsList.Count == 0) return position;

        Vector3 closestPoint = position;
        float minDist = float.MaxValue;

        foreach (var col in walkBoundsList)
        {
            Vector3 candidate = col.ClosestPoint(position);
            float dist = Vector3.Distance(position, candidate);
            if (dist < minDist)
            {
                minDist = dist;
                closestPoint = candidate;
            }
        }

        return closestPoint;
    }

    private void UpdateAnimation(bool walking)
    {
        if (animator != null)
        {
            animator.SetBool(IS_WALKING_PARAM, walking);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        foreach (var col in walkBoundsList)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(currentTargetPosition, 0.5f);

        Gizmos.color = isCurrentlyMoving ? Color.green : Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}