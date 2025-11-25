using UnityEngine;
using UnityEngine.UI;

public class Nero : MonoBehaviour
{
    private int MaxFillCount=4;
    private int currentFillCount=0;

    public Image FillImage;



    private void Start()
    {
        currentFillCount=0;
    }

    public void AddFillCount()
    {
        currentFillCount++;

        if(currentFillCount>=MaxFillCount)
        {
            currentFillCount=0;
            SpawnNeroBubble();
        }

        UpdateFillImage();
    }

    private void UpdateFillImage()
    {
        FillImage.fillAmount=currentFillCount/MaxFillCount;
    }

    private void SpawnNeroBubble()
    {
        ObjectPool.Instance.SpawnBubble(BubbleTypes.Nero);
    }
}
