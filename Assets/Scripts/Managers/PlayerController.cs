using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Player's Team")]
    public int playerTeamID = 0; // 0 = Player's Team

    [Header("Smooth Movement")]
    public float acceleration = 15f;
    public float deceleration = 20f;

    // Selected Agent and Movement
    [SerializeField] private LayerMask agentLayer;
    private Agent selectedAgent;
    private GameControls controls;
    private Vector2 moveInput;
    private Vector2 smoothVelocity;

    void Awake()
    {
        this.controls = new GameControls();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // If there is a Selected Agent at the Start
        if (selectedAgent != null)
        {
            // Set that Selected Agent to be Player Controlled
            this.selectedAgent.isPlayerControlled = true;
            this.selectedAgent.state = AgentState.PlayerControlled;
            this.selectedAgent.SetSelected(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.gameStarted == false) return;

        // Movement Inputs
        this.moveInput = this.controls.Player.Move.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.gameStarted == false) return;

        // An Agent has been Selected
        if (this.selectedAgent != null)
        {
            // Do Nothing if the Agent is Imprisoned
            if (this.selectedAgent.state == AgentState.Imprisoned) return;

            // Movement
            HandleMovement();
        }
    }

    void OnEnable()
    {
        this.controls.Player.Enable();
        this.controls.Player.SelectAgent.performed += OnSelectAgent;
    }

    void OnDisable()
    {
        this.controls.Player.SelectAgent.performed -= OnSelectAgent;
        this.controls.Player.Disable();
    }

    // Call after Agents are Spawned in Game Manager
    public void OnAgentsSpawned()
    {
        this.selectedAgent = GameManager.Instance.teamPlayer.FirstOrDefault();
        this.selectedAgent.isPlayerControlled = true;
        this.selectedAgent.state = AgentState.PlayerControlled;
        this.selectedAgent.SetSelected(true);
    }

    void OnSelectAgent(InputAction.CallbackContext ctx)
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        Collider2D hit = Physics2D.OverlapPoint(mouseWorld, this.agentLayer);
        if (hit == null) return;

        Agent clicked = hit.GetComponent<Agent>();
        if (clicked == null) return;
        if (clicked.teamID != playerTeamID) return;
        if (clicked.isImprisoned) return;

        // Deselect old Agent
        if (this.selectedAgent != null)
        {
            this.selectedAgent.isPlayerControlled = false;
            this.selectedAgent.SetSelected(false);
        }

        // Select new Agent
        this.selectedAgent = clicked;
        this.selectedAgent.isPlayerControlled = true;
        this.selectedAgent.state = AgentState.PlayerControlled;
        this.selectedAgent.SetSelected(true);

        // Reset the Smooth Velocity so Momentum is not carried over.
        this.smoothVelocity = Vector2.zero;
    }

    void HandleMovement()
    {
        // Apply Acceleration and smooth toward Input Direction using Lerp
        float lerpSpeed = this.moveInput.sqrMagnitude > 0.01f ? this.acceleration : this.deceleration;
        this.smoothVelocity = Vector2.Lerp(this.smoothVelocity, this.moveInput, lerpSpeed * Time.deltaTime);

        // Normalise to prevent faster Diagonals
        if (this.smoothVelocity.sqrMagnitude > 1f) this.smoothVelocity = this.smoothVelocity.normalized;

        // Apply Velocity to the Position
        Vector2 targetPosition = this.selectedAgent.rb.position + this.smoothVelocity * this.selectedAgent.moveSpeed * Time.fixedDeltaTime;
        targetPosition = this.ClampToCameraBounds(targetPosition); // Clamp to camera bounds
        this.selectedAgent.rb.MovePosition(targetPosition);
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
}
