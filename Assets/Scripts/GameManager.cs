using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int score;
    public bool isGameOver;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddScore(int s)
    {
        if (isGameOver) return;
        score += s;
    }

    public void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        Debug.Log("Game Over! Score: " + score);
    }
}