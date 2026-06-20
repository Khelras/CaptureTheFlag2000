using System.Linq;
using UnityEngine;

public class RuleBasedDefender : MonoBehaviour
{
    private Agent self;
    private SteeringBehaviours steering;

    [Header("Detection")]
    public float detectionRadius = 5f;

    // Working Memory
    private bool enemyInTerritory;
    private bool enemyCarryingFlag;
    private bool prisonerNeedsGuard;
    private Agent closestEnemy;

    void Awake()
    {
        this.self = GetComponent<Agent>();
        this.steering = GetComponent<SteeringBehaviours>();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.gameStarted == false) return; // Ensure Game is Playing
        if (this.self.isImprisoned) return; // Ensure the Agent is NOT in Prison

        this.UpdateWorkingMemory();
        this.ExecuteRules();
    }

    void UpdateWorkingMemory()
    {
        // Reset the Closest Enemy Memory
        this.closestEnemy = null;
        float closestDistance = this.detectionRadius;
        int enemyTeam = (this.self.teamID == 0) ? 1 : 0;

        // Loop through the all Enemy Agents
        foreach (var enemy in ((enemyTeam == 0) ? GameManager.Instance.teamPlayer : GameManager.Instance.teamEnemy))
        {
            // Ignore if the Enemy Agent is in Prison
            if (enemy.isImprisoned) continue;

            // Distance between this Agent and the Enemy Agent
            float distance = Vector2.Distance(this.transform.position, enemy.transform.position);
            if (distance < closestDistance) // Check if this Distance is the Closest
            {
                // Save the Closest Distanced Enemy
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        // Memory Updated
        this.enemyInTerritory = this.closestEnemy != null && GameManager.Instance.IsInEnemyTerritory(this.closestEnemy) == false;
        this.enemyCarryingFlag = this.closestEnemy != null && this.closestEnemy.carriedFlag != null;
        this.prisonerNeedsGuard = GameManager.Instance.GetImprisonedAgents(enemyTeam).Count > 0;
    }

    // Forward Chaining — Fire the First matching Rule.
    // Therefore, the Rules with a Higher Priority should be closer to the First in the order.
    void ExecuteRules()
    {
        // Rule 1: IF an Enemy is Carrying a Flag, PURSUE the Enemy
        if (this.enemyCarryingFlag == true && this.closestEnemy != null)
        {
            this.self.state = AgentState.Defending;
            this.steering.ApplySteering(this.steering.Pursue(this.closestEnemy.GetComponent<Rigidbody2D>()));
            return;
        }

        // Rule 2: IF an Enemy is in our Territory, PURSUE the Enemy
        if (this.enemyInTerritory == true && this.closestEnemy != null)
        {
            this.self.state = AgentState.Defending;
            this.steering.ApplySteering(this.steering.Pursue(this.closestEnemy.GetComponent<Rigidbody2D>()));
            return;
        }

        // Rule 3: IF there are Prisoners in our Prison, PATROL near the Prison
        if (this.prisonerNeedsGuard == true)
        {
            this.self.state = AgentState.Defending;
            Vector2 prisonPosition = GameManager.Instance.GetPrisonZone((this.self.teamID == 0) ? 1 : 0).transform.position;
            this.steering.ApplySteering(this.steering.Arrive(prisonPosition));
            return;
        }

        // Rule 4: Default Rule, PATROL near our own Flags
        this.self.state = AgentState.Defending;
        this.PatrolFlags();
    }

    void PatrolFlags()
    {
        // Arrive at the Nearest Uncaptured Flag to Guard it
        int myTeam = self.teamID;
        var myFlags = (myTeam == 0) ? GameManager.Instance.teamPlayerFlags : GameManager.Instance.teamEnemyFlags;

        var nearestFlag = myFlags
            .Where(f => f.isCaptured  == false) // Uncaptured Flags
            .OrderBy(f => Vector2.Distance(this.transform.position, f.transform.position)) // Closest
            .FirstOrDefault(); // First

        // Patrol and Guard the Nearest Uncaptured Flag
        if (nearestFlag != null) this.steering.ApplySteering(this.steering.Arrive(nearestFlag.transform.position));
    }
}
