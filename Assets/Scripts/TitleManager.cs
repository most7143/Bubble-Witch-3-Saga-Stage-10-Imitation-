using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public Button StartButton;

    private void Start()
    {
        StartButton.onClick.AddListener(OnStartButtonClick);
    }

    public void OnStartButtonClick()
    {
       
        SceneManager.LoadScene("MainScene");
    }
}
