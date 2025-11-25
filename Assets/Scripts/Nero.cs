using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class Nero : MonoBehaviour
{
    private int MaxFillCount = 4;
    private int currentFillCount = 0;

    public Image FillImage;

    public Button AddFillButton;

    [Header("참조")]
    public UIBubbleShooter UIBubbleShooter; // UIBubbleShooter 참조 추가

    private Tween fillTween; // 진행 중인 애니메이션 추적용


    private Coroutine reloadBubbleCoroutine;

    private void Start()
    {
        currentFillCount = 0;
        AddFillButton.onClick.AddListener(ClickAddFillButton);
    }

    public void ClickAddFillButton()
    {
        if (IngameManager.Instance.CurrentState != BattleState.Normal)
            return;

        // 버블 재장전 연출 시작
        if (UIBubbleShooter != null)
        {
            if (reloadBubbleCoroutine == null)
                reloadBubbleCoroutine = StartCoroutine(ReloadBubbleCoroutine());
        }

    }

    /// <summary>
    /// 버블 재장전 코루틴 (ReloadAfterShot 사용)
    /// </summary>
    private IEnumerator ReloadBubbleCoroutine()
    {
        // 0번째 버블 사라지게
        if (UIBubbleShooter.CurrentBubble != null)
        {
            // 버블의 위치와 이미지 가져오기
            UIBubble currentBubble = UIBubbleShooter.CurrentBubble;
            Vector3 bubbleStartPos = currentBubble.Rect.position;
            Sprite bubbleSprite = currentBubble.Image.sprite;

            // 버블을 비활성화 (링크 끊기)
            UIBubbleShooter.UnselectBubble();

            // 버블 이미지를 복제해서 이동 연출
            GameObject bubblePrefab = Resources.Load<GameObject>("UIBubble");
            if (bubblePrefab == null)
            {
                Debug.LogError("UIBubble prefab not found!");
                yield break;
            }

            // 프리팹을 인스턴스화 (Canvas의 자식으로 생성)
            GameObject bubble = Instantiate(bubblePrefab, currentBubble.Rect.parent);
            
            // UIBubble 컴포넌트 가져오기
            UIBubble uiBubble = bubble.GetComponent<UIBubble>();
            if (uiBubble != null)
            {
                // 타입과 스프라이트 설정
                uiBubble.SetType(currentBubble.Type);
            }

            // 버블 초기화 (이전 애니메이션 및 상태 정리)
            if (bubble != null)
            {
                // 이전 DOTween 애니메이션 Kill
                bubble.transform.DOKill();
            }

            // 버블의 시작 위치를 UI 버블의 월드 위치로 설정
            RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
            bubbleRect.position = bubbleStartPos;

            // Nero 게이지의 위치 가져오기 (FillImage의 RectTransform)
            RectTransform fillImageRect = FillImage.GetComponent<RectTransform>();
            Vector3 targetPos = fillImageRect.position;

            // 버블이 게이지로 이동하는 애니메이션
            float moveDuration = 0.5f;
            Sequence moveSeq = DOTween.Sequence();

            // 이동 애니메이션 (RectTransform.DOMove 사용)
            moveSeq.Append(bubbleRect.DOMove(targetPos, moveDuration)
                .SetEase(Ease.InQuad));

            // 이동 완료 후 게이지 채우기 및 오브젝트 제거
            moveSeq.OnComplete(() =>
            {
                // 버블이 도착한 후 게이지 채우기
                AddFillCount();
                
                if (bubble != null)
                {
                    // 애니메이션 정리
                    bubble.transform.DOKill();
                    Destroy(bubble);
                }
            });

            // 이동 애니메이션이 끝날 때까지 대기
            yield return new WaitForSeconds(moveDuration);
        }
        else
        {
            // 버블이 없어도 게이지 채우기
            AddFillCount();
        }

        // 재장전 시작
        UIBubbleShooter.ReloadAfterShot();

        // 재장전 애니메이션이 끝나고 Normal 상태로 전환될 때까지 대기
        yield return new WaitUntil(() =>
            IngameManager.Instance.CurrentState == BattleState.Normal ||
            !UIBubbleShooter.BubbleRotation.IsRotating);
        reloadBubbleCoroutine = null;
    }



    public void AddFillCount()
    {
        currentFillCount++;

        if (currentFillCount >= MaxFillCount)
        {
            currentFillCount = 0;
            SpawnNeroBubble();
        }

        UpdateFillImage();
    }

    private void UpdateFillImage()
    {
        float targetFill = (float)currentFillCount / MaxFillCount;

        // 기존 애니메이션이 있으면 정리
        if (fillTween != null && fillTween.IsActive())
        {
            fillTween.Kill();
        }

        // 게이지가 올라가면서 채워지는 애니메이션
        fillTween = FillImage.DOFillAmount(targetFill, 0.3f)
            .SetEase(Ease.OutQuad);
    }

    private void SpawnNeroBubble()
    {
        // UI 슈터에 Nero 버블이 곧 들어올 예정임을 표시
        if (UIBubbleShooter != null)
        {
            UIBubbleShooter.SetNeroBubblePending();
            // ReloadAfterShot이 호출될 때 Nero 로직이 실행됨
        }
    }
}
