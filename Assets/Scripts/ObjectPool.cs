using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }
    
    public GameObject Bubble;
    public GameObject Fairy;
    public int PoolSize = 10;

    public List<Bubble> BubbleList = new List<Bubble>();

    public List<Fairy> FairyList = new List<Fairy>();



  

  private void Start()
  {
    for(int i = 0; i < PoolSize; i++)
    {
       Fairy fairy = AddedFairy();
       fairy.gameObject.SetActive(false);
    }
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
}
