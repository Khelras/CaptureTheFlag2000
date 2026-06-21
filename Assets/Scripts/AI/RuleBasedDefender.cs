using System.Linq;
using UnityEngine;

public class RuleBasedDefender : MonoBehaviour
{
    private Agent self;
    private SteeringBehaviours steering;

    [Header("Detection")]
    public float detectionRadius = 6f;

    // Working Memory
    private bool enemyInMyTerritory;
    private bool enemyCarryingMyFlag;
    private bool myPrisonNeedsGuards;
    private Agent assignedTarget;

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
        if (self.isPlayerControlled) return; // Ensure the Agent is NOT already controlled by the Player
        if (this.self.isImprisoned) return; // Ensure the Agent is NOT in Prison

        this.UpdateWorkingMemory();
        this.ExecuteRules();
    }

    public void AssignTarget(Agent target) => this.assignedTarget = target;
    public void ClearTarget() => this.assignedTarget = null;

    void UpdateWorkingMemory()
    {
        int enemyTeam = (this.self.teamID == 0) ? 1 : 0;
        var enemies = (enemyTeam == 0) ? GameManager.Instance.teamPlayer : GameManager.Instance.teamEnemy;

        // Defender Memory
        this.enemyInMyTerritory = false;
        this.enemyCarryingMyFlag = false;

        // Loop through all the Enemy Agents
        foreach (var enemy in enemies)
        {
            // Enemies in Prison are not a Threat themselves
            if (enemy.isImprisoned) continue;

            // Check if this Enemy is within my Territory
            if (GameManager.Instance.IsInEnemyTerritory(enemy) == true)
            {
                // This Enemy is within my Territory
                this.enemyInMyTerritory = true;
                
                // This Enemy is also carrying my Flag
                if (enemy.carriedFlag != null) enemyCarryingMyFlag = true;
            }
        }

        // My Prison needs Guards if we have Prisoners present
        this.myPrisonNeedsGuards = GameManager.Instance.GetImprisonedAgents(enemyTeam).Count > 0;
    }

    // Forward Chaining — Fire the First matching Rule.
    // Therefore, the Rules with a Higher Priority should be closer to the First in the order.
    void ExecuteRules()
    {
        // Rule 1: IF Assigned to a Specific Target, PURSUE them
        if (this.assignedTarget != null && this.assignedTarget.isImprisoned == false)
        {
            this.self.state = AgentState.Defending;
            this.steering.ApplySteering(this.steering.Pursue(this.assignedTarget.GetComponent<Rigidbody2D>()));
            return;
        }

        // Rule 2: IF an Enemy is Carrying my Flag, PURSUE the nearest Enemy with a Flag
        if (this.enemyCarryingMyFlag == true)
        {
            this.self.state = AgentState.Defending;
            Agent carrier = this.GetNearestEnemyWithFlag(); // Nearest Enemy with a Flag
            if (carrier != null)
                this.steering.ApplySteering(this.steering.Pursue(carrier.GetComponent<Rigidbody2D>()));
            
            return;
        }

        // Rule 3: IF an Enemy is in our Territory, PURSUE the nearest Enemy
        if (this.enemyInMyTerritory == true)
        {
            this.self.state = AgentState.Defending;
            Agent intruder = this.GetNearestEnemyInMyTerritory(); // Nearest Enemy
            if (intruder != null)
                this.steering.ApplySteering(this.steering.Pursue(intruder.GetComponent<Rigidbody2D>()));
            
            return;
        }

        // Rule 4: Default Rule, PATROL
        this.ExecutePatrolRole();
    }

    // Patrol Target Assigned by the Respective Team Manager
    [HideInInspector] public Vector2 patrolTarget;

    void ExecutePatrolRole()
    {
        self.state = AgentState.Defending;

        // Patrol Duty based on their Patrol Target Assigned by the Respective Team Manager
        if (this.patrolTarget != Vector2.zero) this.steering.ApplySteering(this.steering.Arrive(patrolTarget));
    }

    Agent GetNearestEnemyWithFlag()
    {
        // Enemy Team
        int enemyTeam = self.teamID == 0 ? 1 : 0;
        var enemies = enemyTeam == 0 ? GameManager.Instance.teamPlayer : GameManager.Instance.teamEnemy;

        // Return the Nearest Enemy with a Flag
        return enemies
            .Where(e => !e.isImprisoned && e.carriedFlag != null)
            .OrderBy(e => Vector2.Distance(this.transform.position, e.transform.position))
            .FirstOrDefault();
    }

    Agent GetNearestEnemyInMyTerritory()
    {
        // Enemy Team
        int enemyTeam = self.teamID == 0 ? 1 : 0;
        var enemies = (enemyTeam == 0) ? GameManager.Instance.teamPlayer : GameManager.Instance.teamEnemy;

        // Return the Nearest Enemy within my Territory
        return enemies
            .Where(e => e.isImprisoned == false && GameManager.Instance.IsInEnemyTerritory(e) == true)
            .OrderBy(e => Vector2.Distance(this.transform.position, e.transform.position))
            .FirstOrDefault();
    }

    // Visualise Detection Radius in the Editor
    void OnDrawGizmosSelected()
    {
        // Opaque Green
        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(this.transform.position, this.GetComponent<RuleBasedDefender>()?.detectionRadius ?? 1f);
    }
}
