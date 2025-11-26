using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }
    
    public GameObject Bubble;
    public GameObject Fairy;
    public GameObject UIFloaty;
    public int PoolSize = 10;

    public List<Bubble> BubbleList = new List<Bubble>();

    public List<Fairy> FairyList = new List<Fairy>();

    public List<UIFloaty> UIFloatyList = new List<UIFloaty>();

    // Canvas 관련 코드 제거 (ScoreSystem으로 이동)
    // [SerializeField] private Canvas uiCanvas;
    // private Canvas uiCanvas;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


  

  private void Start()
  {
    for(int i = 0; i < PoolSize; i++)
    {
       Fairy fairy = AddedFairy();
       fairy.gameObject.SetActive(false);

       UIFloaty uiFloaty = AddedUIFloaty();
       uiFloaty.gameObject.SetActive(false);
    }

    // Canvas 찾기 코드 제거 (ScoreSystem으로 이동)
    // if (UIFloatyList.Count > 0 && UIFloatyList[0] != null)
    // {
    //     uiCanvas = UIFloatyList[0].GetComponentInParent<Canvas>();
    // }
  }

    public Bubble SpawnBubble(BubbleTypes type)
    {
        foreach (Bubble bubble in BubbleList)
        {
            if (!bubble.gameObject.activeSelf)
            {
                bubble.SetBubble(type, false); // 스포너에서 생성된 버블은 isShot=false
                bubble.gameObject.SetActive(true);
                return bubble;
            }
        }



        return AddedBubble(type);

    }

    public UIFloaty AddedUIFloaty()
    {
        GameObject uiFloatyObj = Instantiate(UIFloaty, transform);
        UIFloatyList.Add(uiFloatyObj.GetComponent<UIFloaty>());
        return uiFloatyObj.GetComponent<UIFloaty>();
    }

    public Bubble AddedBubble(BubbleTypes type)
    {
        GameObject bubbleObj = Instantiate(Bubble, transform);
        bubbleObj.GetComponent<Bubble>().SetBubble(type, false); // 스포너에서 생성된 버블은 isShot=false
        BubbleList.Add(bubbleObj.GetComponent<Bubble>());

        return bubbleObj.GetComponent<Bubble>();
    }

    public Fairy SpawnFairy()
    {
        // 리스트를 역순으로 순회하면서 null이거나 파괴된 객체 제거
        for (int i = FairyList.Count - 1; i >= 0; i--)
        {
            if (FairyList[i] == null)
            {
                FairyList.RemoveAt(i);
                continue;
            }
            
            if (!FairyList[i].gameObject.activeSelf)
            {
                FairyList[i].gameObject.SetActive(true);
                return FairyList[i];
            }
        }

        return AddedFairy();
    }

    public Fairy AddedFairy()
    {
        GameObject fairyObj = Instantiate(Fairy, transform);
        FairyList.Add(fairyObj.GetComponent<Fairy>());
        return fairyObj.GetComponent<Fairy>();
    }
    

    public UIFloaty SpawnUIFloaty(int score, Vector2 uiPosition, Canvas canvas)
    {
        foreach (UIFloaty uifloaty in UIFloatyList)
        {
            if (!uifloaty.gameObject.activeSelf)
            {
                // 먼저 활성화
                uifloaty.gameObject.SetActive(true);
                
                // Canvas를 부모로 설정
                if (canvas != null)
                {
                    uifloaty.transform.SetParent(canvas.transform, false);
                }
                
                // 변환된 UI 좌표를 사용하여 위치 설정
                RectTransform rectTransform = uifloaty.transform as RectTransform;
                if (rectTransform != null)
                {
                    rectTransform.localPosition = uiPosition;
                }
                else
                {
                    uifloaty.transform.position = uiPosition;
                }
                
                // 활성화 후 Spawn 호출
                uifloaty.Spawn(score);
                return uifloaty;
            }
        }

        UIFloaty newUIFloaty = AddedUIFloaty();
        if (newUIFloaty != null && canvas != null)
        {
            // 새로 생성된 UIFloaty도 활성화
            newUIFloaty.gameObject.SetActive(true);
            newUIFloaty.transform.SetParent(canvas.transform, false);
            RectTransform rectTransform = newUIFloaty.transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.localPosition = uiPosition;
            }
            // 활성화 후 Spawn 호출
            newUIFloaty.Spawn(score);
        }
        return newUIFloaty;
    }

    public void DespawnUIFloaty(UIFloaty uifloaty)
    {
        if (uifloaty == null) return;
        uifloaty.gameObject.SetActive(false);
    }
    
    

    public void DespawnFairy(Fairy fairy)
    {
        if (fairy == null) return;
        
        // 객체가 파괴되지 않았을 때만 처리
        if (fairy.gameObject != null)
        {
            fairy.gameObject.SetActive(false);
        }
        
        // 리스트에서 안전하게 제거
        FairyList.RemoveAll(f => f == null || f == fairy);
    }

    public void DespawnBubble(Bubble bubble)
    {
        if (bubble == null) return;

        // 애니메이션 재생
        bubble.DestroyBubble();

        // 애니메이션이 끝날 때까지 기다린 후 비활성화
        StartCoroutine(DespawnAfterAnimation(bubble));
    }

    private IEnumerator DespawnAfterAnimation(Bubble bubble)
    {
        if (bubble != null && bubble.Anim != null)
        {
            yield return new WaitForSeconds(0.2f);
            bubble.gameObject.SetActive(false);
        }


    }

}
