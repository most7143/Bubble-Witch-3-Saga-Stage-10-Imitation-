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

    // Nero 버블이 곧 들어올 예정인지 체크하는 플래그
    private bool isNeroBubblePending = false;

    // Nero 버블이 이미 추가되었는지 체크하는 플래그
    private bool isNeroBubbleAdded = false;

    private readonly Queue<UIBubble> inactiveBubbles = new Queue<UIBubble>();
    private bool needsAutoAlignAfterShot = false;



    void Start()
    {
        ShooterButton.onClick.AddListener(ClickShooter);

        CircleCenter = Rect.anchoredPosition;

        InitializeBubbles();

        // ShottingCount 초기값 설정

        UpdateShottingCountUI();
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
        // Normal 상태가 아니면 발사 불가 (Shooting, Destroying, RespawnBubbles 등)
        if (BubbleRotation.IsRotating || IngameManager.Instance.CurrentState != BattleState.Normal) return;

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

        // 새로 장전될 버블을 즉시 선택 상태로 표시
        SelectBubble(Bubbles[0]);
        CurrentBubble = Bubbles[0];
        CurrentBubble.gameObject.SetActive(true);

        // 나머지 버블은 비선택 상태 유지
        for (int i = 1; i < Bubbles.Count; i++)
        {
            Bubbles[i].gameObject.SetActive(true);
            DeselectBubble(Bubbles[i]);
        }

    }

    /// <summary>
    /// 재장전 애니메이션 실행 (버튼 클릭 시 사용)
    /// </summary>
    public void AnimateReload()
    {
        if (Bubbles == null || Bubbles.Count < 2 || BubbleRotation == null)
            return;

        // 0번째 버블은 이미 사라진 상태이므로, 1번째 버블을 0번째로 이동
        // 버블 리스트 순환: 1번째 버블을 0번째로 이동, 0번째 버블(사라진)을 맨 뒤로
        UIBubble first = Bubbles[0];  // 사라진 버블
        UIBubble second = Bubbles[1]; // 장전될 버블

        // 리스트에서 제거
        Bubbles.RemoveAt(0);
        Bubbles.RemoveAt(0); // 이제 인덱스 0이 원래 1번째 버블

        // 사라진 버블(원래 0번째)을 맨 뒤로, 장전될 버블(원래 1번째)을 0번째로
        Bubbles.Insert(0, second); // 원래 1번째 버블을 0번째로
        Bubbles.Add(first);        // 원래 0번째 버블을 맨 뒤로

        // 사라진 버블(이제 맨 뒤)에 새 타입 설정
        BubbleTypes[] randomTypes = { BubbleTypes.Red, BubbleTypes.Blue, BubbleTypes.Yellow };
        BubbleTypes newType = randomTypes[Random.Range(0, randomTypes.Length)];
        first.SetType(newType);

        // 재장전 애니메이션 실행 (이제 Bubbles[0]이 장전될 버블, Bubbles[1]이 새로 생성된 버블)
        BubbleRotation.AnimateBubbleReload(Bubbles, CircleCenter);

        // 애니메이션 완료 후 선택 상태 업데이트
        StartCoroutine(UpdateSelectionAfterReload());
    }

    private System.Collections.IEnumerator UpdateSelectionAfterReload()
    {
        // 재장전 애니메이션이 끝날 때까지 대기
        yield return new WaitUntil(() => !BubbleRotation.IsRotating);

        // 0번째 버블 선택 (스케일은 이미 AnimateBubbleRotation에서 처리됨)
        if (Bubbles != null && Bubbles.Count > 0)
        {
            CurrentBubble = Bubbles[0];
            // SelectBubble을 호출하지 않음 (스케일 애니메이션 중복 방지)

            // 나머지 버블은 비선택 상태
            for (int i = 1; i < Bubbles.Count; i++)
            {
                DeselectBubble(Bubbles[i]);
            }
        }

        // 애니메이션 완료 후 Normal 상태로 전환
        if (IngameManager.Instance != null && IngameManager.Instance.CurrentState == BattleState.Reloading)
        {
            IngameManager.Instance.ChangeState(BattleState.Normal);
        }
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

        // 회전 애니메이션이 진행 중이면 스케일 애니메이션은 AnimateBubbleRotation에서 처리
        // 회전 애니메이션이 없으면 즉시 스케일 변경
        if (BubbleRotation != null && !BubbleRotation.IsRotating)
        {
            bubble.Rect.DOScale(bubble.SelectedScale, 0.3f)
                .SetEase(Ease.OutBack);
        }
        else
        {
            // 회전 애니메이션 중이면 즉시 변경하지 않음 (애니메이션에서 처리)
            bubble.Rect.localScale = bubble.SelectedScale;
        }
    }

    public void DeselectBubble(UIBubble bubble)
    {
        bubble.Rect.localScale = bubble.DeselectedScale;
    }

    public void UnselectBubble()
    {
        if (CurrentBubble != null)
        {
            // Nero 버블이 발사되면 플래그 리셋
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
            if (IngameManager.Instance != null)
            {
                IngameManager.Instance.ChangeState(BattleState.Reloading);
            }

            AddNeroBubbleToShooter();
            isNeroBubblePending = false;
            isNeroBubbleAdded = true;
            return;
        }

        if (needsAutoAlignAfterShot && Bubbles.Count >= 2)
        {
            needsAutoAlignAfterShot = false;

            if (IngameManager.Instance != null)
            {
                IngameManager.Instance.ChangeState(BattleState.Reloading);
            }

            BubbleRotation.AnimateBubbleRotation(Bubbles, BubbleRotationTypes.Rotate);
            StartCoroutine(UpdateSelectionAfterReload());
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
            if (IngameManager.Instance != null)
            {
                IngameManager.Instance.ChangeState(BattleState.Reloading);
            }

            UIBubble newBubble = CreateOrReuseBubble(GetRandomPlayableType(), false);
            if (newBubble == null)
                return;

            Bubbles.Add(newBubble);

            BubbleRotation.AnimateReloadAfterShot(Bubbles, CircleCenter);
            StartCoroutine(UpdateSelectionAfterReload());
        }
    }

    /// <summary>
    /// Nero 버블이 곧 들어올 예정인지 확인
    /// </summary>
    public bool IsNeroBubblePending()
    {
        return isNeroBubblePending;
    }

    /// <summary>
    /// Nero 버블이 곧 들어올 예정임을 표시
    /// </summary>
    public void SetNeroBubblePending()
    {
        isNeroBubblePending = true;
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

        // 이미 Nero 버블이 추가되었으면 중복 추가 방지
        if (isNeroBubbleAdded)
        {
            return;
        }

        // 0번째에 Nero 버블 추가
        UIBubble neroBubble = CreateOrReuseBubble(BubbleTypes.Nero);
        if (neroBubble == null)
            return;

        Bubbles.Insert(0, neroBubble);


        UIBubble extraBubble = CreateOrReuseBubble(GetRandomPlayableType());
        if (extraBubble != null)
        {
            Bubbles.Add(extraBubble);
        }

        // Nero 버블 추가 완료 플래그 설정
        isNeroBubbleAdded = true;

        // 버블 배치 및 스케일 애니메이션
        BubbleRotation.AnimateNeroBubbleSpawn(Bubbles, CircleCenter);

        // 애니메이션 완료 후 선택 상태 업데이트
        StartCoroutine(UpdateSelectionAfterNeroSpawn());
    }

    private System.Collections.IEnumerator UpdateSelectionAfterNeroSpawn()
    {
        // 스케일 애니메이션이 끝날 때까지 대기
        yield return new WaitUntil(() => !BubbleRotation.IsRotating);

        // 0번째 버블(Nero) 선택
        if (Bubbles != null && Bubbles.Count > 0)
        {
            SelectBubble(Bubbles[0]);
            CurrentBubble = Bubbles[0];

            // 나머지 버블은 비선택 상태
            for (int i = 1; i < Bubbles.Count; i++)
            {
                DeselectBubble(Bubbles[i]);
            }
        }

        // Nero 버블이 사용되면 플래그 리셋 (다음 Nero 버블을 위해)
        // 또는 Nero 버블이 발사되면 리셋
    }

    /// <summary>
    /// Nero 버블이 발사되었을 때 호출 (플래그 리셋)
    /// </summary>
    public void OnNeroBubbleShot()
    {
        isNeroBubbleAdded = false;
    }

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

    private BubbleTypes GetRandomPlayableType()
    {
        BubbleTypes[] randomTypes = { BubbleTypes.Red, BubbleTypes.Blue, BubbleTypes.Yellow };
        return randomTypes[Random.Range(0, randomTypes.Length)];
    }


    public void UpdateShottingCountUI()
    {
        ShottingCount.text = ShottingCountValue.ToString();
    }
}
