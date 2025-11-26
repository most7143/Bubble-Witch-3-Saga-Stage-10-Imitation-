using UnityEngine;
using TMPro;
using System.Collections;

public class UIFloaty : MonoBehaviour
{
    public TextMeshProUGUI ScoreText;

    /// <summary>
    /// 플로팅 점수 텍스트 표시
    /// </summary>
    public void Spawn(int score)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        ScoreText.text = "+ " + score.ToString();
        StartCoroutine(DespawnCoroutine());
    }

    /// <summary>
    /// 플로팅 점수 텍스트 자동 제거 코루틴
    /// </summary>
    private IEnumerator DespawnCoroutine()
    {
        yield return new WaitForSeconds(1f);
        ObjectPool.Instance.DespawnUIFloaty(this);
    }
}
