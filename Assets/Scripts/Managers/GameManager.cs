using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Teams")]
    public List<Agent> teamPlayer = new List<Agent>();
    public List<Agent> teamEnemy = new List<Agent>();
    public List<Flag> teamPlayerFlags = new List<Flag>();
    public List<Flag> teamEnemyFlags = new List<Flag>();

    [Header("Prisons")]
    public PrisonZone teamPlayerSidePrisonZone;
    public PrisonZone teamEnemySidePrisonZone;

    [Header("Territories")]
    public float fieldCentreX = 0.0f; // Dividing line on X axis

    [Header("Game State")]
    public bool gameStarted = false;
    public int winnerTeamID = -1;

    [Header("Score")]
    public int teamPlayerScore = 0;
    public int teamEnemyScore = 0;
    public ScoreZone teamPlayerSideScoreZone;
    public ScoreZone teamEnemySideScoreZone;

    void Awake()
    {
        // Singleton Design
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public List<Flag> GetUncapturedFlags(int enemyTeamID)
    {
        return (enemyTeamID == 0 ? this.teamPlayerFlags : this.teamEnemyFlags)
            .Where(f => f.isCaptured == false).ToList();
    }

    public List<Agent> GetImprisonedAgents(int myTeamID)
    {
        return (myTeamID == 0 ? this.teamPlayer : this.teamEnemy)
            .Where(a => a.isImprisoned == true).ToList();
    }


    // Returns the Team ID's Prison Zone on their Side
    public PrisonZone GetPrisonZone(int teamID)
    {
        return (teamID == 0) ? this.teamPlayerSidePrisonZone : this.teamEnemySidePrisonZone;
    }

    public ScoreZone GetScoreZone(int teamID)
    {
        return (teamID == 0) ? this.teamPlayerSideScoreZone : this.teamEnemySideScoreZone;
    }

    public bool IsInEnemyTerritory(Agent agent)
    {
        // Is the Enemy Half depending on the Agent's TeamID
        bool inEnemyHalf = agent.transform.position.x > fieldCentreX;
        return (agent.teamID == 0) ? !inEnemyHalf : inEnemyHalf;
    }

    public bool CanTag(Agent agent)
    {
        // Agent can Tag only on their own Territory
        bool inOwnTerritory = agent.transform.position.x > this.fieldCentreX;

        // Player is More than CenterX and Enemy Side is Less than CenterX
        return (agent.teamID == 0) ? inOwnTerritory : !inOwnTerritory;
    }

    public void OnFlagScored(int scoringTeamID)
    {
        // Update Score
        if (scoringTeamID == 0) this.teamPlayerScore++;
        else this.teamEnemyScore++;

        // Update Score Text
        UIManager.Instance.UpdateScore(this.teamPlayerScore, this.teamEnemyScore);

        // Check for a Win
        this.CheckWinCondition();
    }

    public void CheckWinCondition()
    {
        // Win Condition based on the amount of Flags Scored
        bool playerWins = this.teamEnemyFlags.All(f => f.isCaptured); // Player Captured all Enemy Flags
        bool enemyWins = this.teamPlayerFlags.All(f => f.isCaptured); // Enemy Captured all Player Flags
        //bool playerWins = this.teamPlayerScore >= 4;
        //bool enemyWins = this.teamEnemyScore >= 4;

        // Ensure one of the Sides won
        if (playerWins == true || enemyWins == true)
        {
            this.TriggerWin((playerWins == true) ? 0 : 1);
            return;
        }

        // Win Condition based on the amount of Agents in Prison
        playerWins = this.teamEnemy.All(a => a.isImprisoned == true); // Player Wins if ALL Enemy Agents are in Prison
        enemyWins = this.teamPlayer.All(a => a.isImprisoned == true); // Enemy Wins if ALL Player Agents are in Prison

        // Ensure one of the Sides won
        if (playerWins == true || enemyWins == true) this.TriggerWin((playerWins == true) ? 0 : 1);
    }

    void TriggerWin(int teamID)
    {
        this.winnerTeamID = teamID;
        this.gameStarted = false;
        UIManager.Instance.ShowWinScreen(teamID);
    }
}
