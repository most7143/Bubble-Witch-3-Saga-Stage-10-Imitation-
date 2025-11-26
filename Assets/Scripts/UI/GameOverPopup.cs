using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverPopup : MonoBehaviour
{
    public Button TitleButton;

    public TextMeshProUGUI ClearText;


    public TextMeshProUGUI ScoreText;


    /// <summary>
    /// 타이틀 버튼 이벤트 등록
    /// </summary>
    private void Start()
    {
        TitleButton.onClick.AddListener(OnTitleButtonClick);
    }

    /// <summary>
    /// 타이틀 버튼 클릭 시 타이틀 씬으로 이동
    /// </summary>
    private void OnTitleButtonClick()
    {
        SceneManager.LoadScene("TitleScene");
    }

    /// <summary>
    /// 게임 클리어 UI 표시
    /// </summary>
    public void ShowClear()
    {
        ClearText.text = "CLEAR";
        SetScore();
    }

    /// <summary>
    /// 게임 실패 UI 표시
    /// </summary>
    public void ShowFail()
    {
        ClearText.text = "FAIL";
        SetScore();
    }

    /// <summary>
    /// 점수 텍스트 업데이트
    /// </summary>
    public void SetScore()
    {
        ScoreText.text = "Score: " + ScoreSystem.Instance.Score.ToString();
    }
 
}
