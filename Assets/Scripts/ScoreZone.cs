using UnityEngine;

public class ScoreZone : MonoBehaviour
{
    public int ownerTeamID; // 0 = Player's Team, 1 = Enemy's Team

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Agent agent = other.GetComponent<Agent>();
        if (agent == null) return; // Ensure it is an Agent
        if (agent.teamID != this.ownerTeamID) return; // Ensure the Agent's Team
        if (agent.carriedFlag == null) return; // Ensure the Agent is carrying a Flag

        // Agent Scored
        ScoreFlag(agent);
    }

    void ScoreFlag(Agent agent)
    {
        Flag flag = agent.carriedFlag;

        // Mark flag as captured
        flag.isBeingCarried = false;
        flag.isCaptured = true;
        flag.transform.SetParent(flag.baseParent);

        // Place it visually in the score zone
        flag.transform.position = this.transform.position;
        flag.gameObject.SetActive(false); // Hide the Flag

        // Reset Agent State
        agent.carriedFlag = null;
        agent.state = AgentState.Idle;

        // Update Score UI and Check for a Win
        GameManager.Instance.OnFlagScored(this.ownerTeamID);
    }
}
