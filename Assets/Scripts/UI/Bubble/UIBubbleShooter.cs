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


    public int ShottingCountValue = 22;

    private bool isNeroBubblePending = false;
    private bool isNeroBubbleAdded = false;
    private readonly Queue<UIBubble> inactiveBubbles = new Queue<UIBubble>();
    private bool needsAutoAlignAfterShot = false;
    private static readonly BubbleTypes[] PLAYABLE_TYPES = { BubbleTypes.Red, BubbleTypes.Blue, BubbleTypes.Yellow };

    /// <summary>
    /// Nero 버블이 곧 들어올 예정인지 확인
    /// </summary>
    public bool IsNeroBubblePending
    {
        get { return isNeroBubblePending; }
    }

    /// <summary>
    /// Nero 버블이 곧 들어올 예정임을 표시
    /// </summary>
    public bool SetNeroBubblePending
    {
        set { isNeroBubblePending = true; }
    }

    /// <summary>
    /// Nero 버블이 발사되었을 때 호출 (플래그 리셋)
    /// </summary>
    public bool OnNeroBubbleShot
    {
        set { isNeroBubbleAdded = false; }
    }



    /// <summary>
    /// 초기화 및 버튼 이벤트 등록
    /// </summary>
    void Start()
    {
        ShooterButton.onClick.AddListener(ClickShooter);

        CircleCenter = Rect.anchoredPosition;

        InitializeBubbles();

        UpdateShottingCountUI();
    }

    /// <summary>
    /// 버블 초기화 및 배치
    /// </summary>
    private void InitializeBubbles()
    {
        if (Bubbles == null)
            Bubbles = new List<UIBubble>();

        for (int i = 0; i < 2; i++)
        {
            UIBubble bubble = CreateOrReuseBubble(GetRandomPlayableType());
            if (bubble != null)
            {
                Bubbles.Add(bubble);
            }
        }

        if (HasBubbles())
        {
            BubbleRotation.ArrangeBubblesInCircle(Bubbles, CircleCenter);
            SelectFirstBubbleAndDeselectOthers();
        }
    }



    /// <summary>
    /// 슈터 버튼 클릭 처리 (버블 회전)
    /// </summary>
    public void ClickShooter()
    {
        if (BubbleRotation.IsRotating || IngameManager.Instance.CurrentState != BattleState.Normal) return;

        UpdateSelectedBubble();  

        BubbleRotation.AnimateBubbleRotation(Bubbles, BubbleRotationTypes.Rotate);
    }

    /// <summary>
    /// 발사 후 새로운 버블 준비 (타입 설정 및 즉시 표시)
    /// </summary>
    public void PrepareNewBubbleAfterShot()
    {
        if (!HasBubbles())
            return;

        RotateBubblesList();

        Bubbles[Bubbles.Count - 1].SetType(GetRandomPlayableType());

        SetAllBubblesActive(true);

        SelectFirstBubbleAndDeselectOthers();
    }

    /// <summary>
    /// 재장전 애니메이션 실행 (버튼 클릭 시 사용)
    /// </summary>
    public void AnimateReload()
    {
        if (!HasBubbles(2) || BubbleRotation == null)
            return;

        UIBubble first = Bubbles[0];
        UIBubble second = Bubbles[1];

        Bubbles.RemoveAt(0);
        Bubbles.RemoveAt(0);
        Bubbles.Insert(0, second);
        Bubbles.Add(first);

        first.SetType(GetRandomPlayableType());

        BubbleRotation.AnimateBubbleReload(Bubbles, CircleCenter);
        StartCoroutine(UpdateSelectionAfterAnimation(false));
    }

    /// <summary>
    /// 애니메이션 완료 후 선택 상태 업데이트 (공통 로직)
    /// </summary>
    private System.Collections.IEnumerator UpdateSelectionAfterAnimation(bool selectFirstBubble = true)
    {
        yield return new WaitUntil(() => !BubbleRotation.IsRotating);

        if (HasBubbles())
        {
            if (selectFirstBubble)
            {
                SelectBubble(Bubbles[0]);
                CurrentBubble = Bubbles[0];
            }
            else
            {
                CurrentBubble = Bubbles[0];
            }

            DeselectAllBubblesExcept(0);
        }

        ChangeStateIfPossible(BattleState.Normal, BattleState.Reloading);
    }






    /// <summary>
    /// 선택된 버블 업데이트
    /// </summary>
    public void UpdateSelectedBubble()
    {
        if (!HasBubbles())
            return;

        RotateBubblesList();
        SelectFirstBubbleAndDeselectOthers();
    }

    /// <summary>
    /// 버블 선택 처리
    /// </summary>
    private void SelectBubble(UIBubble bubble)
    {
        CurrentBubble = bubble;

        if (BubbleRotation != null && !BubbleRotation.IsRotating)
        {
            bubble.Rect.DOScale(bubble.SelectedScale, 0.3f)
                .SetEase(Ease.OutBack);
        }
        else
        {
            bubble.Rect.localScale = bubble.SelectedScale;
        }
    }

    /// <summary>
    /// 버블 선택 해제 처리
    /// </summary>
    public void DeselectBubble(UIBubble bubble)
    {
        bubble.Rect.localScale = bubble.DeselectedScale;
    }

    /// <summary>
    /// 현재 선택된 버블 해제 및 비활성화
    /// </summary>
    public void UnselectBubble()
    {
        if (CurrentBubble != null)
        {
            if (CurrentBubble.Type == BubbleTypes.Nero)
            {
                isNeroBubbleAdded = false;
            }

            bool hadMoreThanTwoBeforeRemoval = Bubbles != null && Bubbles.Count > 2;

            CurrentBubble.gameObject.SetActive(false);

            if (Bubbles != null && Bubbles.Remove(CurrentBubble))
            {
                inactiveBubbles.Enqueue(CurrentBubble);
            }

            needsAutoAlignAfterShot = hadMoreThanTwoBeforeRemoval;
            CurrentBubble = null;
        }
    }

    /// <summary>
    /// 발사 후 재장전 (어디서든 호출 가능)
    /// Nero 버블이 들어올 예정이면 Nero 로직, 아니면 원래 로직
    /// </summary>
    public void ReloadAfterShot()
    {
        if (Bubbles == null || BubbleRotation == null)
            return;

        if (isNeroBubbleAdded)
            return;

        if (isNeroBubblePending)
        {
            ChangeStateToReloading();
            AddNeroBubbleToShooter();
            isNeroBubblePending = false;
            isNeroBubbleAdded = true;
            return;
        }

        if (needsAutoAlignAfterShot && HasBubbles(2))
        {
            needsAutoAlignAfterShot = false;
            ChangeStateToReloading();
            BubbleRotation.AnimateBubbleRotation(Bubbles, BubbleRotationTypes.Rotate);
            StartCoroutine(UpdateSelectionAfterAnimation(false));
            return;
        }

        if (Bubbles.Count == 0)
        {
            UIBubble baseBubble = CreateOrReuseBubble(GetRandomPlayableType());
            if (baseBubble != null)
            {
                baseBubble.gameObject.SetActive(true);
                Bubbles.Add(baseBubble);
            }
        }

        if (Bubbles.Count < 2)
        {
            ChangeStateToReloading();

            UIBubble newBubble = CreateOrReuseBubble(GetRandomPlayableType(), false);
            if (newBubble == null)
                return;

            Bubbles.Add(newBubble);
            BubbleRotation.AnimateReloadAfterShot(Bubbles, CircleCenter);
            StartCoroutine(UpdateSelectionAfterAnimation(false));
        }
    }

    /// <summary>
    /// Nero 버블을 슈터에 추가 (0번째에 Nero, 2번째에 새 버블)
    /// </summary>
    public void AddNeroBubbleToShooter()
    {
        if (BubbleRotation == null)
            return;

        if (Bubbles == null)
            Bubbles = new List<UIBubble>();

        if (isNeroBubbleAdded)
        {
            return;
        }

        UIBubble neroBubble = CreateOrReuseBubble(BubbleTypes.Nero);
        if (neroBubble == null)
            return;

        Bubbles.Insert(0, neroBubble);

        UIBubble extraBubble = CreateOrReuseBubble(GetRandomPlayableType());
        if (extraBubble != null)
        {
            Bubbles.Add(extraBubble);
        }

        isNeroBubbleAdded = true;

        BubbleRotation.AnimateNeroBubbleSpawn(Bubbles, CircleCenter);
        StartCoroutine(UpdateSelectionAfterNeroSpawn());
    }

    /// <summary>
    /// Nero 버블 스폰 후 선택 상태 업데이트
    /// </summary>
    private System.Collections.IEnumerator UpdateSelectionAfterNeroSpawn()
    {
        yield return UpdateSelectionAfterAnimation(true);
    }

    /// <summary>
    /// 버블 생성 또는 재사용
    /// </summary>
    private UIBubble CreateOrReuseBubble(BubbleTypes type, bool activateImmediately = true)
    {
        UIBubble bubble = null;

        if (inactiveBubbles.Count > 0)
        {
            bubble = inactiveBubbles.Dequeue();
        }
        else
        {
            GameObject bubblePrefab = Resources.Load<GameObject>("UIBubble");
            if (bubblePrefab == null)
            {
                Debug.LogError("UIBubble prefab not found!");
                return null;
            }

            GameObject bubbleObj = Instantiate(bubblePrefab, Rect.parent);
            bubble = bubbleObj.GetComponent<UIBubble>();
        }

        if (bubble != null)
        {
            bubble.SetType(type);
            bubble.Rect.SetParent(Rect.parent, false);
            bubble.gameObject.SetActive(activateImmediately);
        }

        return bubble;
    }

    /// <summary>
    /// 랜덤 플레이 가능한 버블 타입 반환
    /// </summary>
    private BubbleTypes GetRandomPlayableType()
    {
        return PLAYABLE_TYPES[Random.Range(0, PLAYABLE_TYPES.Length)];
    }

    /// <summary>
    /// 버블 리스트가 유효한지 확인
    /// </summary>
    private bool HasBubbles(int minCount = 1)
    {
        return Bubbles != null && Bubbles.Count >= minCount;
    }

    /// <summary>
    /// 버블 리스트를 순환 (첫 번째를 맨 뒤로)
    /// </summary>
    private void RotateBubblesList()
    {
        if (!HasBubbles())
            return;

        UIBubble first = Bubbles[0];
        Bubbles.RemoveAt(0);
        Bubbles.Add(first);
    }

    /// <summary>
    /// 첫 번째 버블 선택하고 나머지 비선택
    /// </summary>
    private void SelectFirstBubbleAndDeselectOthers()
    {
        if (!HasBubbles())
            return;

        SelectBubble(Bubbles[0]);
        CurrentBubble = Bubbles[0];
        DeselectAllBubblesExcept(0);
    }

    /// <summary>
    /// 특정 인덱스를 제외한 모든 버블 비선택
    /// </summary>
    private void DeselectAllBubblesExcept(int exceptIndex)
    {
        if (!HasBubbles())
            return;

        for (int i = 0; i < Bubbles.Count; i++)
        {
            if (i != exceptIndex)
            {
                DeselectBubble(Bubbles[i]);
            }
        }
    }

    /// <summary>
    /// 모든 버블의 활성화 상태 설정
    /// </summary>
    private void SetAllBubblesActive(bool active)
    {
        if (!HasBubbles())
            return;

        foreach (var bubble in Bubbles)
        {
            if (bubble != null)
            {
                bubble.gameObject.SetActive(active);
            }
        }
    }

    /// <summary>
    /// Reloading 상태로 전환
    /// </summary>
    private void ChangeStateToReloading()
    {
        ChangeStateIfPossible(BattleState.Reloading);
    }

    /// <summary>
    /// 상태 전환 (IngameManager null 체크 포함)
    /// </summary>
    private void ChangeStateIfPossible(BattleState newState, BattleState? requiredCurrentState = null)
    {
        if (IngameManager.Instance == null)
            return;

        if (requiredCurrentState.HasValue && IngameManager.Instance.CurrentState != requiredCurrentState.Value)
            return;

        IngameManager.Instance.ChangeState(newState);
    }


    /// <summary>
    /// 발사 횟수 UI 업데이트
    /// </summary>
    public void UpdateShottingCountUI()
    {
        ShottingCount.text = ShottingCountValue.ToString();
    }
}
