using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    public GameObject Bubble;
    public int PoolSize = 10;

    public List<Bubble> BubbleList = new List<Bubble>();

    void Start()
    {
        
    }

    public void InitializePool()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            GameObject bubbleObj = Instantiate(Bubble, transform);
            BubbleList.Add(bubbleObj.GetComponent<Bubble>());
            bubbleObj.SetActive(false);
        }
    }

    
    public Bubble SpawnBubble()
    {
        foreach (Bubble bubble in BubbleList)
        {
            if (!bubble.gameObject.activeSelf)
            {
                bubble.gameObject.SetActive(true);
                return bubble;
            }
        }



        return AddedBubble();

    }


    public Bubble AddedBubble()
    {
        GameObject bubbleObj = Instantiate(Bubble, transform);
        BubbleList.Add(bubbleObj.GetComponent<Bubble>());

        return bubbleObj.GetComponent<Bubble>();
    }

    public void DespawnBubble(Bubble bubble)
    {
        bubble.gameObject.SetActive(false);
    }
}
