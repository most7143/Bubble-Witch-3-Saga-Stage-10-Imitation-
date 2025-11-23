using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Bubble : MonoBehaviour
{

    public Rigidbody2D Rigid;
    public SpriteRenderer SpriteRenderer;

    public Animator Anim;

    [SerializeField] private BubbleTypes Type = BubbleTypes.Red;

    // 헥사맵 좌표 정보
    private Vector3 hexMapPosition;
    private int hexRow = -1;
    private int hexCol = -1;
    private HexMap hexMap;

    public BubbleTypes GetBubbleType() => Type;
    public void SetBubbleType(BubbleTypes type)
    {
        Type = type;
        UpdateVisual();
    }

    void OnDestroy()
    {
        UnregisterFromHexMap();
    }

    /// <summary>
    /// 헥사맵에 버블 등록
    /// </summary>
    private void RegisterToHexMap()
    {
        if (hexMap == null)
            hexMap = FindObjectOfType<HexMap>();

        if (hexMap != null && hexRow >= 0 && hexCol >= 0)
        {
            hexMapPosition = transform.position;
            hexMap.RegisterBubble(hexRow, hexCol, this);
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
    public void SetHexPosition(int row, int col, Vector3 worldPosition)
    {
        hexRow = row;
        hexCol = col;
        hexMapPosition = worldPosition;
    }

    /// <summary>
    /// 헥사맵 위치 가져오기
    /// </summary>
    public Vector3 GetHexMapPosition() => hexMapPosition;

    /// <summary>
    /// 위치가 변경되었을 때 호출 (외부에서 호출)
    /// </summary>
    public void UpdateHexMapPosition(Vector3 newPosition)
    {
        if (hexMap == null)
            hexMap = FindObjectOfType<HexMap>();

        if (hexMap != null)
        {
            UnregisterFromHexMap();
            hexMapPosition = newPosition;
            var (row, col) = hexMap.WorldToGrid(newPosition);
            hexRow = row;
            hexCol = col;
            RegisterToHexMap();
        }
    }

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

   
}
