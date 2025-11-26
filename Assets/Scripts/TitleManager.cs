using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public Button StartButton;

    private void Awake()
    {
        // 게임 시작 시 720x1280 해상도로 전체화면 고정
        Screen.SetResolution(720, 1280, true);
    }

    private void Start()
    {
        StartButton.onClick.AddListener(OnStartButtonClick);
    }

    public void OnStartButtonClick()
    {
       
        SceneManager.LoadScene("MainScene");
    }
}
