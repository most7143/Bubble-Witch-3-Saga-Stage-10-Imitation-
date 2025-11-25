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
                    
                    // 스케일 애니메이션: 현재 스케일에서 목표 스케일로
                    scaleSeq.Join(bubble.Rect.DOScale(targetScale, rotationDuration)
                        .SetEase(Ease.OutBack));
                }

                scaleSeq.OnComplete(() => IsRotating = false);
            });
        }
        else
        {
            // Rotate 타입일 때: 회전이 끝난 후 스케일 애니메이션 추가
            seq.OnComplete(() =>
            {
                Sequence scaleSeq = DOTween.Sequence();

                // 모든 버블에 대해 스케일 애니메이션 적용
                for (int i = 0; i < bubbles.Count; i++)
                {
                    UIBubble bubble = bubbles[i];
                    if (bubble == null) continue;

                    Vector3 currentScale = bubble.Rect.localScale;
                    Vector3 targetScale = (i == 0) ? bubble.SelectedScale : bubble.DeselectedScale;
                    
                    // 스케일 애니메이션: 현재 스케일에서 목표 스케일로
                    scaleSeq.Join(bubble.Rect.DOScale(targetScale, rotationDuration)
                        .SetEase(Ease.OutBack));
                }

                scaleSeq.OnComplete(() => IsRotating = false);
            });
        }
    }

    /// <summary>
    /// 재장전 애니메이션: 1번째 버블이 반시계로 0번째 위치로 이동, 그 후 스케일이 커지면서 나타남
    /// </summary>
    public void AnimateBubbleReload(List<UIBubble> bubbles, Vector2 circleCenter)
    {
        if (bubbles == null || bubbles.Count < 2 || IsRotating) return;

        IsRotating = true;
        this.circleCenter = circleCenter;

        float[] targetAngles = GetBubbleAngles(bubbles.Count);
        
        // 0번째 버블(장전될 버블)이 현재 위치에서 0번째 위치로 이동
        // 실제로는 이미 리스트에서 0번째로 이동했으므로, 현재 각도에서 0번째 위치 각도로 이동
        if (bubbles[0] != null)
        {
            UIBubble reloadBubble = bubbles[0];
            float startAngle = reloadBubble.currentAngle;
            float targetAngle = targetAngles[0]; // 0번째 위치의 각도

            float angleDiff = targetAngle - startAngle;

            // -180 ~ 180 범위로 정규화
            while (angleDiff > 180f) angleDiff -= 360f;
            while (angleDiff < -180f) angleDiff += 360f;

            // 반시계 방향으로 회전 (음수면 360도 더하기)
            if (angleDiff < 0f)
            {
                angleDiff += 360f;
            }

            Sequence reloadSeq = DOTween.Sequence();

            // 0번째 버블이 0번째 위치로 반시계 방향 이동
            reloadSeq.Append(DOTween.To(() => reloadBubble.currentAngle,
                currentMoveAngle =>
                {
                    reloadBubble.currentAngle = currentMoveAngle;
                    float rad = currentMoveAngle * Mathf.Deg2Rad;
                    Vector2 pos = circleCenter + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                    reloadBubble.Rect.anchoredPosition = pos;
                },
                startAngle + angleDiff,
                rotationDuration
            ).SetEase(Ease.OutCubic));

            // 이동 완료 후 스케일이 커지면서 나타남
            reloadSeq.OnComplete(() =>
            {
                if (reloadBubble != null)
                {
                    // 스케일을 0에서 시작해서 커지게
                    reloadBubble.Rect.localScale = Vector3.zero;
                    reloadBubble.gameObject.SetActive(true);
                    
                    // 스케일 애니메이션
                    reloadBubble.Rect.DOScale(reloadBubble.SelectedScale, rotationDuration)
                        .SetEase(Ease.OutBack)
                        .OnComplete(() => IsRotating = false);
                }
                else
                {
                    IsRotating = false;
                }
            });
        }
        else
        {
            IsRotating = false;
        }
    }

    /// <summary>
    /// 발사 후 재장전 애니메이션: 0번째 버블이 반시계로 이동, 1번째 버블은 스케일로 생성
    /// </summary>
    public void AnimateReloadAfterShot(List<UIBubble> bubbles, Vector2 circleCenter)
    {
        if (bubbles == null || bubbles.Count < 2 || IsRotating) return;

        IsRotating = true;
        this.circleCenter = circleCenter;

        float[] targetAngles = GetBubbleAngles(bubbles.Count);
        
        // targetAngles가 null이면 에러
        if (targetAngles == null || targetAngles.Length == 0)
        {
            Debug.LogError("targetAngles is null or empty!");
            IsRotating = false;
            return;
        }
        
        Sequence reloadSeq = DOTween.Sequence();
        
        // 0번째 버블(원래 1번째)이 0번째 위치로 반시계 방향 이동
        if (bubbles[0] != null)
        {
            UIBubble firstBubble = bubbles[0];
            
            // currentAngle이 초기화되지 않았을 수 있으므로 체크
            if (!firstBubble.gameObject.activeSelf)
            {
                firstBubble.gameObject.SetActive(true);
            }
            
            // 각도가 설정되지 않았으면 현재 위치에서 계산
            float startAngle = firstBubble.currentAngle;
            if (startAngle == 0f)
            {
                // 현재 위치에서 각도 계산
                Vector2 currentPos = firstBubble.Rect.anchoredPosition;
                Vector2 dir = currentPos - circleCenter;
                startAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                firstBubble.currentAngle = startAngle;
            }
            
            float targetAngle = targetAngles[0];

            float angleDiff = targetAngle - startAngle;

            // -180 ~ 180 범위로 정규화
            while (angleDiff > 180f) angleDiff -= 360f;
            while (angleDiff < -180f) angleDiff += 360f;

            // 반시계 방향으로 회전
            if (angleDiff < 0f)
            {
                angleDiff += 360f;
            }

            // 0번째 버블 이동
            reloadSeq.Append(DOTween.To(() => firstBubble.currentAngle,
                currentMoveAngle =>
                {
                    firstBubble.currentAngle = currentMoveAngle;
                    float rad = currentMoveAngle * Mathf.Deg2Rad;
                    Vector2 pos = circleCenter + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                    firstBubble.Rect.anchoredPosition = pos;
                },
                startAngle + angleDiff,
                rotationDuration
            ).SetEase(Ease.OutCubic));
        }
        else
        {
            Debug.LogError("bubbles[0] is null!");
            IsRotating = false;
            return;
        }

        // 이동 완료 후 1번째 버블(새로운 버블) 스케일 애니메이션
        reloadSeq.OnComplete(() =>
        {
            if (bubbles.Count >= 2 && bubbles[1] != null)
            {
                UIBubble newBubble = bubbles[1];
                
                // 1번째 위치에 배치
                float[] angles = GetBubbleAngles(bubbles.Count);
                if (angles != null && angles.Length > 1)
                {
                    float angle = angles[1];
                    newBubble.currentAngle = angle;
                    float rad = angle * Mathf.Deg2Rad;
                    Vector2 pos = circleCenter + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                    newBubble.Rect.anchoredPosition = pos;
                }
                
                // 스케일 0에서 시작해서 커지게
                newBubble.Rect.localScale = Vector3.zero;
                newBubble.gameObject.SetActive(true);
                
                // 스케일 애니메이션
                newBubble.Rect.DOScale(newBubble.DeselectedScale, rotationDuration)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => IsRotating = false);
            }
            else
            {
                IsRotating = false;
            }
            
            // 2번째 이후 버블은 비활성화 상태로 유지
            for (int i = 2; i < bubbles.Count; i++)
            {
                if (bubbles[i] != null)
                {
                    bubbles[i].gameObject.SetActive(false);
                }
            }
        });
    }

    /// <summary>
    /// Nero 버블 스폰 애니메이션: 0번째와 2번째 버블이 제자리에서 스케일로 생성
    /// </summary>
    public void AnimateNeroBubbleSpawn(List<UIBubble> bubbles, Vector2 circleCenter)
    {
        if (bubbles == null || bubbles.Count < 3 || IsRotating) return;

        IsRotating = true;
        this.circleCenter = circleCenter;

        float[] targetAngles = GetBubbleAngles(bubbles.Count);
        
        // 각도 설정이 없으면 동적으로 계산
        if (targetAngles == null || targetAngles.Length < bubbles.Count)
        {
            targetAngles = CalculateDefaultAngles(bubbles.Count);
        }
        
        if (targetAngles == null || targetAngles.Length < 3)
        {
            Debug.LogError($"targetAngles is null or insufficient! Count: {bubbles.Count}, Angles length: {(targetAngles?.Length ?? 0)}");
            IsRotating = false;
            return;
        }

        // 모든 버블의 위치를 먼저 설정
        for (int i = 0; i < bubbles.Count; i++)
        {
            if (bubbles[i] != null)
            {
                float angle = targetAngles[i];
                bubbles[i].currentAngle = angle;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 pos = circleCenter + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                bubbles[i].Rect.anchoredPosition = pos;
            }
        }

        Sequence spawnSeq = DOTween.Sequence();

        // 0번째 버블(Nero) 스케일 애니메이션
        if (bubbles[0] != null)
        {
            UIBubble neroBubble = bubbles[0];
            
            // 스케일 0에서 시작해서 커지게
            neroBubble.Rect.localScale = Vector3.zero;
            neroBubble.gameObject.SetActive(true);
            
            spawnSeq.Join(neroBubble.Rect.DOScale(neroBubble.SelectedScale, rotationDuration)
                .SetEase(Ease.OutBack));
        }

        // 1번째 버블은 이미 있으므로 위치만 재설정 (스케일 애니메이션 없음)
        if (bubbles.Count > 1 && bubbles[1] != null)
        {
            // 위치는 이미 위에서 설정됨
            bubbles[1].gameObject.SetActive(true);
        }

        // 2번째 버블(새 버블) 스케일 애니메이션
        if (bubbles.Count > 2 && bubbles[2] != null)
        {
            UIBubble newBubble = bubbles[2];
            
            // 스케일 0에서 시작해서 커지게
            newBubble.Rect.localScale = Vector3.zero;
            newBubble.gameObject.SetActive(true);
            
            spawnSeq.Join(newBubble.Rect.DOScale(newBubble.DeselectedScale, rotationDuration)
                .SetEase(Ease.OutBack));
        }

        spawnSeq.OnComplete(() => IsRotating = false);
    }

    /// <summary>
    /// 기본 각도 계산 (각도 설정이 없을 때 사용)
    /// </summary>
    private float[] CalculateDefaultAngles(int count)
    {
        float[] angles = new float[count];
        
        // 3개 버블의 경우: 12시, 5시, 7시
        if (count == 3)
        {
            angles[0] = 90f;   // 12시
            angles[1] = -50f;  // 5시
            angles[2] = -140f; // 7시
        }
        else if (count == 2)
        {
            angles[0] = 90f;   // 12시
            angles[1] = -50f;  // 5시
        }
        else if (count == 1)
        {
            angles[0] = 90f;   // 12시
        }
        else
        {
            // 4개 이상일 때는 균등 분배
            float angleStep = 360f / count;
            for (int i = 0; i < count; i++)
            {
                angles[i] = 90f - (angleStep * i); // 12시부터 시계방향
            }
        }
        
        return angles;
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
