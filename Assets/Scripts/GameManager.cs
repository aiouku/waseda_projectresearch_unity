using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int score;
    public bool isGameOver;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public Text scoreText;
    public Button backToTitleButton;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (backToTitleButton != null)
            backToTitleButton.onClick.AddListener(BackToTitle);
    }

    public void AddScore(int s)
    {
        if (isGameOver) return;
        score += s;
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        Time.timeScale = 0f;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (scoreText != null) scoreText.text = $"スコア: {score}";
    }

    void BackToTitle()
    {
        Time.timeScale = 1f; // 停止したタイムスケールをリセット
        SceneManager.LoadScene("Title");
    }
}
