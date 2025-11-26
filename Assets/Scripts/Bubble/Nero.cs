using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class Nero : MonoBehaviour
{
    public Image FillImage;

    public Button AddFillButton;

    public bool IsActive = false;

    private int MaxFillCount = 100;
    private int currentFillCount = 0;

    [Header("참조")]
    public UIBubbleShooter UIBubbleShooter;

    private Tween fillTween;
    private Coroutine reloadBubbleCoroutine;

    /// <summary>
    /// AddFillButton 비활성화
    /// </summary>
    public bool DeactivateAddFillButton
    {
        set
        {
            if (value)
            {
                AddFillButton.gameObject.SetActive(false);
                IsActive = false;
            }
        }
    }

    /// <summary>
    /// AddFillButton 활성화
    /// </summary>
    public bool ActivateAddFillButton
    {
        set
        {
            if (value)
            {
                AddFillButton.gameObject.SetActive(true);
                IsActive = true;
            }
        }
    }

    /// <summary>
    /// Nero 게이지 초기화 및 버튼 이벤트 등록
    /// </summary>
    private void Start()
    {
        currentFillCount = 0;
        AddFillButton.onClick.AddListener(ClickAddFillButton);
    }

    /// <summary>
    /// Nero 게이지 채우기 버튼 클릭 처리
    /// </summary>
    public void ClickAddFillButton()
    {
        if (IngameManager.Instance.CurrentState != BattleState.Normal)
            return;

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
        if (UIBubbleShooter.CurrentBubble != null)
        {
            UIBubbleShooter.ShottingCountValue--;
            UIBubbleShooter.UpdateShottingCountUI();
            
            UIBubble currentBubble = UIBubbleShooter.CurrentBubble;
            Vector3 bubbleStartPos = currentBubble.Rect.position;

            UIBubbleShooter.UnselectBubble();

            GameObject bubblePrefab = Resources.Load<GameObject>("UIBubble");
            if (bubblePrefab == null)
            {
                Debug.LogError("UIBubble prefab not found!");
                yield break;
            }

            GameObject bubble = Instantiate(bubblePrefab, currentBubble.Rect.parent);

            UIBubble uiBubble = bubble.GetComponent<UIBubble>();
            if (uiBubble != null)
            {
                uiBubble.SetType(currentBubble.Type);
            }

            if (bubble != null)
            {
                bubble.transform.DOKill();
            }

            RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
            bubbleRect.position = bubbleStartPos;

            RectTransform fillImageRect = FillImage.GetComponent<RectTransform>();
            Vector3 targetPos = fillImageRect.position;

            float moveDuration = 0.5f;
            Sequence moveSeq = DOTween.Sequence();

            moveSeq.Append(bubbleRect.DOMove(targetPos, moveDuration)
                .SetEase(Ease.InQuad));

            moveSeq.OnComplete(() =>
            {
                AddFillCount(20);

                if (bubble != null)
                {
                    bubble.transform.DOKill();
                    Destroy(bubble);
                }
            });

            yield return new WaitForSeconds(moveDuration);
        }
        else
        {
            AddFillCount(20);
        }

        UIBubbleShooter.ReloadAfterShot();

        yield return new WaitUntil(() =>
            IngameManager.Instance.CurrentState == BattleState.Normal ||
            !UIBubbleShooter.BubbleRotation.IsRotating);
        reloadBubbleCoroutine = null;
    }



    /// <summary>
    /// Nero 게이지 채우기
    /// </summary>
    public void AddFillCount(int count)
    {
        currentFillCount+=count;

        if (currentFillCount >= MaxFillCount)
        {
            currentFillCount = 0;
            DeactivateAddFillButton = true;
            SpawnNeroBubble();
        }

        UpdateFillImage();
    }

    /// <summary>
    /// Nero 게이지 이미지 업데이트
    /// </summary>
    private void UpdateFillImage()
    {
        float targetFill = (float)currentFillCount / MaxFillCount;

        if (fillTween != null && fillTween.IsActive())
        {
            fillTween.Kill();
        }

        fillTween = FillImage.DOFillAmount(targetFill, 0.3f)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Nero 버블 스폰 예약
    /// </summary>
    private void SpawnNeroBubble()
    {
        if (UIBubbleShooter != null)
        {
            UIBubbleShooter.SetNeroBubblePending = true;
        }
    }
}
