using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SheepAgentBVH : MonoBehaviour
{
    [HideInInspector] public FlockManagerBVH manager;

    Rigidbody rb;
    float speed;

    // Shared buffer to avoid allocations (max ~20 neighbors assumed)
    private Collider[] neighborBuffer = new Collider[20];

    // Physics + flock settings
    [Header("Physics Settings")]
    [SerializeField] private float movementSmoothness = 5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float speedMultiplier = 2.0f;

    float cohesionWeight = 1.0f;
    float separationWeight = 2.5f;
    float alignmentWeight = 1.0f;
    float followWeight = 3.0f;

    float stopDistance = 6.0f;
    float startDistance = 10.0f;
    bool isIdle = false;

    [SerializeField] private float maxSeparationForce = 2.5f;
    [SerializeField] private float deadZoneRadius = 0.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (manager != null)
            speed = Random.Range(manager.minSpeed, manager.maxSpeed) * speedMultiplier;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        if (manager == null) return;

        Vector3 moveDirection = CalculateFlocking();
        moveDirection.y = 0;

        // Movement logic (same as previous version)
        if (moveDirection.magnitude < deadZoneRadius && isIdle)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        if (moveDirection.magnitude > 1.0f) moveDirection.Normalize();

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));

            float currentSpeedMultiplier = isIdle ? 0.3f : 1.0f;
            Vector3 targetVelocity = moveDirection * speed * currentSpeedMultiplier;
            Vector3 smoothVel = Vector3.Lerp(
                new Vector3(rb.velocity.x, 0, rb.velocity.z),
                new Vector3(targetVelocity.x, 0, targetVelocity.z),
                movementSmoothness * Time.fixedDeltaTime
            );

            rb.velocity = new Vector3(smoothVel.x, rb.velocity.y, smoothVel.z);
        }
        else
        {
            Vector3 currentVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            Vector3 dampedVel = Vector3.Lerp(currentVel, Vector3.zero, 8f * Time.fixedDeltaTime);
            rb.velocity = new Vector3(dampedVel.x, rb.velocity.y, dampedVel.z);
        }
    }

    Vector3 CalculateFlocking()
    {
        // Basic leader-follow logic
        Vector3 leaderPosOnGround = new Vector3(manager.leader.position.x, transform.position.y, manager.leader.position.z);
        float distToLeader = Vector3.Distance(transform.position, leaderPosOnGround);
        Vector3 follow = Vector3.zero;

        // Idle hysteresis
        if (isIdle) { if (distToLeader > startDistance) isIdle = false; }
        else { if (distToLeader < stopDistance) isIdle = true; }

        if (!isIdle)
        {
            float followIntensity = Mathf.Clamp01((distToLeader - stopDistance) / (startDistance - stopDistance));
            follow = (leaderPosOnGround - transform.position).normalized * followIntensity;
        }
        else if (distToLeader > stopDistance + 1.0f)
        {
            follow = (leaderPosOnGround - transform.position).normalized * 0.2f;
        }

        Vector3 cohesion = Vector3.zero;
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        int groupSize = 0;

        // BVH-based neighbor lookup (non-alloc)
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            manager.neighborDistance,
            neighborBuffer,
            manager.sheepLayer
        );

        // Process neighbors
        for (int i = 0; i < count; i++)
        {
            Collider c = neighborBuffer[i];
            if (c.gameObject == this.gameObject) continue;

            Transform t = c.transform;
            float dist = Vector3.Distance(t.position, transform.position);

            cohesion += t.position;
            alignment += t.forward;

            if (dist < 2.5f)
            {
                float separationStrength = (2.5f - dist) / 2.5f;
                Vector3 separationDir = (transform.position - t.position).normalized;
                Vector3 separationForce = separationDir * separationStrength * separationStrength;
                separation += Vector3.ClampMagnitude(separationForce, maxSeparationForce);
            }
            groupSize++;
        }

        // Combine final steering
        float flockingInfluence = isIdle ? 0.3f : 1.0f;

        if (groupSize > 0)
        {
            Vector3 averagePos = cohesion / groupSize;
            averagePos.y = transform.position.y;

            Vector3 cohesionDir = (averagePos - transform.position).normalized;
            Vector3 alignmentDir = (alignment / groupSize).normalized;

            float distanceToGroupCenter = Vector3.Distance(transform.position, averagePos);
            float cohesionDamping = Mathf.Clamp01(distanceToGroupCenter / 3.0f);

            return (cohesionDir * cohesionWeight * flockingInfluence * cohesionDamping) +
                   (separation * separationWeight) +
                   (alignmentDir * alignmentWeight * flockingInfluence) +
                   (follow * followWeight);
        }

        return follow * followWeight;
    }
}
