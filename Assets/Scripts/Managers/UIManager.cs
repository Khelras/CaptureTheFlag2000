using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text feedbackText;
    private Coroutine feedbackCoroutine;

    [Header("Start Panel")]
    public GameObject startPanel;
    public TMP_Text teamSizeValueText;
    private int chosenTeamSize = 4;
    private const int minTeamSize = 2;
    private const int maxTeamSize = 10;

    [Header("Win Panel")]
    public GameObject winPanel;
    public TMP_Text winText;   

    void Awake()
    {
        // Singleton Design
        Instance = this;

        this.startPanel.SetActive(true); // Show the Start Panel Menu
        this.winPanel.SetActive(false); // Hide the Win Panel Menu
        this.feedbackText.alpha = 0f; // Initially Hide the Feedback Text
        this.UpdateTeamSizeText();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Called by MinusButton OnClick
    public void OnDecreaseTeamSize()
    {
        // Clamped to Minimum Team Size
        this.chosenTeamSize = Mathf.Max(minTeamSize, this.chosenTeamSize - 1);
        this.UpdateTeamSizeText();
    }

    // Called by PlusButton OnClick
    public void OnIncreaseTeamSize()
    {
        // Clamped to Maximum Team Size
        this.chosenTeamSize = Mathf.Min(maxTeamSize, this.chosenTeamSize + 1);
        this.UpdateTeamSizeText();
    }

    void UpdateTeamSizeText()
    {
        teamSizeValueText.text = this.chosenTeamSize.ToString();
    }

    // Called by StartButton OnClick
    public void OnStartClicked()
    {
        // Start the Game
        GameManager.Instance.chosenTeamSize = this.chosenTeamSize;
        GameManager.Instance.SpawnAgents();
        GameManager.Instance.gameStarted = true;

        // Hide the Start Panel
        this.startPanel.SetActive(false);

        //ShowFeedback("Game Started! Capture all enemy flags to win!");
    }

    public void UpdateScore(int playerScore, int enemyScore)
    {
        if (this.scoreText != null) this.scoreText.text = $"Enemy: {enemyScore} | Player: {playerScore}";
    }

    public void ShowFeedback(int teamID, string message)
    {
        // Stop any Previous Feedback Text Fading
        if (this.feedbackCoroutine != null) StopCoroutine(this.feedbackCoroutine);

        // Set the Feedback Text Color
        this.feedbackText.color = (teamID == 0)
            ? new Color(0f, 0.8f, 0f, 0.8f) // Green for Feedback from Player
            : new Color(0.8f, 0f, 0f, 0.8f); // Red for Feedback from Enemy

        // Start a CoRoutine
        this.feedbackCoroutine = StartCoroutine(FeedbackFade(message));
    }

    IEnumerator FeedbackFade(string message)
    {
        // Immediate show the Feedback Text at full Opacity
        this.feedbackText.text = message;
        this.feedbackText.alpha = 1f;

        // Pause here for a set amount of Seconds
        yield return new WaitForSeconds(2.5f);

        // Resumes after the set amount of Seconds and then Fade out the Feedback Text
        float elapsed = 0f;
        float fadeDuration = 1f;

        // Each Iteration of this Loop will be ONE Frame
        while (elapsed < fadeDuration)
        {
            // Fade out the Feedback Text
            elapsed += Time.deltaTime;
            feedbackText.alpha = 1f - (elapsed / fadeDuration);

            // Pause and then go to the next Iteration on the Next Frame
            yield return null;
        }

        // Feedback Text has Faded out
        this.feedbackText.alpha = 0f;
        this.feedbackText.text = "";
    }

    public void ShowWinScreen(int teamID)
    {
        // Show the Winning Team
        this.winPanel.SetActive(true);
        this.winText.text = $"Team {(teamID == 0 ? "Player" : "Enemy")} Wins!";
    }

    // Called by RestartButton OnClick
    public void OnRestartClicked()
    {
        // Restart the Game
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
