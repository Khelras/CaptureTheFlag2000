using UnityEngine;

public enum AIGoal
{
    CaptureFlag,
    FreeTeammate
}

[System.Serializable]
public class Goal
{
    public AIGoal type;
    public float insistence;
}

public class GOBDecisionMaking : MonoBehaviour
{
    private Agent self;

    [Header("Goals")]
    public Goal captureGoal = new Goal { type = AIGoal.CaptureFlag, insistence = 5f };
    public Goal freeGoal = new Goal { type = AIGoal.FreeTeammate, insistence = 0f };

    // Insistence Change Rates over Time
    [Header("Insistence Rates (per Second)")]
    public float captureInsistenceRate = 0.5f;

    private void Awake()
    {
        this.self = GetComponent<Agent>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public AIGoal EvaluateBestGoal()
    {
        int enemyTeamID = this.self.teamID == 0 ? 1 : 0;

        // If Agent is already Carrying a Flag then simply go to Score
        if (this.self.carriedFlag != null) return AIGoal.CaptureFlag;

        // Update insistences based on world state
        this.UpdateInsistences(enemyTeamID);

        // Calculate the Discontentment for each Action
        float discontentCapture = this.CalculateDiscontentment(AIGoal.CaptureFlag);
        float discontentFree = this.CalculateDiscontentment(AIGoal.FreeTeammate);

        // Pick the Action that will Result in LOWEST Discontentment
        return (discontentCapture <= discontentFree) ? AIGoal.CaptureFlag : AIGoal.FreeTeammate;
    }

    void UpdateInsistences(int enemyTeamID)
    {
        // 'Capturing Flags' Insistence rises over time (prioritise wanting to Capture Flags)
        int remainingFlags = GameManager.Instance.GetUncapturedFlags(enemyTeamID).Count;
        float captureInsistenceIncrease = this.captureGoal.insistence + this.captureInsistenceRate * Time.deltaTime;
        this.captureGoal.insistence = (remainingFlags > 0) ? Mathf.Clamp(captureInsistenceIncrease, 1f, 5f) : 0f;

        // 'Freeing Teammates' Insistence Spikes when Teammates are in Prison
        int imprisoned = GameManager.Instance.GetImprisonedAgents(this.self.teamID).Count;
        freeGoal.insistence = (imprisoned > 0) ? 3f + imprisoned : 0f;
    }

    float CalculateDiscontentment(AIGoal action)
    {
        // Simulate the Effect of each Action on the Goals
        float captureAfter = this.captureGoal.insistence;
        float freeAfter = this.freeGoal.insistence;
        if (action == AIGoal.CaptureFlag) captureAfter = Mathf.Max(0, captureAfter - 2f); // Capturing a Flag
        else freeAfter = Mathf.Max(0, freeAfter - 3f); // Freeing a Teammate

        // Discontentment is the Sum of Squared Insistences after an Action
        return (captureAfter * captureAfter) + (freeAfter * freeAfter);
    }
}
