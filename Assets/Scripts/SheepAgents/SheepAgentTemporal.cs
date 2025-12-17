using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SheepAgentTemporal : MonoBehaviour
{
    [HideInInspector] public FlockManager manager;
    Rigidbody rb;
    float speed;
    public float lastCalcTime = 0f;

    // --- NONALLOC BUFFER ---
    private Collider[] neighborBuffer = new Collider[30];

    [Header("Physics Settings")]
    [SerializeField] private float movementSmoothness = 5f; //higher smoothness faster responses
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float speedMultiplier = 2.0f; // Adjust this to make sheep faster/slower

    // Weights
    float cohesionWeight = 1.0f;
    float separationWeight = 2.5f;
    float alignmentWeight = 1.0f;
    float followWeight = 3.0f; // Increased to follow leader more strongly

    // Buffer Zone Settings
    float stopDistance = 6.0f;    // Increased - sheep stop farther from leader
    float startDistance = 10.0f;  // Increased - larger buffer zone
    bool isIdle = false;

    // Damping
    [SerializeField] private float maxSeparationForce = 2.5f;
    [SerializeField] private float deadZoneRadius = 0.5f; // Area where forces are ignored

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        speed = Random.Range(manager.minSpeed, manager.maxSpeed) * speedMultiplier;
        
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        if (manager == null) return;

        float startTime = Time.realtimeSinceStartup;
        Vector3 moveDirection = CalculateFlocking();
        lastCalcTime = (Time.realtimeSinceStartup - startTime) * 1000f; // ms
        moveDirection.y = 0;

        // Check if we're basically at target position - create a "dead zone"
        if (moveDirection.magnitude < deadZoneRadius && isIdle)
        {
            // We're close enough - just stop completely
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        if (moveDirection.magnitude > 1.0f)
        {
            moveDirection.Normalize();
        }

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // A) Rotation
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));

            // B) Movement with speed multiplier based on distance
            float speedMultiplier = isIdle ? 0.3f : 1.0f; // Move slower when idle
            Vector3 targetVelocity = moveDirection * speed * speedMultiplier;

            Vector3 smoothVel = Vector3.Lerp(
                new Vector3(rb.velocity.x, 0, rb.velocity.z),
                new Vector3(targetVelocity.x, 0, targetVelocity.z),
                movementSmoothness * Time.fixedDeltaTime
            );

            rb.velocity = new Vector3(smoothVel.x, rb.velocity.y, smoothVel.z);
        }
        else
        {
            // Gradual stop
            Vector3 currentVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            Vector3 dampedVel = Vector3.Lerp(currentVel, Vector3.zero, 8f * Time.fixedDeltaTime);
            rb.velocity = new Vector3(dampedVel.x, rb.velocity.y, dampedVel.z);
        }
    }

    Vector3 CalculateFlocking()
    {
        Vector3 leaderPosOnGround = new Vector3(manager.leader.position.x, transform.position.y, manager.leader.position.z);
        float distToLeader = Vector3.Distance(transform.position, leaderPosOnGround);
        Vector3 follow = Vector3.zero;

        // --- IMPROVED HYSTERESIS ---
        if (isIdle)
        {
            if (distToLeader > startDistance)
            {
                isIdle = false;
            }
        }
        else
        {
            if (distToLeader < stopDistance)
            {
                isIdle = true;
            }
        }

        // Calculate follow force with distance-based intensity
        if (!isIdle)
        {
            // Far from leader - follow strongly
            float followIntensity = Mathf.Clamp01((distToLeader - stopDistance) / (startDistance - stopDistance));
            follow = (leaderPosOnGround - transform.position).normalized * followIntensity;
        }
        else
        {
            // Close to leader - very weak follow or none
            if (distToLeader > stopDistance + 1.0f)
            {
                // Only follow if drifting away from the stop zone
                follow = (leaderPosOnGround - transform.position).normalized * 0.2f;
            }
            // else: stay put, don't follow at all
        }

        Vector3 cohesion = Vector3.zero;
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        int groupSize = 0;

        // --- NONALLOC OPTIMIZATION ---
        // Use Physics.OverlapSphereNonAlloc instead of looping through all sheep
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            manager.neighborDistance,
            neighborBuffer,
            manager.sheepLayer
        );

        for (int i = 0; i < count; i++)
        {
            Collider c = neighborBuffer[i];
            
            // Skip self
            if (c.gameObject == this.gameObject) continue;

            Vector3 neighborPos = c.transform.position;
            float dist = Vector3.Distance(neighborPos, transform.position);

            // Cohesion - move towards group center
            cohesion += neighborPos;
            
            // Alignment - match group direction
            alignment += c.transform.forward;

            // Separation - avoid crowding
            if (dist < 2.5f) // Slightly increased separation distance
            {
                // Distance-based separation with smooth falloff
                float separationStrength = (2.5f - dist) / 2.5f; // 0 to 1
                Vector3 separationDir = (transform.position - neighborPos).normalized;
                Vector3 separationForce = separationDir * separationStrength * separationStrength; // Squared for sharper falloff

                separationForce = Vector3.ClampMagnitude(separationForce, maxSeparationForce);
                separation += separationForce;
            }
            groupSize++;
        }

        // Only apply cohesion/alignment when NOT idle or when far from group
        float flockingInfluence = isIdle ? 0.3f : 1.0f;

        if (groupSize > 0)
        {
            Vector3 averagePos = cohesion / groupSize;
            averagePos.y = transform.position.y;
            Vector3 cohesionDir = (averagePos - transform.position).normalized;
            Vector3 alignmentDir = (alignment / groupSize).normalized;

            // CRITICAL: Reduce cohesion when near leader to prevent clustering
            float distanceToGroupCenter = Vector3.Distance(transform.position, averagePos);
            float cohesionDamping = Mathf.Clamp01(distanceToGroupCenter / 3.0f); // Reduce cohesion when within 3m of center

            return (cohesionDir * cohesionWeight * flockingInfluence * cohesionDamping) +
                   (separation * separationWeight) + // Separation always active
                   (alignmentDir * alignmentWeight * flockingInfluence) +
                   (follow * followWeight);
        }

        return follow * followWeight;
    }
}