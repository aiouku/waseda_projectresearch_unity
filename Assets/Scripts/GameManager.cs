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
        // unityroomはスコアボードを最大2つまでしか作れないため、3モード分を1ボードに統合する。
        // モードは小数部に埋め込む: +0.1=Crane, +0.2=LaserPointer, +0.3=ParabolicThrow
        // (scoreは常に整数のため、小数部と衝突しない。ボード側の書式を「小数(第1位)」にしておくこと)
        int dropMode = PlayerPrefs.GetInt("DropMode", 0);
        float modeTag = (dropMode + 1) * 0.1f;
        float encodedScore = Mathf.Round((score + modeTag) * 10f) / 10f;

        UnityroomApiClient.Instance.SendScore(1, encodedScore, ScoreboardWriteMode.HighScoreDesc);
#endif
    }

    void BackToTitle()
    {
        Time.timeScale = 1f; // 停止したタイムスケールをリセット
        SceneManager.LoadScene("Title");
    }
}
