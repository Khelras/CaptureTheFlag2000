using UnityEngine;

public class Flag : MonoBehaviour
{
    public int ownerTeamID; // 0 = Player's Team, 1 = Enemy's Team
    public bool isCaptured = false;
    public bool isBeingCarried = false;
    public Transform baseParent;
    public Vector3 homePosition;

    private Agent carrier;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Save the Base Parent and the Home Position
        this.baseParent = this.transform.parent;
        this.homePosition = this.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Agent agent = other.GetComponent<Agent>();
        if (agent == null) return;
        if (agent.isImprisoned) return;
        if (this.isBeingCarried == true || this.isCaptured == true) return; // Flag is already Taken
        if (agent.teamID == ownerTeamID) return; // Cannot steal your own Flag
        if (agent.carriedFlag != null) return; // Agent is already carrying a Flag
        
        // Pickup the Flag
        this.PickUp(agent);
    }

    void PickUp(Agent agent)
    {
        // Update Flag and Agent States
        this.isBeingCarried = true;
        this.carrier = agent;
        agent.carriedFlag = this;
        agent.state = AgentState.CarryingFlag;

        // Attach the Flag to the Agent
        this.transform.SetParent(agent.transform);
        this.transform.localPosition = new Vector3(0.3f, 0.6f, 0f);
    }

    public void ReturnToHome()
    {
        // Reset Flag States
        this.isCaptured = false;
        this.isBeingCarried = false;

        // Original Flag Position
        this.transform.SetParent(this.baseParent);
        this.transform.position = homePosition;
    }
}
