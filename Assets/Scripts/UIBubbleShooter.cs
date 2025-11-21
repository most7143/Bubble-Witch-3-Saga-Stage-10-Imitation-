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


   
    public void ClickShooter()
    {
        if(BubbleRotation.IsRotating) return;

        UpdateSelectedBubble();  // 선택 갱신

        BubbleRotation.AnimateBubbleRotation(Bubbles); // 애니메이션
    }

    
    private void InitializeBubbles()
    {
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




    public void UpdateSelectedBubble()
    {
        if (Bubbles == null || Bubbles.Count == 0)
            return;


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
        bubble.Rect.localScale = Vector3.one;
    }

    private void DeselectBubble(UIBubble bubble)
    {
        bubble.Rect.localScale = Vector3.one * 0.8f;
    }
}
