using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerTeamManager : MonoBehaviour
{
    public int playerTeamID = 0; // The Team with a Player Controller
    public float roleReassignInterval = 2f;
    private float timer;

    // The Player Controller
    [SerializeField] private PlayerController playerController;

    // List of Attacker and Defender Agents
    private List<Agent> currentAttackers = new List<Agent>();
    private List<Agent> currentDefenders = new List<Agent>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.gameStarted == false) return;

        // Role Re-Assign Intervals
        this.timer -= Time.deltaTime;
        if (this.timer <= 0f)
        {
            this.AssignRoles();
            this.AssignDefenderPatrols();
            this.timer = this.roleReassignInterval;
        }
    }

    // Call after Agents are Spawned in Game Manager
    public void OnAgentsSpawned()
    {
        this.AssignRoles();
        this.AssignDefenderPatrols();
    }

    void AssignRoles()
    {
        // The Player's Team
        var team = (playerTeamID == 0) ? GameManager.Instance.teamPlayer : GameManager.Instance.teamEnemy;

        // Get all Available Agents
        var available = team.Where(a => a.isImprisoned == false).ToList();
        if (available.Count == 0) return;

        // The Player Controlled Agent counts as an Attacker
        var playerAgent = available.FirstOrDefault(a => a.isPlayerControlled);
        bool playerIsAttacking = playerAgent != null && GameManager.Instance.IsInEnemyTerritory(playerAgent) == true;

        // Max Attackers
        int maxAttackers = Mathf.Min(2, available.Count - 1);

        // Keep Agents as Attackers that are already in the Opposing Team's Territory
        var alreadyAttacking = available
            .Where(a => a.isPlayerControlled == false && GameManager.Instance.IsInEnemyTerritory(a) == true)
            .ToList();

        // Count Player as an Attacker Slot
        int usedSlots = alreadyAttacking.Count + ((playerIsAttacking == true) ? 1 : 0);
        int slotsRemaining = Mathf.Max(0, maxAttackers - usedSlots);
        
        // New AI Attackers
        var newAttackers = available
            .Where(a => a.isPlayerControlled == false && alreadyAttacking.Contains(a) == false)
            .Take(slotsRemaining)
            .ToList();

        // Update the Memory
        this.currentAttackers = alreadyAttacking.Concat(newAttackers).ToList();
        this.currentDefenders = available.Except(currentAttackers).ToList();

        // Loop through each Agent
        foreach (var agent in available)
        {
            // Ignore Agents that are controlled by the Player
            if (agent.isPlayerControlled) continue;

            bool isAttacker = this.currentAttackers.Contains(agent);
            var attacker = agent.GetComponent<GOBAttacker>(); // Goal Oriented Behaviours for Attacking
            var defender = agent.GetComponent<RuleBasedDefender>(); // Rule Based Systems for Defending
            if (attacker != null) attacker.enabled = isAttacker;
            if (defender != null) defender.enabled = !isAttacker;
        }
    }

    void AssignDefenderPatrols()
    {
        // Ensure there are Defenders ready for Patrol Duty
        if (this.currentDefenders.Count == 0) return;

        // Get Enemies currently in our Territory
        int enemyTeam = (this.playerTeamID == 0) ? 1 : 0;
        var enemyTeamAgents = enemyTeam == 0 ? GameManager.Instance.teamPlayer : GameManager.Instance.teamEnemy;
        var intruders = enemyTeamAgents
            .Where(e => e.isImprisoned == false && GameManager.Instance.IsInEnemyTerritory(e) == true)
            .ToList();

        // Prison Patrol Duty if there are Prisoners Present
        bool hasPrisoners = GameManager.Instance.GetImprisonedAgents(enemyTeam).Count > 0;
        Vector2 prisonPosition = GameManager.Instance.GetPrisonZone(this.playerTeamID).transform.position;

        // Flag Patrol Duty
        Vector2 flagPatrolPosition = GameManager.Instance.GetScoreZone(this.playerTeamID).transform.position;

        for (int i = 0; i < this.currentDefenders.Count; i++)
        {
            // Ensure there is a Defender
            var defender = this.currentDefenders[i].GetComponent<RuleBasedDefender>();
            if (defender == null) continue;

            // If there are Intruders Present
            if (i < intruders.Count)
            {
                // Assign each Defender to their Own Intruder to Chase
                defender.AssignTarget(intruders[i]);
                continue;
            }

            // No Intruder to Chase so clear any Assigned Target and Assign a Patrol Duty
            defender.ClearTarget();

            // Defender 0 always Patrols Flags
            // Defender 1 Patrols the Prison IF there are Prisoners, otherwise also Patrol Flags
            if (i == 1 && hasPrisoners) defender.patrolTarget = prisonPosition;
            else defender.patrolTarget = flagPatrolPosition;
        }
    }
}
