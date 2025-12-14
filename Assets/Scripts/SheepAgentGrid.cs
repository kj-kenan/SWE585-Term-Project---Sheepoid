using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SheepAgentGrid : MonoBehaviour
{
    [HideInInspector] public FlockManagerGrid manager;

    Rigidbody rb;
    float speed;

    [Header("Physics Settings")]
    [SerializeField] private float movementSmoothness = 5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float speedMultiplier = 2.0f;

    // Behaviour weights
    float cohesionWeight = 1.0f;
    float separationWeight = 2.5f;
    float alignmentWeight = 1.0f;
    float followWeight = 3.0f;

    // States
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

        Vector3 moveDirection = CalculateFlockingGrid();
        moveDirection.y = 0;

        // Idle dead zone
        if (moveDirection.magnitude < deadZoneRadius && isIdle)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        if (moveDirection.magnitude > 1.0f)
            moveDirection.Normalize();

        // Movement + rotation smoothing
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
            // Slow down naturally
            Vector3 currentVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            Vector3 dampedVel = Vector3.Lerp(currentVel, Vector3.zero, 8f * Time.fixedDeltaTime);
            rb.velocity = new Vector3(dampedVel.x, rb.velocity.y, dampedVel.z);
        }
    }

    Vector3 CalculateFlockingGrid()
    {
        // Leader follow logic
        Vector3 leaderGroundPos = new Vector3(manager.leader.position.x, transform.position.y, manager.leader.position.z);
        float distToLeader = Vector3.Distance(transform.position, leaderGroundPos);
        Vector3 follow = Vector3.zero;

        // Idle state handling
        if (isIdle)
        {
            if (distToLeader > startDistance) isIdle = false;
        }
        else
        {
            if (distToLeader < stopDistance) isIdle = true;
        }

        // Follow intensity
        if (!isIdle)
        {
            float t = Mathf.Clamp01((distToLeader - stopDistance) / (startDistance - stopDistance));
            follow = (leaderGroundPos - transform.position).normalized * t;
        }
        else if (distToLeader > stopDistance + 1.0f)
        {
            follow = (leaderGroundPos - transform.position).normalized * 0.2f;
        }

        Vector3 cohesion = Vector3.zero;
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        int groupSize = 0;

        // --- Spatial Grid ---

        // Current cell
        Vector2Int myGridPos = new Vector2Int(
            Mathf.FloorToInt(transform.position.x / manager.gridSize),
            Mathf.FloorToInt(transform.position.z / manager.gridSize)
        );

        // Scan 3×3 neighbor cells
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int cell = myGridPos + new Vector2Int(x, y);

                if (manager.spatialGrid.TryGetValue(cell, out List<SheepAgentGrid> cellSheep))
                {
                    for (int i = 0; i < cellSheep.Count; i++)
                    {
                        SheepAgentGrid neighbor = cellSheep[i];
                        if (neighbor == this) continue;

                        float dist = Vector3.Distance(neighbor.transform.position, transform.position);

                        if (dist <= manager.neighborDistance)
                        {
                            cohesion += neighbor.transform.position;
                            alignment += neighbor.transform.forward;

                            if (dist < 2.5f)
                            {
                                float s = (2.5f - dist) / 2.5f;
                                Vector3 dir = (transform.position - neighbor.transform.position).normalized;
                                Vector3 force = dir * s * s;
                                force = Vector3.ClampMagnitude(force, maxSeparationForce);
                                separation += force;
                            }

                            groupSize++;
                        }
                    }
                }
            }
        }

        // Apply flocking rules
        float flockMult = isIdle ? 0.3f : 1.0f;

        if (groupSize > 0)
        {
            Vector3 avgPos = cohesion / groupSize;
            avgPos.y = transform.position.y;

            Vector3 cohDir = (avgPos - transform.position).normalized;
            Vector3 alignDir = (alignment / groupSize).normalized;

            float centerDist = Vector3.Distance(transform.position, avgPos);
            float cohDamp = Mathf.Clamp01(centerDist / 3.0f);

            return
                cohDir * cohesionWeight * flockMult * cohDamp +
                separation * separationWeight +
                alignDir * alignmentWeight * flockMult +
                follow * followWeight;
        }

        return follow * followWeight;
    }
}
