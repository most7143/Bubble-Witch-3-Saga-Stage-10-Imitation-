// Simplified UIBubbleAim with unified wall collision helpers
using UnityEngine;
using System;
using System.Collections.Generic;

public class UIBubbleAim : MonoBehaviour
{
    [Header("참조")]
    public RectTransform Rect;
    public UIBubbleShooter Shooter;
    public LineRenderer AimLineRenderer;

    public HexMap HexMapController;

    [Header("조준 설정")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask bubbleLayer;
    [SerializeField] private float maxAimDistance = 20f;
    [SerializeField] private float bubbleRadius = 0.4f;
    [SerializeField] private int maxReflections = 5;
    [SerializeField] private float lineWidth = 0.05f;

    [Header("프리뷰")]
    public UIBubblePreview Preview;

    public bool IsAiming {get; private set;} = false;
    private int aimMode = 1;

    private Camera cam;
    private LayerMask hitMask;
    private readonly List<Vector3> aimPoints = new List<Vector3>();

    void Start()
    {
        cam = Camera.main;
        hitMask = wallLayer | bubbleLayer;
        SetupLineRenderer();
    }


    void Update()
    {

        // Normal 상태가 아니면 조준 불가 (Shooting, Destroying, RespawnBubbles 등)
        if(IngameManager.Instance.CurrentState != BattleState.Normal)
        {
            IsAiming=false;
            return;
        }


        if (IsAiming)
        {
            UpdateAim();
        }
        else
        {
            if (Preview != null)
            {
                Preview.HidePreviewBubble();
                Preview.SetNeroPreviewCenter(null);
            }
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
        {
            Preview.SetPreviewPosition(null);
            Preview.SetNeroPreviewCenter(null);
        }

        if (Shooter == null || Shooter.CurrentBubble == null)
            return;

        Vector3 start = Shooter.CurrentBubble.Rect.position;
        Vector2 dir = GetAimDirection(start);
        CalculateReflections(start, dir);

        DrawLine();
    }
    

    // 방향 가져오기, 조준 모드에 따라 달라지게 설정정
    private Vector2 GetAimDirection(Vector3 start)
    {
        Vector2 inputPos = GetPointerPosition();
        Vector3 world = cam.ScreenToWorldPoint(
            new Vector3(inputPos.x, inputPos.y, cam.WorldToScreenPoint(start).z)
        );
        Vector2 dir = (aimMode == 1) ? (world - start) : (start - world);
        return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.up;
    }

    private Vector2 GetPointerPosition()
    {
        if (Input.touchCount > 0) return Input.GetTouch(0).position;
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
            else if (IsBubble(reflectHit.collider))
            {
                HandleBubbleHit(reflectHit, start, reflectDir, reflectHit.distance, reflectPos);
            }
            else
            {
                aimPoints.Add(new Vector3(reflectHit.point.x, reflectHit.point.y, start.z));
            }
        }
    }

    private void HandleBubbleHit(RaycastHit2D hit, Vector3 start, Vector2 direction, float hitDistance, Vector2? startPos = null)
    {
        Vector2 rayStartPos = startPos ?? (Vector2)start;

        Bubble hitBubble = hit.collider.GetComponent<Bubble>();
        if (hitBubble == null) return;

        // 버블이 hexMap에 등록되어 있는지 확인 (등록되지 않은 버블은 조준선이 무시)
        // hexRow와 hexCol이 -1이면 등록되지 않은 상태
        if (!IsBubbleRegisteredInHexMap(hitBubble))
        {
            return;
        }

        Vector3 bubbleCenter = hitBubble.transform.position;
        Vector2 bubblePos2D = bubbleCenter;

        Vector2 toBubbleCenter = (bubblePos2D - hit.point).normalized;
        float distToCenter = Vector2.Distance(hit.point, bubblePos2D);
        float bubbleActualRadius = HexMapController.BubbleRadius > 0 ? HexMapController.BubbleRadius : bubbleRadius;

        float surfaceDistance = bubbleActualRadius - distToCenter;
        Vector2 surfacePoint2D = (surfaceDistance > 0.01f)
            ? hit.point + toBubbleCenter * surfaceDistance
            : hit.point;

        Vector3 surfacePoint = new Vector3(surfacePoint2D.x, surfacePoint2D.y, start.z);

        Vector3? landingPos = null;
        if (Preview != null)
        {
            landingPos = Preview.HandleBubbleHit(hit, bubbleCenter, hitBubble, direction, surfacePoint,
                (surfacePos) => aimPoints.Add(surfacePos));

            bool isNeroBubble = Shooter != null &&
                                Shooter.CurrentBubble != null &&
                                Shooter.CurrentBubble.Type == BubbleTypes.Nero;

            if (isNeroBubble)
                Preview.SetNeroPreviewCenter(landingPos);
            else
                Preview.SetNeroPreviewCenter(null);
        }
        else aimPoints.Add(surfacePoint);
    }

    private void AddLineEnd(Vector2 pos, Vector2 dir, float dist, float z)
    {
        Vector2 end = pos + dir * dist;
        aimPoints.Add(new Vector3(end.x, end.y, z));
    }

    private bool IsBubble(Collider2D c) => (bubbleLayer.value & (1 << c.gameObject.layer)) != 0;
    private bool IsWall(Collider2D c) => (wallLayer.value & (1 << c.gameObject.layer)) != 0;

    /// <summary>
    /// 버블이 hexMap에 등록되어 있는지 확인
    /// </summary>
    private bool IsBubbleRegisteredInHexMap(Bubble bubble)
    {
        if (bubble == null)
            return false;

        // Bubble의 IsRegisteredInHexMap 메서드 사용
        return bubble.IsRegisteredInHexMap();
    }

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
        if (aimPoints.Count < 2) return Vector2.up;
        return (aimPoints[1] - aimPoints[0]).normalized;
    }

    public void SetAimEnabled(bool enabled, int mode = 1)
    {
        IsAiming = enabled;
        aimMode = mode;
        AimLineRenderer.enabled = enabled;

        if (!enabled)
        {
            AimLineRenderer.positionCount = 0;
            if (Preview != null)
            {
                Preview.HidePreviewBubble();
                Preview.ResetHysteresis();
                Preview.SetNeroPreviewCenter(null);
            }
        }
    }


    /// 단일 Raycast로 벽 충돌을 확인
    public bool TryGetWallHit(Vector3 start, Vector2 dir, float distance, out RaycastHit2D hit)
    {
        hit = Physics2D.Raycast(start, dir, distance, wallLayer);
        return hit.collider != null;
    }

    /// 현재 조준 경로에서 첫 번째 벽 충돌점 가져오기
    public Vector3? GetFirstWallHitAlongAim()
    {
        if (aimPoints.Count < 2) return null;

        Vector3 start = aimPoints[0];
        Vector3 end = aimPoints[1];
        Vector2 dir = (end - start).normalized;
        float dist = Vector2.Distance(start, end);

        if (TryGetWallHit(start, dir, dist, out RaycastHit2D hit))
            return hit.point;

        return null;
    }

    /// 시작점 → 프리뷰 위치 사이에서 벽이 먼저 있는지 확인
    public bool TryGetWallBetween(Vector3 start, Vector3 preview, out RaycastHit2D hit)
    {
        Vector2 dir = ((Vector2)preview - (Vector2)start).normalized;
        float dist = Vector2.Distance(start, preview);
        return TryGetWallHit(start, dir, dist, out hit);
    }

    // path 반환
    public List<Vector3> GetPathPoints()
    {
        if (aimPoints == null || aimPoints.Count < 2) return null;
        return new List<Vector3>(aimPoints);
    }

}