using UnityEngine;
using System;
using System.Collections.Generic;

public class UIBubbleAim : MonoBehaviour
{
    [Header("참조")]
    public RectTransform Rect;
    public UIBubbleShooter Shooter;
    public LineRenderer AimLineRenderer;

    [Header("조준 설정")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask bubbleLayer;
    [SerializeField] private float maxAimDistance = 20f;
    [SerializeField] private float bubbleRadius = 0.4f;
    [SerializeField] private int maxReflections = 5;
    [SerializeField] private float lineWidth = 0.05f;
    
    [Header("프리뷰")]
    public UIBubblePreview Preview;

    private bool isAiming = false;
    private int aimMode = 1;

    private Camera cam;
    private LayerMask hitMask;
    private readonly List<Vector3> aimPoints = new List<Vector3>();

    // hex info cached
    private HexMap hexMapController;
    private float hexMapBubbleRadius = 0.25f;
    private float hexMapWidth = 0.5f;
    private float hexMapHeight = 0.433f;

    void Start()
    {
        cam = Camera.main;
        hitMask = wallLayer | bubbleLayer;
        SetupLineRenderer();
        LoadHexMapInfo();
        
     
    }

    /// <summary>
    /// 헥사맵 정보 로드: 가능한 경우 BubbleTileController의 값(positions, widths, radius)을 직접 사용한다.
    /// </summary>
    private void LoadHexMapInfo()
    {
        hexMapController = FindObjectOfType<HexMap>();
        if (hexMapController != null && hexMapController.Positions != null)
        {
            // BubbleTileController에서 직접 값 가져오기
            hexMapWidth = hexMapController.HexWidth > 0 ? hexMapController.HexWidth : hexMapWidth;
            hexMapHeight = hexMapController.HexHeight > 0 ? hexMapController.HexHeight : hexMapHeight;
            hexMapBubbleRadius = hexMapController.BubbleRadius > 0 ? hexMapController.BubbleRadius : hexMapBubbleRadius;
            
            Debug.Log($"Loaded hex map from controller: W={hexMapWidth:F3}, H={hexMapHeight:F3}, R={hexMapBubbleRadius:F3}");
            return;
        }

        // fallback: 씬에 있는 버블로부터 간격 추정 (원래 로직)
        CalculateHexMapSpacing();
    }

    private void CalculateHexMapSpacing()
    {
        Bubble[] bubbles = FindObjectsOfType<Bubble>();
        if (bubbles == null || bubbles.Length < 2)
            return;

        List<Vector3> positionsList = new List<Vector3>();
        foreach (Bubble bubble in bubbles)
        {
            if (bubble != null && bubble.gameObject.activeInHierarchy)
                positionsList.Add(bubble.transform.position);
        }

        if (positionsList.Count < 2)
            return;

        // X 간격 계산
        positionsList.Sort((a, b) => a.x.CompareTo(b.x));
        float minXDist = float.MaxValue;
        for (int i = 1; i < positionsList.Count; i++)
        {
            float dist = Mathf.Abs(positionsList[i].x - positionsList[i - 1].x);
            if (dist > 0.01f && dist < minXDist)
                minXDist = dist;
        }

        // Y 간격 계산
        positionsList.Sort((a, b) => a.y.CompareTo(b.y));
        float minYDist = float.MaxValue;
        for (int i = 1; i < positionsList.Count; i++)
        {
            float dist = Mathf.Abs(positionsList[i].y - positionsList[i - 1].y);
            if (dist > 0.01f && dist < minYDist)
                minYDist = dist;
        }

        if (minXDist < float.MaxValue)
        {
            hexMapWidth = minXDist;
            hexMapBubbleRadius = hexMapWidth * 0.5f;
        }

        if (minYDist < float.MaxValue)
        {
            hexMapHeight = minYDist / 0.75f;
            if (hexMapHeight > 0)
                hexMapBubbleRadius = hexMapHeight / Mathf.Sqrt(3f);
        }

        Debug.Log($"헥사맵 간격 추정: Width={hexMapWidth:F3}, Height={hexMapHeight:F3}, Radius={hexMapBubbleRadius:F3}");
    }

    void Update()
    {
        if (isAiming)
        {
            UpdateAim();
        }
        else
        {
            if (Preview != null)
                Preview.HidePreviewBubble();
        }
    }

    private void SetupLineRenderer()
    {
        AimLineRenderer.positionCount = 0;
        AimLineRenderer.startWidth = lineWidth;
        AimLineRenderer.endWidth = lineWidth;
        AimLineRenderer.useWorldSpace = true;
    }

    private void UpdateAim()
    {
        aimPoints.Clear();
        
        if (Preview != null)
            Preview.SetPreviewPosition(null);

        if (Shooter == null || Shooter.CurrentBubble == null)
            return;

        Vector3 start = Shooter.CurrentBubble.Rect.position;
        Vector2 dir = GetAimDirection(start);

        CalculateReflections(start, dir);
        DrawLine();
    }

    private Vector2 GetAimDirection(Vector3 start)
    {
        Vector2 inputPos = GetPointerPosition();
        Vector3 world = cam.ScreenToWorldPoint(
            new Vector3(inputPos.x, inputPos.y, cam.WorldToScreenPoint(start).z)
        );

        Debug.DrawLine(start, world, Color.red, 0.1f);

        Vector2 dir = (aimMode == 1) ? (world - start) : (start - world);
        return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.up;
    }

    private Vector2 GetPointerPosition()
    {
        if (Input.touchCount > 0)
            return Input.GetTouch(0).position;

        return Input.mousePosition;
    }

   
    private void CalculateReflections(Vector3 start, Vector2 dir)
    {
        Vector2 pos = start;
        float remaining = maxAimDistance;

        aimPoints.Add(start);

        RaycastHit2D hit = Physics2D.Raycast(pos, dir, remaining, hitMask);

        if (!hit.collider)
        {
            AddLineEnd(pos, dir, remaining, start.z);
            return;
        }

        if (IsBubble(hit.collider))
        {
            HandleBubbleHit(hit, start, dir, hit.distance);
            return;
        }

        if (IsWall(hit.collider))
        {
            Vector3 wallHitPoint = new Vector3(hit.point.x, hit.point.y, start.z);
            aimPoints.Add(wallHitPoint);

            Vector2 reflectDir = Vector2.Reflect(dir, hit.normal).normalized;
            float reflectRemain = remaining - hit.distance;
            Vector2 reflectPos = hit.point + hit.normal * 0.05f;

            RaycastHit2D reflectHit = Physics2D.Raycast(reflectPos, reflectDir, reflectRemain, hitMask);

            if (!reflectHit.collider)
            {
                AddLineEnd(reflectPos, reflectDir, reflectRemain, start.z);
            }
            else
            {
                if (IsBubble(reflectHit.collider))
                {
                    HandleBubbleHit(reflectHit, start, reflectDir, reflectHit.distance, reflectPos);
                }
                else
                {
                    aimPoints.Add(new Vector3(reflectHit.point.x, reflectHit.point.y, start.z));
                }
            }
        }
    }

    private void HandleBubbleHit(RaycastHit2D hit, Vector3 start, Vector2 direction, float hitDistance, Vector2? startPos = null)
    {
        Vector2 rayStartPos = startPos ?? (Vector2)start;

        Bubble hitBubble = hit.collider.GetComponent<Bubble>();
        if (hitBubble == null)
            return;

        Vector3 bubbleCenter = hitBubble.transform.position;
        Vector2 bubblePos2D = bubbleCenter;

        Vector2 toBubbleCenter = (bubblePos2D - hit.point).normalized;
        float distToCenter = Vector2.Distance(hit.point, bubblePos2D);
        float bubbleActualRadius = hexMapBubbleRadius > 0 ? hexMapBubbleRadius : bubbleRadius;

        // 표면까지의 보정: 충돌지점에서 중심 방향으로 surfaceDistance만큼 이동하여 '버블 표면 좌표'를 얻음.
        float surfaceDistance = bubbleActualRadius - distToCenter;
        Vector2 surfacePoint2D;
        if (surfaceDistance > 0.01f)
        {
            surfacePoint2D = hit.point + toBubbleCenter * surfaceDistance;
        }
        else
        {
            surfacePoint2D = hit.point;
        }

        Vector3 surfacePoint = new Vector3(surfacePoint2D.x, surfacePoint2D.y, start.z);
        
        // 프리뷰에서 처리하도록 위임
        if (Preview != null)
        {
            Preview.HandleBubbleHit(hit, bubbleCenter, hitBubble, direction, surfacePoint, 
                (surfacePos) => aimPoints.Add(surfacePos));
        }
        else
        {
            // Preview가 없으면 기본 처리
            aimPoints.Add(surfacePoint);
        }
    }

    private void AddLineEnd(Vector2 pos, Vector2 dir, float dist, float z)
    {
        Vector2 end = pos + dir * dist;
        aimPoints.Add(new Vector3(end.x, end.y, z));
    }

    private bool IsBubble(Collider2D c)
        => (bubbleLayer.value & (1 << c.gameObject.layer)) != 0;

    private bool IsWall(Collider2D c)
        => (wallLayer.value & (1 << c.gameObject.layer)) != 0;

    private void DrawLine()
    {
        if (aimPoints.Count < 2)
        {
            AimLineRenderer.positionCount = 0;
            return;
        }

        AimLineRenderer.positionCount = aimPoints.Count;
        AimLineRenderer.SetPositions(aimPoints.ToArray());
    }

    public Vector2 GetAimDirection()
    {
        if (aimPoints.Count < 2)
            return Vector2.up;

        return (aimPoints[1] - aimPoints[0]).normalized;
    }

    public void SetAimEnabled(bool enabled, int mode = 1)
    {
        isAiming = enabled;
        aimMode = mode;
        AimLineRenderer.enabled = enabled;

        if (!enabled)
        {
            AimLineRenderer.positionCount = 0;
            if (Preview != null)
            {
                Preview.HidePreviewBubble();
                Preview.ResetHysteresis();
            }
        }
    }
    
    /// <summary>
    /// 현재 레이 궤적 가져오기
    /// </summary>
    public List<Vector3> GetTrajectory()
    {
        if (aimPoints == null || aimPoints.Count < 2)
            return null;
            
        return new List<Vector3>(aimPoints);
    }
    
    /// <summary>
    /// 첫 번째 벽 충돌 위치 가져오기
    /// </summary>
    public Vector3? GetFirstWallHitPoint()
    {
        if (Shooter == null || Shooter.CurrentBubble == null || !isAiming)
            return null;
            
        // 현재 조준 방향으로 레이를 쏴서 첫 번째 벽 충돌 확인
        Vector3 start = Shooter.CurrentBubble.Rect.position;
        Vector2 dir = GetAimDirection(start);
        
        RaycastHit2D hit = Physics2D.Raycast(start, dir, maxAimDistance, hitMask);
        
        if (hit.collider && IsWall(hit.collider))
        {
            return new Vector3(hit.point.x, hit.point.y, start.z);
        }
        
        return null;
    }
    
    /// <summary>
    /// 첫 번째 충돌이 벽인지 확인
    /// </summary>
    public bool IsFirstHitWall()
    {
        return GetFirstWallHitPoint().HasValue;
    }
    
    /// <summary>
    /// 조준 중인지 확인
    /// </summary>
    public bool IsAiming() => isAiming;
    
    /// <summary>
    /// 특정 위치에서 벽과 충돌하는지 확인
    /// </summary>
    public bool CheckWallCollision(Vector3 position, Vector2 direction, float checkDistance = 0.1f)
    {
        RaycastHit2D hit = Physics2D.Raycast(position, direction, checkDistance, wallLayer);
        return hit.collider != null && IsWall(hit.collider);
    }
    
    /// <summary>
    /// 특정 위치에서 벽 충돌 정보 가져오기
    /// </summary>
    public RaycastHit2D? GetWallCollision(Vector3 position, Vector2 direction, float checkDistance = 0.1f)
    {
        RaycastHit2D hit = Physics2D.Raycast(position, direction, checkDistance, wallLayer);
        if (hit.collider != null && IsWall(hit.collider))
        {
            return hit;
        }
        return null;
    }
}
