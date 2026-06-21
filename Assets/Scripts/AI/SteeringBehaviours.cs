using UnityEngine;

public class SteeringBehaviours : MonoBehaviour
{
    [Header("Steering Settings")]
    public float maxSpeed = 5f;
    public float maxForce = 10f;
    public float slowingRadius = 2f;

    [Header("Obstacle Avoidance")]
    public float obstacleDetectionDistance = 5f;
    public LayerMask obstacleLayer;

    [HideInInspector] public Vector2 velocity;
    private Rigidbody2D rb;

    void Awake()
    {
        this.rb = GetComponent<Rigidbody2D>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        
    }

    public void ApplySteering(Vector2 steeringForce)
    {
        // Apply Steering with Obstacle Avoidance
        Vector2 obstacleAvoid = this.ObstacleAvoidance();
        Vector2 combined = steeringForce + obstacleAvoid * 3f;

        // Apply Acceleration to Velocity
        this.velocity += combined * Time.fixedDeltaTime;
        this.velocity = Vector2.ClampMagnitude(this.velocity, maxSpeed);

        // Apply Velocity to the Position
        Vector2 desiredPosition = rb.position + this.velocity * Time.fixedDeltaTime;
        desiredPosition = this.ClampToCameraBounds(desiredPosition); // Clamp to camera bounds
        desiredPosition = this.ResolveObstacleCollision(desiredPosition); // Obstacle Collisions
        this.rb.MovePosition(desiredPosition);
    }

    public Vector2 ObstacleAvoidance()
    {
        Vector2 avoidance = Vector2.zero;

        // Scale detection with speed
        float detectionLength = this.obstacleDetectionDistance +
            (this.velocity.magnitude / maxSpeed) * this.obstacleDetectionDistance;

        // Forward Vector and Right Perpendicular
        Vector2 forward = this.velocity.sqrMagnitude > 0.01f ? this.velocity.normalized : Vector2.up;
        Vector2 right = new Vector2(forward.y, -forward.x);

        // Find all Colliding Obstacles within the Detection Range
        Collider2D[] hits = Physics2D.OverlapCircleAll(this.rb.position, detectionLength, this.obstacleLayer);
        if (hits.Length == 0) return Vector2.zero;

        // Radius of the Agent
        float agentRadius = GetComponent<CircleCollider2D>().radius;

        // Find the Closest Obstacle relative to the Local Space of the Agent
        float closestLocalX = float.MaxValue;
        float closestLocalY = 0f;
        float closestObstacleRadius = 0f;

        // Loop through the Obstacles
        foreach (var hit in hits)
        {
            // Direction to the Obstacle
            Vector2 toObstacle = (Vector2)hit.bounds.center - this.rb.position;

            // Transform to Local Space Relative to the Agent
            float localX = Vector2.Dot(toObstacle, forward);
            float localY = Vector2.Dot(toObstacle, right);

            // Ignore obstacles behind the agent
            if (localX < 0) continue;

            // Ignore Obstacles that are too far to the Side
            float obstacleRadius = hit.bounds.extents.magnitude;
            float expandedRadius = obstacleRadius + agentRadius;
            if (Mathf.Abs(localY) > expandedRadius) continue;

            // Keep the Smallest localX as the Immediate Threat
            if (localX < closestLocalX)
            {
                // Closest Obstacle
                closestLocalX = localX;
                closestLocalY = localY;
                closestObstacleRadius = obstacleRadius;
            }
        }

        // There was no Obstacle
        if (closestLocalX == float.MaxValue) return Vector2.zero;

        // Scale Force by how close the Agent is to the Obstacle
        float distanceFactor = 1f - Mathf.Clamp01(closestLocalX / detectionLength);
        float urgency = distanceFactor * distanceFactor; // Boost it so Force ramps up Sharply when very Close

        // Lateral Force that pushes away from the Obstacles Lateral to the Agent
        // Push Strength is based on HOW ALIGNED the Agent is with the Obstacle Center.
        // When localY is near 0 then the Obstacle is Dead Ahead so we get MAXIMUM lateral push.
        float expandedR = closestObstacleRadius + agentRadius;
        float lateralOverlap = 1f - Mathf.Clamp01(Mathf.Abs(closestLocalY) / expandedR);

        // Steer AWAY from whichever Side the Obstacle Center is on.
        float steerSign = closestLocalY >= 0f ? -1f : 1f;
        Vector2 lateralForce = steerSign * right * lateralOverlap * urgency * maxSpeed * 1.5f;

        
        // Only Brake significantly when very close AND Obstacle is ahead.
        // Keep Braking weaker than Lateral Force to allow the Agent to Steer around the Obstacle
        Vector2 brakingForce = -forward * urgency * maxSpeed * 0.3f;

        // Obstacle Avoidance
        avoidance = lateralForce + brakingForce;
        return avoidance;
    }

    Vector2 ResolveObstacleCollision(Vector2 desiredPosition)
    {
        // Agent Radius
        float agentCircleRadius = GetComponent<CircleCollider2D>().radius;

        // Check if the Desired Position overlaps any Obstacle
        Collider2D hit = Physics2D.OverlapCircle(desiredPosition, agentCircleRadius, this.obstacleLayer);

        // There is no Wall and can therefore move Freely
        if (hit == null) return desiredPosition;

        // Find the Closest Point on the Obstacle Collider to our Current Position
        Vector2 currentPosition = this.rb.position;
        Vector2 closestPoint = hit.ClosestPoint(currentPosition);

        // Push Out Direction from the Obstacle Surface toward Agent
        Vector2 pushDirection = (currentPosition - closestPoint).normalized;

        // Slide along the Obstacle
        Vector2 movement = desiredPosition - currentPosition;
        Vector2 slideMovement = movement - Vector2.Dot(movement, -pushDirection) * (-pushDirection);
        Vector2 resolvedPosition = currentPosition + slideMovement;

        // Double Check to make sure the Resolved Position also does not overlap with an Obstacle
        if (Physics2D.OverlapCircle(resolvedPosition, agentCircleRadius, obstacleLayer) != null)
            return currentPosition; // Fully Blocked and cannot Move

        return resolvedPosition;
    }

    Vector2 ClampToCameraBounds(Vector2 position)
    {
        // Keeps within the Bounds of the Camera
        Camera cam = Camera.main;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        // Clamping
        float agentRadius = 0.36f / 2.0f;
        float x = Mathf.Clamp(position.x, -halfWidth + agentRadius, halfWidth - agentRadius);
        float y = Mathf.Clamp(position.y, -halfHeight + agentRadius, halfHeight - agentRadius);

        return new Vector2(x, y);
    }

    // --------------------------------------------------
    // STEERING BEHAVIOURS
    // --------------------------------------------------

    public Vector2 Seek(Vector2 targetPosition)
    {
        // Get the Desired Velocity and the Steering Force
        Vector2 desired = (targetPosition - this.rb.position).normalized * this.maxSpeed;
        Vector2 steering = desired - this.velocity;

        // Seek Steering Force limited to the Max Force
        return Vector2.ClampMagnitude(steering, this.maxForce);
    }

    public Vector2 Flee(Vector2 targetPosition)
    {
        // Get the Desired Velocity and the Steering Force
        Vector2 desired = (this.rb.position - targetPosition).normalized * this.maxSpeed;
        Vector2 steering = desired - this.velocity;

        // Flee Steering Force limited to the Max Force
        return Vector2.ClampMagnitude(steering, this.maxForce);
    }

    public Vector2 Arrive(Vector2 targetPosition)
    {
        // Get the Desired Velocity and Distance
        Vector2 desired = targetPosition - this.rb.position;
        float distance = desired.magnitude;

        // Deceleration upon reaching the Slowing Radius
        float speed = (distance < this.slowingRadius) // Ternary (check is within Slowing Radius)
            ? this.maxSpeed * (distance / this.slowingRadius) // Decelerate
            : this.maxSpeed; // Max Speed Otherwise.

        // Get the Desired Velocity and the Steering Force
        desired = desired.normalized * speed;
        Vector2 steering = desired - this.velocity;

        // Arrive Steering Force limited to the Max Force
        return Vector2.ClampMagnitude(steering, this.maxForce);
    }

    public Vector2 Pursue(Rigidbody2D target)
    {
        // Position of the Target
        Vector2 toTarget = target.position - (Vector2)this.transform.position;

        // Prediction of WHERE the Target will BE
        float lookAhead = Mathf.Max(0.5f, toTarget.magnitude / this.maxSpeed);
        Vector2 predictedPosition = (Vector2)target.transform.position + target.linearVelocity * lookAhead;

        // Seek towards the Predicted Position
        return Seek(predictedPosition);
    }

    public Vector2 Evade(Rigidbody2D threat)
    {
        // Position of the Threat
        Vector2 toThreat = (Vector2)threat.transform.position - (Vector2)this.transform.position;

        // Prediction of WHERE the Threat will BE
        float lookAhead = toThreat.magnitude / this.maxSpeed;
        Vector2 predictedPosition = (Vector2)threat.transform.position + threat.linearVelocity * lookAhead;

        // Flee away from the Predicted Position
        return Flee(predictedPosition);
    }
}
