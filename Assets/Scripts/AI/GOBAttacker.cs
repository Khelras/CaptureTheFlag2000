using System.Linq;
using UnityEngine;

public class GOBAttacker : MonoBehaviour
{
    private Agent self;
    private SteeringBehaviours steering;
    private GOBDecision gob;

    [Header("Decision Intervals")]
    public float decisionInterval = 1.5f; // Re-evaluate GOB every N seconds
    private float decisionTimer;

    private AIGoal currentGoal;
    private Agent nearestThreat;

    [Header("Threat Awareness")]
    public float threatDetectionRadius = 5f;

    [Header("Zones")]
    public ScoreZone scoreZone;

    void Awake()
    {
        this.self = GetComponent<Agent>();
        this.steering = GetComponent<SteeringBehaviours>();
        this.gob = GetComponent<GOBDecision>();
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
        if (GameManager.Instance.gameStarted == false) return;
        if (this.self.isImprisoned == true || this.self.isPlayerControlled == true) return;

        // Re-evaluate GOB Periodically
        this.decisionTimer -= Time.fixedDeltaTime;
        if (this.decisionTimer <= 0f)
        {
            this.currentGoal = this.gob.EvaluateBestGoal();
            this.decisionTimer = this.decisionInterval;
        }

        this.DetectThreats();
        this.ExecuteGoal();
    }

    void DetectThreats()
    {
        // Reset the Nearest Threat
        this.nearestThreat = null;
        float closestDistance = this.threatDetectionRadius;
        int enemyTeam = (this.self.teamID == 0) ? 1 : 0;

        // Loop through the all Enemy Agents
        foreach (var enemy in ((enemyTeam == 0) ? GameManager.Instance.teamPlayer : GameManager.Instance.teamEnemy))
        {
            // Ignore if the Enemy Agent is in Prison
            if (enemy.isImprisoned == true) continue;

            // Distance between this Agent and the Enemy Agent
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance) // Check if this Distance is the Closest
            {
                // Save the Closest Distanced Enemy
                closestDistance = distance; 
                nearestThreat = enemy;
            }
        }
    }

    void ExecuteGoal()
    {
        // Evade if is Threatened and is Carrying a Flag
        if (this.nearestThreat != null && this.self.carriedFlag != null)
        {
            this.self.state = AgentState.CarryingFlag;
            this.steering.ApplySteering(this.steering.Evade(this.nearestThreat.GetComponent<Rigidbody2D>()));
            return;
        }

        // Otherwise, Execute Capture Flag Goal or Free Teammate Goal
        if (this.currentGoal == AIGoal.CaptureFlag) this.ExecuteCaptureFlag();
        else if (this.currentGoal == AIGoal.FreeTeammate) this.ExecuteFreeTeammate();
    }

    void ExecuteCaptureFlag()
    {
        int enemyTeamID = (this.self.teamID == 0) ? 1 : 0;

        // Agent is carrying a Flag
        if (this.self.carriedFlag != null)
        {
            // Seek to the Score Zone
            Vector2 scoreZonePosition = this.scoreZone.transform.position;
            Vector2 seekToScoreZone = this.steering.Seek(scoreZonePosition);
            if (this.nearestThreat != null) // There is a Threat Nearby
            {
                // Blend Seek and Evade
                Vector2 evade = this.steering.Evade(this.nearestThreat.GetComponent<Rigidbody2D>());
                this.steering.ApplySteering((seekToScoreZone + evade * 1.5f) * 0.5f);
            }
            else // There is no Threats Nearby
            {
                // Simple apply the Seek Steering Force to the Score Zone
                this.steering.ApplySteering(seekToScoreZone);
            }

            return;
        }

        // Seek the Nearest Uncaptured Enemy Flag
        var flags = GameManager.Instance.GetUncapturedFlags(enemyTeamID);
        if (flags.Count == 0) return; // There is no Flags to Seek

        // Agent is wanting to get a Flag
        self.state = AgentState.MovingToFlag;
        Flag target = flags.OrderBy(f => Vector2.Distance(transform.position, f.transform.position)).First(); // Nearest

        // Evade Threats while Moving to Flag
        Vector2 seekToFlag = this.steering.Seek(target.transform.position);
        if (this.nearestThreat != null) // There is a Threat Nearby
        {
            // Blend Seek and Evade
            Vector2 evade = this.steering.Evade(this.nearestThreat.GetComponent<Rigidbody2D>());
            this.steering.ApplySteering((seekToFlag + evade * 1.5f) * 0.5f);
        }
        else // There is no Threats Nearby
        {
            // Simple apply the Seek Steering Force to the Flag
            this.steering.ApplySteering(seekToFlag);
        }
    }

    void ExecuteFreeTeammate()
    {
        // Agent is wanting to Free a Teammate from Pision
        this.self.state = AgentState.MovingToPrison;
        Vector2 prisonPosition = GameManager.Instance.GetPrisonZone((this.self.teamID == 0) ? 1 : 0).transform.position;

        // Seek to the Prison
        Vector2 seekToPrison = this.steering.Seek(prisonPosition);
        if (this.nearestThreat != null) // There is a Threat Nearby
        {
            // Blend Seek and Evade
            Vector2 evade = this.steering.Evade(this.nearestThreat.GetComponent<Rigidbody2D>());
            this.steering.ApplySteering((seekToPrison + evade * 1.5f) * 0.5f);
        }
        else // There is no Threats Nearby
        {
            // Simple apply the Seek Steering Force to the Prison
            this.steering.ApplySteering(seekToPrison);
        }
    }
}
