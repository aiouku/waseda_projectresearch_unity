using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneUIManager : MonoBehaviour
{
    public Button startButton;
    public GameObject levelSelectPanel; // Crane/Laser/Throw の3ボタンをまとめたPanel
    public Button craneButton;
    public Button laserButton;
    public Button throwButton;

    void Start()
    {
        levelSelectPanel.SetActive(false);

        startButton.onClick.AddListener(OnStartClicked);
        craneButton.onClick.AddListener(() => SelectMode(0)); // Crane
        laserButton.onClick.AddListener(() => SelectMode(1)); // LaserPointer
        throwButton.onClick.AddListener(() => SelectMode(2)); // ParabolicThrow
    }

    void OnStartClicked()
    {
        startButton.gameObject.SetActive(false);
        levelSelectPanel.SetActive(true);
    }

    void SelectMode(int mode)
    {
        PlayerPrefs.SetInt("DropMode", mode);
        SceneManager.LoadScene("SampleScene");
    }
}
