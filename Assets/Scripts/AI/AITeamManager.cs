using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AITeamManager : MonoBehaviour
{
    public int aiTeamID = 1; // The Fully AI-Controlled Team
    public float roleReassignInterval = 2f;
    private float timer;

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
            this.timer = this.roleReassignInterval;
        }
    }

    void AssignRoles()
    {
        // The Full AI Team
        var team = (this.aiTeamID == 0) ? GameManager.Instance.teamPlayer : GameManager.Instance.teamEnemy;

        // Get all Available Agents
        var available = team.Where(a => !a.isImprisoned).ToList();
        if (available.Count == 0) return;

        // Count how many Agents are already in Enemy Territory (keep them as Attackers)
        var alreadyAttacking = available
            .Where(a => GameManager.Instance.IsInEnemyTerritory(a))
            .ToList();

        // Max Attackers
        int maxAttackers = 2;
        int attackerSlotsRemaining = maxAttackers - alreadyAttacking.Count;

        // Fill any remaining Attacker Slots
        var newAttackers = available
            .Where(a => alreadyAttacking.Contains(a) == false)
            .Take(Mathf.Max(0, attackerSlotsRemaining))
            .ToList();

        // List of all Attackers
        var allAttackers = alreadyAttacking.Concat(newAttackers).ToList();

        foreach (var agent in available)
        {
            bool isAttacker = allAttackers.Contains(agent);
            var attacker = agent.GetComponent<GOBAttacker>(); 
            var defender = agent.GetComponent<RuleBasedDefender>(); 

            if (attacker != null) attacker.enabled = isAttacker; // Goal Oriented Behaviours for Attacking
            if (defender != null) defender.enabled = !isAttacker; // Rule Based Systems for Defending
        }
    }
}
