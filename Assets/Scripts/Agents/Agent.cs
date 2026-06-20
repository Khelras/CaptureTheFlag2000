using UnityEngine;
using UnityEngine.InputSystem.XR;

public class Agent : MonoBehaviour
{
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public int prisonSlotIndex = -1;

    [Header("Agent Info")]
    public int teamID; // 0 = Player's Team, 1 = Enemy's Team
    public AgentState state = AgentState.Idle;

    [Header("Agent States")]
    public Flag carriedFlag; // Null if not carrying a Flag
    public bool isImprisoned = false;
    public bool isPlayerControlled = false;

    [Header("Stats")]
    public float moveSpeed = 5f;
    public float tagRadius = 0.5f;

    // For Highlighting the Selected Player-Controlled Agent
    private SpriteRenderer sr;
    private Color baseColor;

    void Awake()
    {
        this.rb = GetComponent<Rigidbody2D>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.sr = GetComponent<SpriteRenderer>();
        this.baseColor = sr.color;
    }

    // Update is called once per frame
    void Update()
    {
        // Ignore if Agent is Controlled by the Player or is in Prison
        if (this.isPlayerControlled == true || this.isImprisoned == true) return;
        this.CheckForTagging();
    }

    void CheckForTagging()
    {
        // Only Check if this Agent CAN Tag
        if (GameManager.Instance.CanTag(this) == false) return;

        // TODO: Wire up to the AI
    }

    public void SetSelected(bool selected)
    {
        this.sr.color = selected ? new Color(0f, 0.9f, 1f, 0.9f) : baseColor;
    }

    public void GetTagged(Transform prisonLocation)
    {
        // Check if the Agent was carrying a Flag
        if (carriedFlag != null)
        {
            // Send the Flag back
            this.carriedFlag.ReturnToHome();
            this.carriedFlag = null;
        }

        // Find the respective Prison Zone and Imprison the Agent
        PrisonZone prison = GameManager.Instance.GetPrisonZone(this.teamID);
        prison.ImprisonAgent(this);
    }
}