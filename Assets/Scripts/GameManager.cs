using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_WEBGL
using unityroom.Api;
#endif

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
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            BackToTitle();
        }
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

        SendScoreToUnityroom();
    }

    void SendScoreToUnityroom()
    {
#if UNITY_WEBGL
        // モードに応じてボード番号を決定
        // ボード1: Crane, ボード2: LaserPointer, ボード3: ParabolicThrow
        int dropMode = PlayerPrefs.GetInt("DropMode", 0);
        int boardNo = dropMode + 1; // 1, 2, 3

        UnityroomApiClient.Instance.SendScore(boardNo, score, ScoreboardWriteMode.HighScoreDesc);
#endif
    }

    void BackToTitle()
    {
        Time.timeScale = 1f; // 停止したタイムスケールをリセット
        SceneManager.LoadScene("Title");
    }
}
