using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    public GameObject Bubble;
    public int PoolSize = 10;

    public List<Bubble> BubbleList = new List<Bubble>();

  

    public Bubble SpawnBubble(BubbleTypes type)
    {
        foreach (Bubble bubble in BubbleList)
        {
            if (!bubble.gameObject.activeSelf)
            {
                bubble.SetBubbleType(type);
                bubble.gameObject.SetActive(true);
                return bubble;
            }
        }



        return AddedBubble(type);

    }


    public Bubble AddedBubble(BubbleTypes type)
    {
        GameObject bubbleObj = Instantiate(Bubble, transform);
        bubbleObj.GetComponent<Bubble>().SetBubbleType(type);
        BubbleList.Add(bubbleObj.GetComponent<Bubble>());

        return bubbleObj.GetComponent<Bubble>();
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
