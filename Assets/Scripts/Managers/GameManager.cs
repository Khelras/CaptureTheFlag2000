using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Spawning")]
    public GameObject playerAgentPrefab;
    public GameObject enemyAgentPrefab;
    public Transform playerAgentParent;
    public Transform enemyAgentParent;
    [HideInInspector] public int chosenTeamSize = 4; // set by UI
    [HideInInspector] public List<Agent> teamPlayer = new List<Agent>();
    [HideInInspector] public List<Agent> teamEnemy = new List<Agent>();

    [Header("Flags")]
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

    public void SpawnAgents()
    {
        // Clear Teams
        this.teamPlayer.Clear();
        this.teamEnemy.Clear();

        // Spawn Evenly Spread Out Horizontally
        float camHeight = Camera.main.orthographicSize * 2f;
        float spacing = camHeight / (this.chosenTeamSize + 1);

        // Agent Spawn Loop
        for (int i = 0; i < this.chosenTeamSize; i++)
        {
            // Spawn Y
            float spawnY = ((i + 1) * spacing) - Camera.main.orthographicSize;

            // -- Player Agent Spawning -- //
            // Instantiate the Game Object
            Vector3 playerPosition = new Vector3(2f, spawnY, 0f);
            GameObject pa = Instantiate(this.playerAgentPrefab, playerPosition, Quaternion.identity);
            pa.transform.SetParent(this.playerAgentParent);

            // Set the Player Agent Properties
            Agent playerAgent = pa.GetComponent<Agent>();
            playerAgent.teamID = 0;
            this.teamPlayer.Add(playerAgent);
            // -- //

            // -- Enemy Agent Spawning -- //
            // Instantiate the Game Object
            Vector3 enemyPosition = new Vector3(-2f, spawnY, 0f);
            GameObject ea = Instantiate(this.enemyAgentPrefab, enemyPosition, Quaternion.identity);
            ea.transform.SetParent(this.enemyAgentParent);

            // Set the Enemy Agent Properties
            Agent enemyAgent = ea.GetComponent<Agent>();
            enemyAgent.teamID = 1;
            this.teamEnemy.Add(enemyAgent);
            // -- //
        }

        // Notify the Player Controller and the Team Managers to Run after Spawn
        FindFirstObjectByType<PlayerController>()?.OnAgentsSpawned();
        FindFirstObjectByType<AITeamManager>()?.OnAgentsSpawned();
        FindFirstObjectByType<PlayerTeamManager>()?.OnAgentsSpawned();
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

        // Feedback Text
        UIManager.Instance.ShowFeedback(scoringTeamID, (scoringTeamID == 0)
            ? "Player Team scored a Flag!"
            : "Enemy Team has scored a Flag!");

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
