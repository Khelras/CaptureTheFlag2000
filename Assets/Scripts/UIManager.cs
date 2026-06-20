using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Panels")]
    public GameObject winPanel;

    [Header("Text")]
    public TMP_Text scoreText;
    public TMP_Text winText;

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

    public void UpdateScore(int playerScore, int enemyScore)
    {
        if (this.scoreText != null) this.scoreText.text = $"Enemy: {enemyScore} | Player: {playerScore}";
    }

    public void ShowWinScreen(int teamID)
    {
        // Show the Winning Team
        this.winPanel.SetActive(true);
        this.winText.text = $"Team {(teamID == 0 ? "Player" : "Enemy")} Wins!";
    }

    public void OnRestartClicked()
    {
        // Restart the Game
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
