using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class UIBubbleRotation : MonoBehaviour
{
    public bool IsRotating = false;

    [Header("원형 배치 설정")]
    [SerializeField] private float radius = 100f; // 원의 반지름
    [SerializeField] private float rotationDuration = 0.5f; // 회전 애니메이션 시간
    private Vector2 circleCenter;





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


    public void ArrangeBubblesInCircle(List<UIBubble> bubbles, Vector2 circleCenter)
    {
        if (bubbles == null || bubbles.Count == 0)
            return;


        float[] angles = GetBubbleAngles(bubbles.Count);

        for (int i = 0; i < bubbles.Count; i++)
        {
            if (bubbles[i] == null) continue;

            float angle = angles[i];
            bubbles[i].currentAngle = angle;


            float rad = angle * Mathf.Deg2Rad;
            Vector2 position = circleCenter + new Vector2(
                Mathf.Cos(rad) * radius,
                Mathf.Sin(rad) * radius
            );

            bubbles[i].Rect.anchoredPosition = position;
        }
    }



    public void AnimateBubbleRotation(List<UIBubble> bubbles, BubbleRotationTypes rotationType)
    {
        if (bubbles == null || bubbles.Count <= 1 || IsRotating) return;

        IsRotating = true;

        float[] targetAngles = GetBubbleAngles(bubbles.Count);
        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < bubbles.Count; i++)
        {
            UIBubble bubble = bubbles[i];
            if (bubble == null) continue;

            float startAngle = bubble.currentAngle;
            float targetAngle = targetAngles[i];

            float angleDiff = targetAngle - startAngle;

            // -180 ~ 180 범위로 정규화
            while (angleDiff > 180f) angleDiff -= 360f;
            while (angleDiff < -180f) angleDiff += 360f;

            // 항상 반시계 방향으로 회전 (양수 각도)
            if (angleDiff < 0f)
            {
                angleDiff += 360f;
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

        // Shot 타입일 때: 회전이 끝난 후 비활성화된 버블 활성화 및 스케일 애니메이션
        if (rotationType == BubbleRotationTypes.Shot)
        {
            seq.OnComplete(() =>
            {
                Sequence scaleSeq = DOTween.Sequence();

                // 모든 버블에 대해 스케일 애니메이션 적용
                for (int i = 0; i < bubbles.Count; i++)
                {
                    UIBubble bubble = bubbles[i];
                    if (bubble == null) continue;

                    Vector3 targetScale = (i == 0) ? bubble.SelectedScale : bubble.DeselectedScale;
                    
                    if (!bubble.gameObject.activeSelf)
                    {
                        // 비활성화된 버블: 스케일을 0으로 설정하고 활성화
                        bubble.Rect.localScale = Vector3.zero;
                        bubble.gameObject.SetActive(true);
                    }
                    else
                    {
                        // 이미 활성화된 버블: 현재 스케일에서 목표 스케일로 애니메이션
                        // (현재 스케일이 잘못 설정되어 있을 수 있으므로)
                    }
                    
                    // 스케일 애니메이션: 현재 스케일에서 목표 스케일로
                    scaleSeq.Join(bubble.Rect.DOScale(targetScale, rotationDuration)
                        .SetEase(Ease.OutBack));
                }

                scaleSeq.OnComplete(() => IsRotating = false);
            });
        }
        else
        {
            // Rotate 타입일 때는 기존 로직 그대로
            seq.OnComplete(() => IsRotating = false);
        }
    }

    public float[] GetBubbleAngles(int count)
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

}
