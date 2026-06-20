using UnityEngine;

public class TagChecker : MonoBehaviour
{
    private Agent self;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.self = GetComponent<Agent>();
    }

    // Update is called once per frame
    void Update()
    {
        // Ignore if in Prison
        if (this.self.isImprisoned) return;

        // Only Tag when on the Agent's OWN territory
        if (GameManager.Instance.CanTag(this.self) == false) return;

        // Check for Agents within the Tag Radius
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, this.self.tagRadius);
        foreach (var col in nearby) // Loop through all of the Agents
        {
            // Check if this Other Agent can be Tagged
            Agent other = col.GetComponent<Agent>();
            if (other == null) continue;
            if (other.teamID == this.self.teamID) continue;  // Skip Agents on Same Team
            if (other.isImprisoned) continue; // Agent already caught
            if (other.state == AgentState.Returning) continue; // Agent just got Freed from Prison

            // The Prison to send the Other Agent to
            Transform prison = (other.teamID == 0)
                ? GameManager.Instance.teamEnemySidePrison // Send to Prison on the Player's Side
                : GameManager.Instance.teamPlayerSidePrison; // Send to Prison on the Enemy's Side

            // Tag the Enemy
            other.GetTagged(prison);
        }
    }

    // Visualise Tag Radius in the Editor
    void OnDrawGizmosSelected()
    {
        // Opaque Red
        Gizmos.color = new Color(1f, 0f, 0f, 0.75f); 
        Gizmos.DrawWireSphere(this.transform.position, this.GetComponent<Agent>()?.tagRadius ?? 1f);
    }
}
