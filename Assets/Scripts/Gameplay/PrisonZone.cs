using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrisonZone : MonoBehaviour
{
    public int ownerTeamID; // The team whose agents get imprisoned here

    // Slot Layout
    private int columns = 2;
    private int rows = 2;

    // Tracks which Agent occupies which Slot Index
    private Dictionary<int, Agent> slots;

    private void Awake()
    {
        this.slots = new Dictionary<int, Agent>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ImprisonAgent(Agent agent)
    {
        // Imprison the Agent
        agent.isImprisoned = true;
        agent.state = AgentState.Imprisoned;
        agent.prisonSlotIndex = this.OccupySlot(agent);
        agent.rb.position = this.SlotIndexToWorldPosition(agent.prisonSlotIndex);

        // Check Win Condition
        GameManager.Instance.CheckWinCondition();
    }

    private int OccupySlot(Agent agent)
    {
        int slotIndex = 0;
        while (this.slots.ContainsKey(slotIndex)) slotIndex++;
        this.slots[slotIndex] = agent;
        return slotIndex;
    }

    private Vector2 SlotIndexToWorldPosition(int index)
    {
        // If the given Index is more than the Slots
        int maxSlots = this.rows * this.columns;
        if ((index + 1) > maxSlots) return (Vector2)this.transform.position;

        // Lay Slots out in a Grid Relative to the Center of the Prison Zone
        int col = index % this.columns;
        int row = index / this.rows;

        float offsetX = col - ((this.columns - 1f) / 2f);
        float offsetY = -(row - ((this.rows - 1f) / 2f));

        return (Vector2)this.transform.position + new Vector2(offsetX, offsetY);
    }

    public void FreeSlot(int slotIndex)
    {
        this.slots.Remove(slotIndex);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // The Agent Entering the Prison Zone
        Agent agent = other.GetComponent<Agent>();
        if (agent == null) return;
        if (agent.isImprisoned) return; // Agent is already in Prison
        if (agent.teamID == ownerTeamID) return; // Ignore the Defending Agents
        if (agent.carriedFlag != null) return; // Agent cannot free another Agent if they are Carrying a Flag

        // This Agent is from the Opposing Team and Entering the Prison to free their Teammate
        var imprisonedAgents = GameManager.Instance.GetImprisonedAgents(agent.teamID);
        Agent imprisonedAgent = imprisonedAgents.OrderByDescending(a => a.prisonSlotIndex).First();
        if (imprisonedAgent == null) return; // No one to free

        // Free the Imprisoned Agent from their slot
        int side = (agent.teamID == 0) ? 1 : 0;
        GameManager.Instance.GetPrisonZone(side).FreeSlot(imprisonedAgent.prisonSlotIndex);

        // Free the Imprisoned Agent
        imprisonedAgent.isImprisoned = false;
        imprisonedAgent.state = AgentState.Idle; // Teleports back to Correct Side
        float sign = (agent.teamID == 0) ? 1f : -1f;
        imprisonedAgent.transform.position = new Vector3(10f * sign, 0f, 0f);
        other.transform.position = new Vector3(9f * sign, 0f, 0f);
    }
}
