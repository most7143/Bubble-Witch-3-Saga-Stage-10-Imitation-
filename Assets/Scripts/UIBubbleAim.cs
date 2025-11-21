using UnityEngine;
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

    private bool isAiming = false;
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
        if (isAiming)
            UpdateAim();
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
        Vector3 start = Shooter.CurrentBubble.Rect.position;
        Vector2 dir = GetAimDirection(start);

        CalculateReflections(start, dir);
        DrawLine();
    }

    // --- 방향 계산 ----------------------------------------------------------

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

    // --- 반사 처리 ----------------------------------------------------------

    private void CalculateReflections(Vector3 start, Vector2 dir)
    {
        Vector2 pos = start;
        float remaining = maxAimDistance;

        aimPoints.Add(start);

        // 첫 번째 레이캐스트
        RaycastHit2D hit = Physics2D.Raycast(pos, dir, remaining, hitMask);

        if (!hit.collider)
        {
            // 충돌 없음
            AddLineEnd(pos, dir, remaining, start.z);
            return;
        }

        // 첫 번째 충돌 지점 추가
        aimPoints.Add(new Vector3(hit.point.x, hit.point.y, start.z));

        // 버블 충돌 → 종료
        if (IsBubble(hit.collider))
        {
            Debug.Log("버블 충돌 - 레이 종료");
            return;
        }

        // 벽 충돌 → 1번만 반사
        if (IsWall(hit.collider))
        {
            Vector2 reflectDir = Vector2.Reflect(dir, hit.normal).normalized;
            float reflectRemain = remaining - hit.distance;
            Vector2 reflectPos = hit.point + hit.normal * 0.05f;

            // 반사된 레이캐스트
            RaycastHit2D reflectHit = Physics2D.Raycast(reflectPos, reflectDir, reflectRemain, hitMask);

            if (!reflectHit.collider)
            {
                // 반사 후 충돌 없음
                AddLineEnd(reflectPos, reflectDir, reflectRemain, start.z);
            }
            else
            {
                // 반사 후 충돌 (버블이든 벽이든 종료)
                aimPoints.Add(new Vector3(reflectHit.point.x, reflectHit.point.y, start.z));
                
                if (IsBubble(reflectHit.collider))
                {
                    Debug.Log("반사 후 버블 충돌 - 레이 종료");
                }
                else
                {
                    Debug.Log("반사 후 벽 충돌 - 레이 종료 (더 이상 반사 안 함)");
                }
            }
        }
    }

    private void AddLineEnd(Vector2 pos, Vector2 dir, float dist, float z)
    {
        Vector2 end = pos + dir * dist;
        aimPoints.Add(new Vector3(end.x, end.y, z));
    }

    private void HandleWallReflection(RaycastHit2D hit, ref Vector2 pos, ref Vector2 dir, ref float remain)
    {
        dir = Vector2.Reflect(dir, hit.normal).normalized;
        remain -= hit.distance;
        pos = hit.point + hit.normal * 0.05f;
    }

    private bool IsBubble(Collider2D c)
        => (bubbleLayer.value & (1 << c.gameObject.layer)) != 0;

    private bool IsWall(Collider2D c)
        => (wallLayer.value & (1 << c.gameObject.layer)) != 0;

    // --- 라인 렌더링 ----------------------------------------------------------

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

    // --- 외부 인터페이스 ------------------------------------------------------

    public void SetAimEnabled(bool enabled, int mode = 1)
    {
        isAiming = enabled;
        aimMode = mode;
        AimLineRenderer.enabled = enabled;

        if (!enabled)
            AimLineRenderer.positionCount = 0;
    }

    public Vector2 GetAimDirection()
    {
        if (aimPoints.Count < 2)
            return Vector2.up;

        return (aimPoints[1] - aimPoints[0]).normalized;
    }
}
