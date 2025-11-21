using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIBubbleShot : MonoBehaviour
{
    [Header("참조")]
    public UIBubbleAim Aim;
    public UIBubblePreview Preview;
    public UIBubbleShooter Shooter;
    public HexMap HexMap;
    public ObjectPool BubblePool;

    [Header("발사 설정")]
    public float shotSpeed = 5f; // 이동 속도
    private bool isShooting = false;
    private GameObject currentShotBubble = null;

    void Start()
    {
        if (Aim == null) Aim = FindObjectOfType<UIBubbleAim>();
        if (Preview == null) Preview = FindObjectOfType<UIBubblePreview>();
        if (Shooter == null) Shooter = FindObjectOfType<UIBubbleShooter>();
        if (HexMap == null) HexMap = FindObjectOfType<HexMap>();
    }

    public void ShootBubble()
    {
        if (isShooting || Shooter == null || Shooter.CurrentBubble == null || Preview == null || Aim == null)
            return;

        Vector3? targetPosition = Preview.GetPreviewBubbleWorldPosition();
        if (!targetPosition.HasValue) return;

        UIBubble uiBubble = Shooter.CurrentBubble;
        Vector3 startPosition = uiBubble.Rect.position;

        Bubble bubbleObj = CreateBubbleFromUI(uiBubble);
        if (bubbleObj == null) return;

        StartCoroutine(MoveBubbleCoroutine(bubbleObj, startPosition, targetPosition.Value));
    }

    private Bubble CreateBubbleFromUI(UIBubble uiBubble)
    {
        if (BubblePool == null)
            BubblePool = FindObjectOfType<ObjectPool>();
        if (BubblePool == null)
        {
            Debug.LogError("BubblePool을 찾을 수 없습니다.");
            return null;
        }

        Bubble bubble = BubblePool.SpawnBubble();
        if (bubble == null) return null;

        bubble.SetBubbleType(uiBubble.Type);
        bubble.transform.position = uiBubble.Rect.position;
        return bubble;
    }

    private IEnumerator MoveBubbleCoroutine(Bubble bubble, Vector3 start, Vector3 target)
    {
        isShooting = true;
        currentShotBubble = bubble.gameObject;

        Shooter.CurrentBubble.gameObject.SetActive(false);
        Aim?.SetAimEnabled(false);

        Vector3 position = start;
        Vector3 direction = (target - start).normalized;

        while (Vector3.Distance(position, target) > 0.05f)
        {
            // 이동
            position += direction * shotSpeed * Time.deltaTime;

            // 벽 충돌 처리
            if (Aim != null)
            {
                // 현재 위치에서 이동 방향으로 벽 충돌 확인
                RaycastHit2D? wallHit = Aim.GetWallCollision(position, direction, shotSpeed * Time.deltaTime * 1.5f);
                
                if (wallHit.HasValue)
                {
                    // 벽 충돌 지점으로 위치 조정
                    position = wallHit.Value.point;
                    
                    // 벽의 법선 벡터를 사용하여 반사
                    Vector2 reflectDir = Vector2.Reflect(direction, wallHit.Value.normal).normalized;
                    direction = reflectDir;
                }
            }

            bubble.transform.position = position;
            yield return null;
        }

        // 최종 위치 정확히
        bubble.transform.position = target;

        // 그리드 정렬 및 HexMap 등록
        Vector3 snappedPos = SnapToGrid(target);
        if (HexMap != null)
            HexMap.RegisterBubble(bubble, snappedPos);
        else
            bubble.UpdateHexMapPosition(snappedPos);

        // UI 업데이트
        Shooter.UpdateSelectedBubble();
        Shooter.CurrentBubble?.gameObject.SetActive(true);

        isShooting = false;
        currentShotBubble = null;
        Aim?.SetAimEnabled(true);
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        if (HexMap == null) return position;

        float hexWidth = HexMap.HexWidth;
        float hexHeight = HexMap.HexHeight;
        if (hexWidth <= 0f || hexHeight <= 0f) return position;

        float roundedX = Mathf.Round(position.x / hexWidth) * hexWidth;
        float roundedY = Mathf.Round(position.y / (hexHeight * 0.75f)) * (hexHeight * 0.75f);
        Vector3 snappedPos = new Vector3(roundedX, roundedY, position.z);

        if (HexMap.IsPositionOccupied(snappedPos))
        {
            snappedPos = HexMap.FindEmptyAdjacentPosition(snappedPos, true);
        }

        return snappedPos;
    }

    public bool IsShooting() => isShooting;
}
