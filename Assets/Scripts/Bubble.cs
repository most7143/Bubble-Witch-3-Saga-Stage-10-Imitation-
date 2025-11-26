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

    [SerializeField] private BubbleTypes Type = BubbleTypes.Red;

    // 헥사맵 좌표 정보
    private Vector3 hexMapPosition;
    private int hexRow = -1;
    private int hexCol = -1;
    private HexMap hexMap;

    // 생성 순서 번호 (에디터 디버그용)
    [SerializeField] private int spawnOrder = -1;
    private TextMesh orderTextMesh;

    public int GetSpawnOrder() => spawnOrder;
    public void SetSpawnOrder(int order)
    {
        spawnOrder = order;
        UpdateOrderDisplay();
    }

    void Start()
    {
        CreateOrderTextMesh();
        UpdateOrderDisplay();
    }

    /// <summary>
    /// 에디터에서만 보이는 순서 번호 TextMesh 생성
    /// </summary>
    private void CreateOrderTextMesh()
    {
#if UNITY_EDITOR
        // TextMesh가 없으면 생성
        if (orderTextMesh == null)
        {
            GameObject textObj = new GameObject("OrderText");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localScale = Vector3.one;
            
            orderTextMesh = textObj.AddComponent<TextMesh>();
            orderTextMesh.anchor = TextAnchor.MiddleCenter;
            orderTextMesh.alignment = TextAlignment.Center;
            orderTextMesh.fontSize = 20;
            orderTextMesh.color = Color.white;
            orderTextMesh.characterSize = 0.1f;
            orderTextMesh.fontStyle = FontStyle.Bold;
        }
#endif
    }

    /// <summary>
    /// 순서 번호 표시 업데이트
    /// </summary>
    private void UpdateOrderDisplay()
    {
#if UNITY_EDITOR
        if (orderTextMesh != null)
        {
            if (spawnOrder >= 0)
            {
                orderTextMesh.text = spawnOrder.ToString();
                orderTextMesh.gameObject.SetActive(true);
            }
            else
            {
                orderTextMesh.gameObject.SetActive(false);
            }
        }
#else
        // 빌드에서는 TextMesh 비활성화
        if (orderTextMesh != null)
        {
            orderTextMesh.gameObject.SetActive(false);
        }
#endif
    }

    /// <summary>
    /// 버블이 hexMap에 등록되어 있는지 확인
    /// </summary>
    public bool IsRegisteredInHexMap()
    {
        return hexRow >= 0 && hexCol >= 0 && hexMap != null;
    }

    public BubbleTypes GetBubbleType() => Type;
    public void SetBubble(BubbleTypes type, bool isShot = false)
    {
        Type = type;
        SetFairy(isShot);
        UpdateVisual();
    }

    private Sequence fairyAnimationSequence;

    void OnDestroy()
    {
        // 애니메이션 정리
        if (fairyAnimationSequence != null && fairyAnimationSequence.IsActive())
        {
            fairyAnimationSequence.Kill();
        }

        UnregisterFromHexMap();
    }

    /// <summary>
    /// 헥사맵에 버블 등록
    /// </summary>


    public void SetFairy(bool isShot = false)
    {
        IsFairy = false;
        FairySpriteRenderer.gameObject.SetActive(false);

        // 발사된 버블이거나 Spell 타입이면 페어리 없음
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
    /// 헥사맵 위치 가져오기
    /// </summary>
    public Vector3 GetHexMapPosition() => hexMapPosition;



    void UpdateVisual()
    {
        if (SpriteRenderer != null)
        {
            SpriteRenderer.sprite = ResourcesManager.Instance.GetBubbleSprite(Type);
        }

        if (Anim != null)
        {
            Anim.runtimeAnimatorController = ResourcesManager.Instance.GetBubbleAnimatorController(Type);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 충돌 상대가 벽 또는 버블이면 처리
        // 필요에 따라 tag 또는 layer 체크 가능
        Rigidbody2D rb = Rigid;
        if (rb == null) return;

        // 충돌 법선 방향 가져오기
        Vector2 normal = collision.contacts[0].normal;

        // 현재 속도 반사
        Vector2 velocity = rb.linearVelocity;
        Vector2 reflected = Vector2.Reflect(velocity, normal);

        // 반사력 조정 (강도)
        float bounceStrength = 1f; // 1f = 현재 속도 유지, 0.5f = 절반만 튕김 등
        rb.linearVelocity = reflected * bounceStrength;
    }

    public void DestroyBubble()
    {
        Anim.SetTrigger("Bomb");
    }


    public void FairyAnimation()
    {
        if (IsFairy && FairySpriteRenderer != null)
        {
            // 기존 애니메이션이 있으면 정리
            if (fairyAnimationSequence != null && fairyAnimationSequence.IsActive())
            {
                fairyAnimationSequence.Kill();
            }

            // 요정 스프라이트의 초기 위치 저장 (버블 중심 기준)
            Vector3 startPosition = FairySpriteRenderer.transform.localPosition;

            // 애니메이션 시퀀스 생성
            fairyAnimationSequence = DOTween.Sequence();

            // 랜덤하게 이동하는 애니메이션 반복
            CreateFairyMovement(fairyAnimationSequence, startPosition);

            // 무한 반복
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

        // 반경에 비례해 한 바퀴 도는 시간도 길어지도록 비율 적용
        float baseDuration = 4f; // 반경 minRadius일 때 기준
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


}
