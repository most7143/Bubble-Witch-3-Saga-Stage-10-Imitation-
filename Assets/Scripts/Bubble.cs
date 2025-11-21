using UnityEngine;

public class Bubble : MonoBehaviour
{
    public SpriteRenderer SpriteRenderer;

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
    
    void Start()
    {
        UpdateVisual();
        // 자동 등록은 나중에 명시적으로 호출하도록 변경
        // RegisterToHexMap();
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
            
        if (hexMap != null)
        {
            hexMapPosition = transform.position;
            hexMap.RegisterBubble(this, hexMapPosition, hexRow, hexCol);
        }
    }
    
    /// <summary>
    /// 헥사맵에서 버블 해제
    /// </summary>
    private void UnregisterFromHexMap()
    {
        if (hexMap != null)
        {
            hexMap.UnregisterBubble(this);
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
        UnregisterFromHexMap();
        hexMapPosition = newPosition;
        RegisterToHexMap();
    }
    
    void UpdateVisual()
    {
        if (SpriteRenderer != null)
        {
            // 색상 변경 예시
            switch (Type)
            {
                case BubbleTypes.Red:
                    SpriteRenderer.color = Color.red;
                    break;
                case BubbleTypes.Blue:
                    SpriteRenderer.color = Color.blue;
                    break;
                case BubbleTypes.Yellow:
                    SpriteRenderer.color = Color.yellow;
                    break;
                case BubbleTypes.Spell:
                    SpriteRenderer.color = Color.magenta;
                    break;
                case BubbleTypes.Nero:
                    SpriteRenderer.color = Color.black;
                    break;
            }
        }
    }
}
