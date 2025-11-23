using UnityEngine;
using System;
using System.Collections.Generic;

public class UIBubblePreview : MonoBehaviour
{

    private struct HysteresisInfo
    {
        public Bubble lastHitBubble;
        public Vector3 lastPreviewPos;
        public bool lastWasLeftSide;
    }


    [Header("프리뷰 설정")]
    [SerializeField] private GameObject previewBubblePrefab;
    [SerializeField] private float previewAlpha = 0.5f;

    [Header("참조")]
    public UIBubbleAim Aim;
    public HexMap HexMapController;

    [Header("크기 설정")]
    [SerializeField] private float bubbleRadius = 0.4f;

    private GameObject previewBubbleInstance;
    private Vector3? previewPosition = null;

    private HysteresisInfo hysteresis;

    private float hexMapBubbleRadius = 0.25f;
    private float hexMapWidth = 0.5f;
    private float hexMapHeight = 0.433f;

    private const float HYSTERESIS_THRESHOLD = 0.1f;


    public Vector3? GetPreviewPosition() => previewPosition;
    public bool HasPreviewPosition() => previewPosition.HasValue;
    public Vector3? GetLastPreviewPosition() => hysteresis.lastPreviewPos != Vector3.zero ? hysteresis.lastPreviewPos : previewPosition;


    private float GetHysteresisThreshold() => hexMapBubbleRadius * HYSTERESIS_THRESHOLD;

    private bool GetLastWasLeftSide() => hysteresis.lastWasLeftSide;



    void Start()
    {
        LoadHexMapInfo();
        SetupPreviewBubble();
    }

    void Update()
    {
        UpdatePreviewBubble();
    }


    private void LoadHexMapInfo()
    {
        if (HexMapController != null)
        {
            hexMapWidth = HexMapController.HexWidth > 0 ? HexMapController.HexWidth : hexMapWidth;
            hexMapHeight = HexMapController.HexHeight > 0 ? HexMapController.HexHeight : hexMapHeight;
            hexMapBubbleRadius = HexMapController.BubbleRadius > 0 ? HexMapController.BubbleRadius : hexMapBubbleRadius;
        }
    }


    private void SetupPreviewBubble()
    {
        previewBubbleInstance = Instantiate(previewBubblePrefab);
        previewBubbleInstance.transform.SetParent(null);

        HidePreviewBubble();
    }


    private void UpdatePreviewBubble()
    {
        if (previewBubbleInstance == null)
            return;

        if (previewPosition.HasValue && Aim.IsAiming == true)
        {
            previewBubbleInstance.transform.position = previewPosition.Value;
            previewBubbleInstance.SetActive(true);
        }
        else
        {
            HidePreviewBubble();
        }
    }



    public void SetPreviewPosition(Vector3? position, Bubble hitBubble = null, bool isLeftSide = false)
    {
        previewPosition = position;

        if (position.HasValue)
            SetHysteresisInfo(hitBubble, position.Value, isLeftSide);
        else
            ResetHysteresis();
    }


    public Vector3? HandleBubbleHit(RaycastHit2D hit, Vector3 bubbleCenter, Bubble hitBubble, Vector2 direction, Vector3 surfacePoint, Action<Vector3> onSurfacePointAdded = null)
    {
        // 초기 검증
        if (hitBubble == null)
        {
            return null;
        }


        onSurfacePointAdded?.Invoke(surfacePoint);

        float threshold = GetHysteresisThreshold();
        bool useHysteresis = IsSameBubble(hitBubble, bubbleCenter);

        bool isLeftSide = (hit.point.x - bubbleCenter.x) < 0f;
        if (useHysteresis)
        {
            if (GetLastWasLeftSide())
                isLeftSide = (hit.point.x - bubbleCenter.x) <= threshold;
            else
                isLeftSide = (hit.point.x - bubbleCenter.x) < -threshold;
        }

        if (Mathf.Abs(hit.point.x - bubbleCenter.x) < threshold)
            isLeftSide = direction.x < 0f ? true : GetLastWasLeftSide();


        // 착지 위치 계산
        Vector3 emptySpacePos = HexMapController.FindEmptyHexSpace(bubbleCenter, hit.point, isLeftSide);

        // 착지 위치가 유효한지 확인 (bubbleCenter의 인접 위치인지, 그리고 비어있는지)
        var (centerRow, centerCol) = HexMapController.WorldToGrid(bubbleCenter);
        var (landingRow, landingCol) = HexMapController.WorldToGrid(emptySpacePos);


        // 인접 위치인지 확인
        var adjacent = HexMapController.GetAdjacentCells(centerRow, centerCol);
        bool isAdjacent = false;
        foreach (var (row, col) in adjacent)
        {
            if (row == landingRow && col == landingCol)
            {
                isAdjacent = true;
                break;
            }
        }


        foreach (var (row, col) in adjacent)
        {
            bool isEmpty = HexMapController.IsEmpty(row, col);

        }

        // 인접 위치가 아니거나 비어있지 않으면 프리뷰 표시 안 함
        if (!isAdjacent || !HexMapController.IsEmpty(landingRow, landingCol))
        {
            // 인접 위치 중 빈 공간 찾기
            bool foundEmpty = false;
            foreach (var (row, col) in adjacent)
            {
                if (HexMapController.IsEmpty(row, col))
                {
                    emptySpacePos = HexMapController.Positions[row, col];
                    foundEmpty = true;
                    break;
                }
            }

            if (!foundEmpty)
            {
                SetPreviewPosition(null);
                ResetHysteresis();
                return null;
            }
        }

        SetHysteresisInfo(hitBubble, emptySpacePos, isLeftSide);
        SetPreviewPosition(emptySpacePos, hitBubble, isLeftSide);
        return emptySpacePos;
    }


    private void SetHysteresisInfo(Bubble hitBubble, Vector3 previewPos, bool isLeftSide)
    {
        hysteresis.lastHitBubble = hitBubble;
        hysteresis.lastPreviewPos = previewPos;
        hysteresis.lastWasLeftSide = isLeftSide;
    }

    public void ResetHysteresis()
    {
        hysteresis.lastHitBubble = null;
        hysteresis.lastPreviewPos = Vector3.zero;
        hysteresis.lastWasLeftSide = false;
    }

    private bool IsSameBubble(Bubble hitBubble, Vector3 bubblePos)
    {
        return hysteresis.lastHitBubble != null &&
               hysteresis.lastHitBubble == hitBubble &&
               Vector3.Distance(bubblePos, hysteresis.lastHitBubble.transform.position) < 0.001f;
    }



    public void HidePreviewBubble()
    {
        if (previewBubbleInstance != null)
            previewBubbleInstance.SetActive(false);
    }




}
