using UnityEngine;
using TMPro;
using System.Collections;

public class UIFloaty : MonoBehaviour
{
    public TextMeshProUGUI ScoreText;
    
    public void Spawn(int score)
    {
        // 게임 오브젝트가 활성화되어 있는지 확인
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        ScoreText.text = "+ " + score.ToString();
        StartCoroutine(DespawnCoroutine());
    }

    private IEnumerator DespawnCoroutine()
    {
        yield return new WaitForSeconds(1f);
        ObjectPool.Instance.DespawnUIFloaty(this);
    }
}
