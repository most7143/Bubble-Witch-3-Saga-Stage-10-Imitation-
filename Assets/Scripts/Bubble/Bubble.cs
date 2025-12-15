using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening; // DOTween 추가

public class Bubble : MonoBehaviour
{

    public Rigidbody2D Rigid;
    public SpriteRenderer SpriteRenderer;

    public Animator Anim;


    public bool IsFairy = false;

    public SpriteRenderer FairySpriteRenderer;

    [SerializeField]
    private BubbleResourceData _bubbleResourceData;
    [SerializeField] private BubbleTypes Type = BubbleTypes.Red;

    // 헥사맵 좌표 정보
    private Vector3 hexMapPosition;
    private int hexRow = -1;
    private int hexCol = -1;
    private HexMap hexMap;

    /// <summary>
    /// 버블이 hexMap에 등록되어 있는지 확인
    /// </summary>
    public bool IsRegisteredInHexMap
    {
        get { return hexRow >= 0 && hexCol >= 0 && hexMap != null; }
    }

    /// <summary>
    /// 버블 타입 가져오기
    /// </summary>
    public BubbleTypes BubbleType
    {
        get { return Type; }
    }

    /// <summary>
    /// 헥사맵 위치 가져오기
    /// </summary>
    public Vector3 HexMapPosition
    {
        get { return hexMapPosition; }
    }
    /// <summary>
    /// 버블 타입 설정 및 시각적 업데이트
    /// </summary>
    public void SetBubble(BubbleTypes type, bool isShot = false)
    {
        Type = type;
        SetFairy(isShot);
        UpdateVisual();
    }

    private Sequence fairyAnimationSequence;

    /// <summary>
    /// 버블 파괴 시 애니메이션 정리 및 헥사맵에서 해제
    /// </summary>
    void OnDestroy()
    {
        if (fairyAnimationSequence != null && fairyAnimationSequence.IsActive())
        {
            fairyAnimationSequence.Kill();
        }

        UnregisterFromHexMap();
    }

    /// <summary>
    /// 요정 설정 및 활성화
    /// </summary>
    public void SetFairy(bool isShot = false)
    {
        IsFairy = false;
        FairySpriteRenderer.gameObject.SetActive(false);

        if (isShot || Type == BubbleTypes.Spell)
            return;

        float chance = Random.Range(0f, 1f);

        if (chance < 0.3f)
        {
            IsFairy = true;
            FairySpriteRenderer.gameObject.SetActive(true);
            FairyAnimation();
        }
    }

    /// <summary>
    /// 헥사맵에서 버블 해제
    /// </summary>
    private void UnregisterFromHexMap()
    {
        if (hexMap != null && hexRow >= 0 && hexCol >= 0)
        {
            hexMap.UnregisterBubble(hexRow, hexCol);
        }
    }

    /// <summary>
    /// 헥사맵 좌표 설정
    /// </summary>
    public void SetHexPosition(int row, int col, Vector3 worldPosition, HexMap map = null)
    {
        hexRow = row;
        hexCol = col;
        hexMapPosition = worldPosition;
        if (map != null)
        {
            hexMap = map;
        }
    }




    /// <summary>
    /// 버블 스프라이트 및 애니메이션 컨트롤러 업데이트
    /// </summary>
    void UpdateVisual()
    {
        if (SpriteRenderer != null)
        {
            SpriteRenderer.sprite = _bubbleResourceData.GetSprite(Type);
        }

        if (Anim != null)
        {
            Anim.runtimeAnimatorController = _bubbleResourceData.GetAnimator(Type);
        }
    }

    /// <summary>
    /// 충돌 시 속도 반사 처리
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D rb = Rigid;
        if (rb == null) return;

        Vector2 normal = collision.contacts[0].normal;
        Vector2 velocity = rb.linearVelocity;
        Vector2 reflected = Vector2.Reflect(velocity, normal);

        float bounceStrength = 1f;
        rb.linearVelocity = reflected * bounceStrength;
    }

    /// <summary>
    /// 버블 파괴 애니메이션 트리거
    /// </summary>
    public void DestroyBubble()
    {
        Anim.SetTrigger("Bomb");
    }

    /// <summary>
    /// 요정 원형 이동 애니메이션 시작
    /// </summary>
    public void FairyAnimation()
    {
        if (IsFairy && FairySpriteRenderer != null)
        {
            if (fairyAnimationSequence != null && fairyAnimationSequence.IsActive())
            {
                fairyAnimationSequence.Kill();
            }

            FairySpriteRenderer.transform.localPosition = Vector3.zero;
            Vector3 startPosition = Vector3.zero;

            fairyAnimationSequence = DOTween.Sequence();

            CreateFairyMovement(fairyAnimationSequence, startPosition);

            fairyAnimationSequence.SetLoops(-1);
        }
    }

    /// <summary>
    /// 요정 이동 애니메이션 생성 (랜덤하게 주변을 이동)
    /// </summary>
    private void CreateFairyMovement(Sequence sequence, Vector3 basePosition)
    {
        float minRadius = 0.15f;
        float maxRadius = 0.35f;
        float radius = Random.Range(minRadius, maxRadius);

        float baseDuration = 4f;
        float revolutionDuration = baseDuration * (radius / minRadius);

        int waypointCount = 16;
        bool clockwise = Random.value < 0.5f;

        float angleStep = (360f / waypointCount) * (clockwise ? -1f : 1f);
        float startAngle = Random.Range(0f, 360f);

        Vector3 firstPoint = basePosition + new Vector3(
            Mathf.Cos(startAngle * Mathf.Deg2Rad) * radius,
            Mathf.Sin(startAngle * Mathf.Deg2Rad) * radius,
            0f
        );
        FairySpriteRenderer.transform.localPosition = firstPoint;

        for (int i = 1; i <= waypointCount; i++)
        {
            float angle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
            Vector3 target = basePosition + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );

            sequence.Append(
                FairySpriteRenderer.transform.DOLocalMove(target, revolutionDuration / waypointCount)
                    .SetEase(Ease.Linear)
            );
        }

        sequence.AppendCallback(() => FairySpriteRenderer.transform.localPosition = firstPoint);
    }

    /// <summary>
    /// 버블이 공격으로 파괴될 때 점수 추가 및 Nero 게이지 증가
    /// </summary>
    public void AttackedDestoryBubble(BubbleTypes type)
    {
        IngameManager.Instance.DestoryBubbleAddScore(type, transform);

        if (IngameManager.Instance.NeroObj.IsActive)
        {
            IngameManager.Instance.NeroObj.AddFillCount(2);
        }
    }

    /// <summary>
    /// 버블이 떨어질 때 점수 추가
    /// </summary>
    public void DropBubbleAddScore()
    {
        IngameManager.Instance.DropBubbleAddScore(transform);
    }


}
