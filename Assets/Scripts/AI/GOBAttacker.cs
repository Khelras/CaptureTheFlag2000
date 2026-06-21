using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GOBAttacker : MonoBehaviour
{
    private Agent self;
    private SteeringBehaviours steering;
    private GOBDecisionMaking gob;

    [Header("Decision Intervals")]
    public float decisionInterval = 1.5f; // Re-evaluate GOB every N seconds
    private float decisionTimer;

    private AIGoal currentGoal;
    private Agent nearestThreat;

    [Header("Threat Awareness")]
    public float immediateFleeRadius = 2.5f;
    public float threatDetectionRadius = 4f;

    void Awake()
    {
        this.self = GetComponent<Agent>();
        this.steering = GetComponent<SteeringBehaviours>();
        this.gob = GetComponent<GOBDecisionMaking>();
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
        // Reset the Nearest Threat for Update
        nearestThreat = null;

        // Check if the Agent was previously Threatened
        bool wasThreatened = this.nearestThreat != null;

        // Agent is on their own Side so Enemy Agents are not a Threat
        if (GameManager.Instance.IsInEnemyTerritory(self) == false)
        {
            // If the Agent was previously Threatened
            if (wasThreatened)
            {
                // Reset their Steering Velocity
                steering.velocity = Vector2.zero;
            }

            // Do NOT proceed and check for a Threat
            return;
        }

        // Find the Closest Threat
        float closest = this.threatDetectionRadius;
        int enemyTeam = this.self.teamID == 0 ? 1 : 0;
        var enemies = (enemyTeam == 0) ? GameManager.Instance.teamPlayer : GameManager.Instance.teamEnemy;

        // Loop through all the Enemy Agents
        foreach (var enemy in enemies)
        {
            // Ignore Imprisoned Enemies
            if (enemy.isImprisoned) continue;

            // Distance between the this Agent and the Enemy Agent
            float distance = Vector2.Distance(this.transform.position, enemy.transform.position);
            if (distance < closest) // Check if this is the Closest Enemy Agent
            { 
                // The Closest Enemy Agent is the Nearest Threat
                closest = distance; 
                this.nearestThreat = enemy; 
            }
        }
    }

    Vector2 GetSteerWithThreatResponse(Vector2 goalSteering)
    {
        if (GameManager.Instance.IsInEnemyTerritory(self) == false) return goalSteering; // Safe on Own Side
        if (this.nearestThreat == null) return goalSteering; // There is no Immediate Threat

        // Distance between the Agent and the Nearest Threat
        float distance = Vector2.Distance(this.transform.position, this.nearestThreat.transform.position);

        // Home Side X-Position
        float homeX = (self.teamID == 0) ? 3f : -3f; // +X for Player and -X for Enemy
        Vector2 homeDirection = new Vector2(homeX, transform.position.y);
        Vector2 seekToHome = this.steering.Seek(homeDirection);

        // The Threat is too close, Purely Evade
        if (distance < this.immediateFleeRadius)
        {
            // Evade
            Vector2 evade = this.steering.Evade(this.nearestThreat.GetComponent<Rigidbody2D>());

            // Blend Evade with Seek to Home to 'Evade to Safety'
            Vector2 evadeToSafety = (evade + seekToHome).normalized * this.steering.maxSpeed;
            return evadeToSafety;
        }

        // Threat is within a Medium Range, Blend and have Evade Weight Scale with Proximity
        if (distance < this.threatDetectionRadius)
        {
            // Evade
            Vector2 evade = this.steering.Evade(this.nearestThreat.GetComponent<Rigidbody2D>());

            // Blend Evade with 'Seek to Home' to 'Evade to Safety'
            Vector2 evadeToSafety = (evade + seekToHome * 0.5f).normalized * this.steering.maxSpeed;

            // Blend between the Steering Goal with 'Evade to Safety'
            float t = 1f - ((distance - this.immediateFleeRadius) / (this.threatDetectionRadius - this.immediateFleeRadius));
            return Vector2.Lerp(goalSteering, this.steering.Evade(this.nearestThreat.GetComponent<Rigidbody2D>()), t * 0.6f);
        }

        // Threat is Far away, Pure Goal Steering
        return goalSteering;
    }

    void ExecuteGoal()
    {
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
            // Steer to the Score Zone with Awareness for Threats
            ScoreZone scoreZone = GameManager.Instance.GetScoreZone(this.self.teamID);
            Vector2 scoreZonePosition = scoreZone.transform.position;
            Vector2 steerToScoreZone = this.GetSteerWithThreatResponse(this.steering.Arrive(scoreZone.transform.position));
            this.steering.ApplySteering(steerToScoreZone);
            return;
        }

        // Seek the Nearest Uncaptured Enemy Flag
        var flags = GameManager.Instance.GetUncapturedFlags(enemyTeamID);
        if (flags.Count == 0) return; // There is no Flags to Seek

        // Agent is wanting to get a Flag
        this.self.state = AgentState.MovingToFlag;
        Flag target = flags.OrderBy(f => Vector2.Distance(transform.position, f.transform.position)).First(); // Nearest

        // Steer to Flag with an Awareness for Threats
        Vector2 steerToFlag = this.GetSteerWithThreatResponse(this.steering.Seek(target.transform.position));
        this.steering.ApplySteering(steerToFlag);
    }

    void ExecuteFreeTeammate()
    {
        // Agent is wanting to Free a Teammate from Pision
        this.self.state = AgentState.MovingToPrison;
        Vector2 prisonPosition = GameManager.Instance.GetPrisonZone((this.self.teamID == 0) ? 1 : 0).transform.position;

        // Steer to the Prison with Awareness for Threats
        Vector2 steerToPrison = this.GetSteerWithThreatResponse(this.steering.Seek(prisonPosition));
        this.steering.ApplySteering(steerToPrison);
    }

    // Visualise Threat Detection Radius in the Editor
    void OnDrawGizmosSelected()
    {
        // Opaque Blue
        Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
        Gizmos.DrawWireSphere(this.transform.position, this.GetComponent<GOBAttacker>()?.threatDetectionRadius ?? 1f);

        // Opaque Purple
        Gizmos.color = new Color(0.5f, 0f, 0.5f, 0.5f);
        Gizmos.DrawWireSphere(this.transform.position, this.GetComponent<GOBAttacker>()?.immediateFleeRadius ?? 1f);
    }
}
