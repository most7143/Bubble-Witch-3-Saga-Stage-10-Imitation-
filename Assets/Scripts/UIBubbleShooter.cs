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

    public TextMeshProUGUI ShottingCount;

    public List<UIBubble> Bubbles;

    public UIBubble CurrentBubble;

    [Header("원형 배치 설정")]
    [SerializeField] private float radius = 100f; // 원의 반지름
    [SerializeField] private float rotationDuration = 0.5f; // 회전 애니메이션 시간

    private Vector2 circleCenter;
    private bool isRotating = false;




    [System.Serializable]
    public class BubbleAngleConfig
    {
        public int bubbleCount;
        public float[] angles;
    }


    [Header("버블 각도 설정 (에디터에서 조정 가능)")]
    [Tooltip("버블 개수별 각도 배열. 인덱스 0 = 1개 버블, 인덱스 1 = 2개 버블, ...")]
    [SerializeField]
    private BubbleAngleConfig[] angleConfigs = new BubbleAngleConfig[]
    {
        new BubbleAngleConfig { bubbleCount = 1, angles = new float[] { 90f } },      // 12시
        new BubbleAngleConfig { bubbleCount = 2, angles = new float[] { 90f, -50f } }, // 12시, 5시
        new BubbleAngleConfig { bubbleCount = 3, angles = new float[] { 90f, -50f, -140f } } // 12시, 5시, 7시
    };

    void Start()
    {
        ShooterButton.onClick.AddListener(ClickShooter);



        circleCenter = Rect.anchoredPosition;

        if (Bubbles != null && Bubbles.Count > 0)
        {
            ArrangeBubblesInCircle();

            SelectBubble(Bubbles[0]);

            for (int i = 1; i < Bubbles.Count; i++)
            {
                DeselectBubble(Bubbles[i]);
            }
        }
    }

    private void ArrangeBubblesInCircle()
    {
        if (Bubbles == null || Bubbles.Count == 0)
            return;


        float[] angles = GetBubbleAngles(Bubbles.Count);

        for (int i = 0; i < Bubbles.Count; i++)
        {
            if (Bubbles[i] == null) continue;

            float angle = angles[i];
            Bubbles[i].currentAngle = angle;


            float rad = angle * Mathf.Deg2Rad;
            Vector2 position = circleCenter + new Vector2(
                Mathf.Cos(rad) * radius,
                Mathf.Sin(rad) * radius
            );

            Bubbles[i].Rect.anchoredPosition = position;
        }
    }

    public void ClickShooter()
    {
        UpdateSelectedBubble();  // 선택 갱신
        AnimateBubbleRotation(); // 애니메이션
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

    public void AnimateBubbleRotation()
    {
        if (Bubbles == null || Bubbles.Count <= 1 || isRotating) return;

        isRotating = true;

        float[] targetAngles = GetBubbleAngles(Bubbles.Count);
        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < Bubbles.Count; i++)
        {
            UIBubble bubble = Bubbles[i];
            if (bubble == null) continue;

            float startAngle = bubble.currentAngle;
            float targetAngle = targetAngles[i];

            float angleDiff = targetAngle - startAngle;

            while (angleDiff > 180f) angleDiff -= 360f;
            while (angleDiff < -180f) angleDiff += 360f;

            if (angleDiff > 0f)
            {
                angleDiff -= 360f;
            }


            seq.Join(DOTween.To(() => bubble.currentAngle,
                currentMoveAngle =>
                {
                    bubble.currentAngle = currentMoveAngle;
                    float rad = currentMoveAngle * Mathf.Deg2Rad;
                    Vector2 pos = circleCenter + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

                    bubble.Rect.anchoredPosition = pos;
                },
                startAngle + angleDiff,
                rotationDuration
            ).SetEase(Ease.OutCubic));
        }

        seq.OnComplete(() => isRotating = false);
    }

    private float[] GetBubbleAngles(int count)
    {
        // 에디터에서 설정한 각도 설정 찾기
        if (angleConfigs != null)
        {
            foreach (var config in angleConfigs)
            {
                if (config.bubbleCount == count && config.angles != null && config.angles.Length == count)
                {
                    return config.angles;
                }
            }
        }

        return null;
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
