using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class UIBubbleShooter : MonoBehaviour
{

    [Header("컴포넌트")]
    public RectTransform Rect;
    public Button ShooterButton;

    public UIBubbleRotation BubbleRotation;

    public TextMeshProUGUI ShottingCount;

    public List<UIBubble> Bubbles;

    public UIBubble CurrentBubble;

    public Vector2 CircleCenter;

    void Start()
    {
        ShooterButton.onClick.AddListener(ClickShooter);

        CircleCenter = Rect.anchoredPosition;

        InitializeBubbles();
    }
     private void InitializeBubbles()
    {
        BubbleTypes[] randomTypes = { BubbleTypes.Red, BubbleTypes.Blue, BubbleTypes.Yellow };

        // Bubbles 리스트 초기화
        if (Bubbles == null)
            Bubbles = new List<UIBubble>();

        for (int i = 0; i < 2; i++)
        {
            GameObject bubblePrefab = Resources.Load<GameObject>("UIBubble");

            GameObject bubbleObj = Instantiate(bubblePrefab, Rect.parent);

            UIBubble bubble = bubbleObj.GetComponent<UIBubble>();

            if (bubble != null)
            {
                bubble.SetType(randomTypes[Random.Range(0, randomTypes.Length)]);
                Bubbles.Add(bubble);
            }

        }

        if (Bubbles != null && Bubbles.Count > 0)
        {
            BubbleRotation.ArrangeBubblesInCircle(Bubbles, CircleCenter);

            SelectBubble(Bubbles[0]);

            for (int i = 1; i < Bubbles.Count; i++)
            {
                DeselectBubble(Bubbles[i]);
            }
        }
    }



    public void ClickShooter()
    {
        if (BubbleRotation.IsRotating) return;

        UpdateSelectedBubble();  // 선택 갱신

        BubbleRotation.AnimateBubbleRotation(Bubbles, BubbleRotationTypes.Rotate); // 애니메이션
    }

    /// <summary>
    /// 발사 후 새로운 버블 준비 (타입 설정 및 즉시 표시)
    /// </summary>
    public void PrepareNewBubbleAfterShot()
    {
        if (Bubbles == null || Bubbles.Count == 0)
            return;

        // 버블 순환
        UIBubble first = Bubbles[0];
        Bubbles.RemoveAt(0);
        Bubbles.Add(first);


        BubbleTypes[] randomTypes = { BubbleTypes.Red, BubbleTypes.Blue, BubbleTypes.Yellow };
        BubbleTypes newType = randomTypes[Random.Range(0, randomTypes.Length)];
        first.SetType(newType);

        // 새로운 버블을 선택하고 바로 보이게 함
        SelectBubble(Bubbles[0]);
        CurrentBubble?.gameObject.SetActive(true);

        // 나머지 버블 비선택
        for (int i = 1; i < Bubbles.Count; i++)
            DeselectBubble(Bubbles[i]);
    }

   




    public void UpdateSelectedBubble()
    {
        if (Bubbles == null || Bubbles.Count == 0)
            return;

        // 버블 순환만 수행 (타입 변경 없음)
        UIBubble first = Bubbles[0];
        Bubbles.RemoveAt(0);
        Bubbles.Add(first);

        SelectBubble(Bubbles[0]);

        for (int i = 1; i < Bubbles.Count; i++)
            DeselectBubble(Bubbles[i]);
    }


    private void SelectBubble(UIBubble bubble)
    {
        CurrentBubble = bubble;
        bubble.Rect.localScale = bubble.SelectedScale;
    }

    private void DeselectBubble(UIBubble bubble)
    {
        bubble.Rect.localScale = bubble.DeselectedScale;
    }
}
