using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverPopup : MonoBehaviour
{
    public Button TitleButton;

    public TextMeshProUGUI ClearText;


    public TextMeshProUGUI ScoreText;


    private void Start()
    {
        TitleButton.onClick.AddListener(OnTitleButtonClick);
    }

    private void OnTitleButtonClick()
    {
        SceneManager.LoadScene("TitleScene");
    }

    public void ShowClear()
    {
        ClearText.text = "CLEAR";
        SetScore();
    }

    public void ShowFail()
    {
        ClearText.text = "FAIL";
        SetScore();
    }
    public void SetScore()
    {
        ScoreText.text = "Score: " + ScoreSystem.Instance.Score.ToString();
    }
 
}
