using UnityEngine;
using System.Collections.Generic;
using System;

public class UIBubblePreview : MonoBehaviour
{
    [Header("프리뷰 설정")]
    [SerializeField] private GameObject previewBubblePrefab;
    [SerializeField] private float previewAlpha = 0.5f;
    
    [Header("참조")]
    public UIBubbleShooter Shooter;
    public HexMap HexMapController;
    
    [Header("크기 설정")]
    [SerializeField] private float bubbleRadius = 0.4f;
    
    // 프리뷰 인스턴스
    private GameObject previewBubbleInstance;
    private SpriteRenderer previewSpriteRenderer;
    private Vector3? previewPosition = null;
    
    // 프리뷰 위치 안정화를 위한 변수들 (히스테리시스)
    private Bubble lastHitBubble = null;
    private Vector3 lastPreviewPos = Vector3.zero;
    private bool lastWasLeftSide = false;
    private const float HYSTERESIS_THRESHOLD = 0.1f;
    
    // 헥사맵 정보 캐싱
    private float hexMapBubbleRadius = 0.25f;
    private float hexMapWidth = 0.5f;
    private float hexMapHeight = 0.433f;
    
    void Start()
    {
        LoadHexMapInfo();
        SetupPreviewBubble();
    }
    
    void Update()
    {
        UpdatePreviewBubble();
    }
    
    /// <summary>
    /// 헥사맵 정보 로드
    /// </summary>
    private void LoadHexMapInfo()
    {
        if (HexMapController == null)
            HexMapController = FindObjectOfType<HexMap>();
            
        if (HexMapController != null)
        {
            hexMapWidth = HexMapController.HexWidth > 0 ? HexMapController.HexWidth : hexMapWidth;
            hexMapHeight = HexMapController.HexHeight > 0 ? HexMapController.HexHeight : hexMapHeight;
            hexMapBubbleRadius = HexMapController.BubbleRadius > 0 ? HexMapController.BubbleRadius : hexMapBubbleRadius;
        }
    }
    
    /// <summary>
    /// 프리뷰 버블 설정
    /// </summary>
    private void SetupPreviewBubble()
    {
        if (previewBubblePrefab == null)
        {
            Bubble existingBubble = FindObjectOfType<Bubble>();
            Sprite bubbleSprite = null;
            if (existingBubble != null && existingBubble.SpriteRenderer != null)
                bubbleSprite = existingBubble.SpriteRenderer.sprite;

            GameObject previewObj = new GameObject("PreviewBubble");
            previewObj.transform.SetParent(null);
            SpriteRenderer sr = previewObj.AddComponent<SpriteRenderer>();
            
            if (bubbleSprite != null)
                sr.sprite = bubbleSprite;
            else
            {
                Texture2D texture = new Texture2D(64, 64);
                Color[] colors = new Color[64 * 64];
                Vector2 center = new Vector2(32, 32);
                float radius = 30f;
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        colors[y * 64 + x] = dist <= radius ? Color.white : Color.clear;
                    }
                }
                texture.SetPixels(colors);
                texture.Apply();
                bubbleSprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
                sr.sprite = bubbleSprite;
            }

            previewBubbleInstance = previewObj;
            previewSpriteRenderer = sr;
        }
        else
        {
            previewBubbleInstance = Instantiate(previewBubblePrefab);
            previewBubbleInstance.transform.SetParent(null);
            previewSpriteRenderer = previewBubbleInstance.GetComponent<SpriteRenderer>();
            if (previewSpriteRenderer == null)
                previewSpriteRenderer = previewBubbleInstance.GetComponentInChildren<SpriteRenderer>();
        }

        if (previewSpriteRenderer != null)
        {
            Color color = previewSpriteRenderer.color;
            color.a = previewAlpha;
            previewSpriteRenderer.color = color;

            float previewScale = hexMapBubbleRadius > 0 ? hexMapBubbleRadius * 2f : bubbleRadius * 2f;
            previewBubbleInstance.transform.localScale = Vector3.one * previewScale;
            previewSpriteRenderer.sortingOrder = 100;
        }

        HidePreviewBubble();
    }
    
    /// <summary>
    /// 프리뷰 위치 설정
    /// </summary>
    public void SetPreviewPosition(Vector3? position, Bubble hitBubble = null, bool isLeftSide = false)
    {
        previewPosition = position;
        
        if (position.HasValue)
        {
            lastHitBubble = hitBubble;
            lastPreviewPos = position.Value;
            lastWasLeftSide = isLeftSide;
        }
        else
        {
            lastHitBubble = null;
            lastPreviewPos = Vector3.zero;
        }
    }
    
    /// <summary>
    /// 프리뷰 업데이트
    /// </summary>
    private void UpdatePreviewBubble()
    {
        if (previewBubbleInstance == null || previewSpriteRenderer == null)
            return;

        if (previewPosition.HasValue && Shooter != null && Shooter.CurrentBubble != null)
        {
            previewBubbleInstance.transform.position = previewPosition.Value;
            previewBubbleInstance.SetActive(true);

            UpdatePreviewBubbleColor(Shooter.CurrentBubble.Type);

            float previewScale = hexMapBubbleRadius > 0 ? hexMapBubbleRadius * 2f : bubbleRadius * 2f;
            previewBubbleInstance.transform.localScale = Vector3.one * previewScale;
        }
        else
        {
            HidePreviewBubble();
        }
    }
    
    /// <summary>
    /// 프리뷰 색상 업데이트
    /// </summary>
    private void UpdatePreviewBubbleColor(BubbleTypes type)
    {
        if (previewSpriteRenderer == null)
            return;

        Color color = GetBubbleColor(type);
        color.a = previewAlpha;
        previewSpriteRenderer.color = color;
    }
    
    /// <summary>
    /// 버블 타입에 따른 색상 반환
    /// </summary>
    private Color GetBubbleColor(BubbleTypes type)
    {
        switch (type)
        {
            case BubbleTypes.Red: return Color.red;
            case BubbleTypes.Blue: return Color.blue;
            case BubbleTypes.Yellow: return Color.yellow;
            case BubbleTypes.Spell: return Color.magenta;
            case BubbleTypes.Nero: return Color.black;
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// 프리뷰 숨기기
    /// </summary>
    public void HidePreviewBubble()
    {
        if (previewBubbleInstance != null)
            previewBubbleInstance.SetActive(false);
    }
    
    /// <summary>
    /// 프리뷰 위치 가져오기
    /// </summary>
    public Vector3? GetPreviewPosition()
    {
        return previewPosition;
    }
    
    /// <summary>
    /// 프리뷰 위치가 있는지 확인
    /// </summary>
    public bool HasPreviewPosition()
    {
        return previewPosition.HasValue;
    }
    
    /// <summary>
    /// 프리뷰 버블 인스턴스의 실제 월드 위치 가져오기
    /// </summary>
    public Vector3? GetPreviewBubbleWorldPosition()
    {
        if (previewBubbleInstance != null && previewBubbleInstance.activeSelf)
        {
            return previewBubbleInstance.transform.position;
        }
        
        // 인스턴스가 없으면 저장된 위치 반환
        return previewPosition;
    }
    
    /// <summary>
    /// 히스테리시스 정보 가져오기
    /// </summary>
    public bool GetLastWasLeftSide() => lastWasLeftSide;
    
    /// <summary>
    /// 히스테리시스 정보 설정
    /// </summary>
    public void SetHysteresisInfo(Bubble hitBubble, Vector3 previewPos, bool isLeftSide)
    {
        lastHitBubble = hitBubble;
        lastPreviewPos = previewPos;
        lastWasLeftSide = isLeftSide;
    }
    
    /// <summary>
    /// 히스테리시스 정보 초기화
    /// </summary>
    public void ResetHysteresis()
    {
        lastHitBubble = null;
        lastPreviewPos = Vector3.zero;
        lastWasLeftSide = false;
    }
    
    /// <summary>
    /// 같은 버블에 대한 연속 충돌인지 확인
    /// </summary>
    public bool IsSameBubble(Bubble hitBubble, Vector3 bubblePos)
    {
        return (lastHitBubble != null && lastHitBubble == hitBubble && 
                Vector3.Distance(bubblePos, lastHitBubble.transform.position) < 0.001f);
    }
    
    /// <summary>
    /// 히스테리시스 임계값 가져오기
    /// </summary>
    public float GetHysteresisThreshold() => hexMapBubbleRadius * HYSTERESIS_THRESHOLD;


    /// <summary>
/// 버블 충돌 처리 - 프리뷰 위치 계산
/// </summary>
public Vector3? HandleBubbleHit(RaycastHit2D hit, Vector3 bubbleCenter, Bubble hitBubble, Vector2 direction, Vector3 surfacePoint, Action<Vector3> onSurfacePointAdded = null)
{
    if (hitBubble == null)
        return null;

    // 표면 위치를 레이에 추가 (콜백 사용)
    if (onSurfacePointAdded != null)
        onSurfacePointAdded(surfacePoint);

    // 미리보기 위치 계산: 빈 헥사 공간을 찾아 설정 (히스테리시스 적용)
    Vector3 emptySpacePos = FindEmptyHexSpace(bubbleCenter, hit.point, hitBubble, direction);
    
    // 충돌한 버블 주변에 빈 자리가 있는지 확인
    bool hasEmptySpace = CheckIfAdjacentSpaceAvailable(bubbleCenter, emptySpacePos);
    
    if (!hasEmptySpace)
    {
        // 빈 자리가 없으면 프리뷰 표시하지 않음
        SetPreviewPosition(null);
        ResetHysteresis();
        return null;
    }
    
    // 왼쪽/오른쪽 판단
    float hitOffsetX = hit.point.x - bubbleCenter.x;
    bool isLeftSide = hitOffsetX < 0f;
    
    // 만약 FindEmptyHexSpace가 충돌한 버블 위치(즉 빈공간 못찾음)을 반환하면
    // 주변 인접칸 중 하나(빈 칸)로 강제 설정 시도
    if (Vector3.Distance(emptySpacePos, bubbleCenter) < 0.001f)
    {
        // 인접 위치 중 첫 빈 곳을 찾아 할당
        Vector3 fallback = GetFirstEmptyAdjacent(bubbleCenter);
        
        // Fallback도 실제로 빈 자리인지 확인
        if (Vector3.Distance(fallback, bubbleCenter) < 0.001f || 
            (HexMapController != null && HexMapController.IsPositionOccupied(fallback)))
        {
            // 빈 자리가 없으면 프리뷰 표시하지 않음
            SetPreviewPosition(null);
            ResetHysteresis();
            return null;
        }
        
        // Fallback 위치의 왼쪽/오른쪽 판단
        float fallbackOffsetX = fallback.x - bubbleCenter.x;
        bool fallbackIsLeftSide = fallbackOffsetX < 0f;
        
        SetPreviewPosition(fallback, hitBubble, fallbackIsLeftSide);
        return fallback;
    }
    else
    {
        // 찾은 위치가 실제로 비어있는지 다시 확인
        if (HexMapController != null && HexMapController.IsPositionOccupied(emptySpacePos))
        {
            // 위치가 차있으면 프리뷰 표시하지 않음
            SetPreviewPosition(null);
            ResetHysteresis();
            return null;
        }
        
        SetPreviewPosition(emptySpacePos, hitBubble, isLeftSide);
        return emptySpacePos;
    }
}

/// <summary>
/// 빈 헥사 공간 찾기 (히스테리시스 적용)
/// </summary>
private Vector3 FindEmptyHexSpace(Vector3 bubblePos, Vector2 hitPoint, Bubble hitBubble, Vector2 rayDirection)
{
    float hysteresisThreshold = GetHysteresisThreshold();
    
    // 같은 버블에 대한 연속 충돌인지 확인
    bool shouldUseHysteresis = IsSameBubble(hitBubble, bubblePos);
    
    // 충돌 지점이 버블 중심의 왼쪽인지 오른쪽인지 판단
    float hitOffsetX = hitPoint.x - bubblePos.x;
    bool isLeftSide = hitOffsetX < 0f;
    
    // 히스테리시스 적용: 이전 결정이 있었다면 임계값을 사용하여 안정화
    bool lastWasLeft = GetLastWasLeftSide();
    if (shouldUseHysteresis)
    {
        if (lastWasLeft)
        {
            if (hitOffsetX > hysteresisThreshold)
                isLeftSide = false;
            else
                isLeftSide = true;
        }
        else
        {
            if (hitOffsetX < -hysteresisThreshold)
                isLeftSide = true;
            else
                isLeftSide = false;
        }
    }
    
    // 방향 벡터도 고려하여 더 안정적으로 판단
    if (Mathf.Abs(hitOffsetX) < hysteresisThreshold)
    {
        if (Mathf.Abs(rayDirection.x) > 0.1f)
        {
            isLeftSide = rayDirection.x < 0f;
        }
        else if (shouldUseHysteresis)
        {
            isLeftSide = lastWasLeft;
        }
    }
    
    // 왼쪽 충돌이면 왼쪽 아래, 오른쪽 충돌이면 오른쪽 아래로 고정
    Vector3 targetPosition;
    if (isLeftSide)
    {
        targetPosition = bubblePos + new Vector3(-hexMapWidth * 0.5f, -hexMapHeight * 0.75f, 0);
    }
    else
    {
        targetPosition = bubblePos + new Vector3(hexMapWidth * 0.5f, -hexMapHeight * 0.75f, 0);
    }
    
    // 그리드 위치로 반올림
    Vector3 roundedPos = RoundToGridPosition(targetPosition);
    
    // HexMap을 통해 위치 확인
    if (HexMapController != null)
    {
        if (HexMapController.IsPositionOccupied(roundedPos))
        {
            // 반대 방향 아래쪽 시도
            Vector3 alternativePosition;
            bool altIsLeftSide = !isLeftSide;
            
            if (altIsLeftSide)
            {
                alternativePosition = bubblePos + new Vector3(-hexMapWidth * 0.5f, -hexMapHeight * 0.75f, 0);
            }
            else
            {
                alternativePosition = bubblePos + new Vector3(hexMapWidth * 0.5f, -hexMapHeight * 0.75f, 0);
            }
            
            Vector3 altRounded = RoundToGridPosition(alternativePosition);
            if (!HexMapController.IsPositionOccupied(altRounded))
            {
                SetHysteresisInfo(hitBubble, altRounded, altIsLeftSide);
                return altRounded;
            }
            
            // 둘 다 차있으면 HexMap의 FindEmptyAdjacentPosition 사용
            Vector3 emptyPos = HexMapController.FindEmptyAdjacentPosition(bubblePos, isLeftSide);
            if (emptyPos != bubblePos)
            {
                float emptyOffsetX = emptyPos.x - bubblePos.x;
                bool emptyIsLeftSide = emptyOffsetX < 0f;
                SetHysteresisInfo(hitBubble, emptyPos, emptyIsLeftSide);
                return emptyPos;
            }
        }
    }
    
    SetHysteresisInfo(hitBubble, roundedPos, isLeftSide);
    return roundedPos;
}

/// <summary>
/// 위치를 그리드 위치로 반올림
/// </summary>
private Vector3 RoundToGridPosition(Vector3 pos)
{
    if (hexMapWidth <= 0 || hexMapHeight <= 0)
        return pos;
    
    float roundedX = Mathf.Round(pos.x / hexMapWidth) * hexMapWidth;
    float roundedY = Mathf.Round(pos.y / (hexMapHeight * 0.75f)) * (hexMapHeight * 0.75f);
    return new Vector3(roundedX, roundedY, pos.z);
}

/// <summary>
/// 충돌한 버블 주변에 빈 자리가 있는지 확인
/// </summary>
private bool CheckIfAdjacentSpaceAvailable(Vector3 bubbleCenter, Vector3 foundPosition)
{
    if (Vector3.Distance(foundPosition, bubbleCenter) < 0.001f)
        return false;
    
    if (HexMapController != null)
    {
        Vector3[] adjacentOffsets = new Vector3[]
        {
            new Vector3(hexMapWidth, 0, 0),
            new Vector3(-hexMapWidth, 0, 0),
            new Vector3(hexMapWidth * 0.5f, hexMapHeight * 0.75f, 0),
            new Vector3(-hexMapWidth * 0.5f, hexMapHeight * 0.75f, 0),
            new Vector3(hexMapWidth * 0.5f, -hexMapHeight * 0.75f, 0),
            new Vector3(-hexMapWidth * 0.5f, -hexMapHeight * 0.75f, 0)
        };
        
        foreach (var offset in adjacentOffsets)
        {
            Vector3 adjacentPos = bubbleCenter + offset;
            if (!HexMapController.IsPositionOccupied(adjacentPos))
                return true;
        }
        
        return false;
    }
    
    return true;
}

/// <summary>
/// 첫 빈 인접 위치 찾기
/// </summary>
private Vector3 GetFirstEmptyAdjacent(Vector3 bubbleCenter)
{
    if (HexMapController != null)
    {
        Vector3 emptyPos = HexMapController.FindEmptyAdjacentPosition(bubbleCenter, true);
        return emptyPos;
    }
    
    Vector3[] adjacentPositions = new Vector3[]
    {
        bubbleCenter + new Vector3(hexMapWidth, 0, 0),
        bubbleCenter + new Vector3(-hexMapWidth, 0, 0),
        bubbleCenter + new Vector3(hexMapWidth * 0.5f, hexMapHeight * 0.75f, 0),
        bubbleCenter + new Vector3(-hexMapWidth * 0.5f, hexMapHeight * 0.75f, 0),
        bubbleCenter + new Vector3(hexMapWidth * 0.5f, -hexMapHeight * 0.75f, 0),
        bubbleCenter + new Vector3(-hexMapWidth * 0.5f, -hexMapHeight * 0.75f, 0)
    };

    HashSet<Vector3> occupied = HexMapController != null ? HexMapController.GetAllOccupiedPositions() : new HashSet<Vector3>();
    float tol = hexMapBubbleRadius * 0.25f;

    foreach (var pos in adjacentPositions)
    {
        Vector3 rnd = RoundToGridPosition(pos);
        if (!IsPositionOccupied(rnd, occupied, tol))
            return rnd;
    }

    return bubbleCenter;
}

/// <summary>
/// 위치가 차있는지 확인
/// </summary>
private bool IsPositionOccupied(Vector3 pos, HashSet<Vector3> occupied, float tolerance)
{
    foreach (var o in occupied)
    {
        if (Vector3.Distance(pos, o) < tolerance)
            return true;
    }
    return false;
}
}
