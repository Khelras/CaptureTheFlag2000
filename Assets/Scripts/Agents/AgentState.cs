public enum AgentState
{
    Idle, // Agent is not doing anything
    MovingToFlag, // Agent is moving towards the opposing team's flags
    CarryingFlag, // Agent is carrying a flag from the opposing team
    MovingToPrison, // Agent is moving towards the opposing team's prison to free a teammate
    Defending, // Agent is defending either their team's flags or prison
    Imprisoned, // Agent is imprisoned
    PlayerControlled // Aget is being controlled by the Player
}