using UnityEngine;

public class SteeringBehaviours : MonoBehaviour
{
    [Header("Steering Settings")]
    public float maxSpeed = 4f;
    public float maxForce = 8f;
    public float slowingRadius = 1.5f;

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
        // Apply Acceleration to Velocity
        this.velocity += steeringForce * Time.fixedDeltaTime;
        this.velocity = Vector2.ClampMagnitude(this.velocity, maxSpeed);

        // Apply Velocity to the Position
        Vector2 targetPosition = rb.position + this.velocity * Time.fixedDeltaTime;
        targetPosition = ClampToCameraBounds(targetPosition); // Clamp to camera bounds
        this.rb.MovePosition(targetPosition);
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
        Vector2 toTarget = target.position - (Vector2)transform.position;

        // Prediction of WHERE the Target will BE
        float lookAhead = toTarget.magnitude / maxSpeed;
        Vector2 predictedPosition = (Vector2)target.transform.position + target.linearVelocity * lookAhead;

        // Seek towards the Predicted Position
        return Seek(predictedPosition);
    }

    public Vector2 Evade(Rigidbody2D threat)
    {
        // Position of the Threat
        Vector2 toThreat = (Vector2)threat.transform.position - (Vector2)transform.position;

        // Prediction of WHERE the Threat will BE
        float lookAhead = toThreat.magnitude / maxSpeed;
        Vector2 predictedPosition = (Vector2)threat.transform.position + threat.linearVelocity * lookAhead;

        // Flee away from the Predicted Position
        return Flee(predictedPosition);
    }
}
