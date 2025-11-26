using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public Button StartButton;

    /// <summary>
    /// 게임 해상도 설정
    /// </summary>
    private void Awake()
    {
        Screen.SetResolution(720, 1280, true);
    }

    /// <summary>
    /// 시작 버튼 이벤트 등록
    /// </summary>
    private void Start()
    {
        StartButton.onClick.AddListener(OnStartButtonClick);
    }

    /// <summary>
    /// 시작 버튼 클릭 시 메인 씬으로 이동
    /// </summary>
    public void OnStartButtonClick()
    {
        SceneManager.LoadScene("MainScene");
    }
}
