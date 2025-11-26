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
    [SerializeField] private GameObject neroAreaPreviewPrefab;
    [SerializeField] private int neroPreviewRange = 2;

    [Header("참조")]
    public UIBubbleAim Aim;
    public HexMap HexMapController;

    [Header("크기 설정")]
    [SerializeField] private float bubbleRadius = 0.4f;

    private GameObject previewBubbleInstance;
    private Vector3? previewPosition = null;
    private readonly List<GameObject> neroPreviewInstances = new List<GameObject>();
    private readonly List<Vector3> neroPreviewWorldPositions = new List<Vector3>();
    private Vector3? neroPreviewCenter = null;

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
        EnsureNeroPreviewPool(1);
    }

    void Update()
    {
        UpdatePreviewBubble();
        UpdateNeroPreviewBubbles();
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

    private GameObject GetNeroPreviewPrefab()
    {
        return neroAreaPreviewPrefab != null ? neroAreaPreviewPrefab : previewBubblePrefab;
    }

    private void EnsureNeroPreviewPool(int requiredCount)
    {
        GameObject prefab = GetNeroPreviewPrefab();
        if (prefab == null)
            return;

        while (neroPreviewInstances.Count < requiredCount)
        {
            GameObject instance = Instantiate(prefab);
            instance.transform.SetParent(null);
            instance.SetActive(false);
            neroPreviewInstances.Add(instance);
        }
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
        else if (previewPosition.HasValue && Aim.IsAiming == true)
        {
            // fallback: gridPosition이 없으면 기존 방식 사용
            previewBubbleInstance.transform.position = previewPosition.Value;
            previewBubbleInstance.SetActive(true);
        }
        else
        {
            HidePreviewBubble();
        }
    }

    private void UpdateNeroPreviewBubbles()
    {
        if (neroPreviewCenter.HasValue && Aim != null && Aim.IsAiming && HexMapController != null)
        {
            var (centerRow, centerCol) = HexMapController.WorldToGrid(neroPreviewCenter.Value);
            if (!HexMapController.IsValidCell(centerRow, centerCol))
            {
                HideNeroPreviewBubbles();
                neroPreviewWorldPositions.Clear();
                return;
            }

            List<(int row, int col)> cells = GetCellsWithinRange(centerRow, centerCol, neroPreviewRange);
            if (cells.Count == 0)
            {
                HideNeroPreviewBubbles();
                neroPreviewWorldPositions.Clear();
                return;
            }

            neroPreviewWorldPositions.Clear();
            EnsureNeroPreviewPool(cells.Count);
            for (int i = 0; i < neroPreviewInstances.Count; i++)
            {
                if (i >= cells.Count)
                {
                    neroPreviewInstances[i].SetActive(false);
                    continue;
                }

                var (row, col) = cells[i];
                if (!HexMapController.IsValidCell(row, col))
                {
                    neroPreviewInstances[i].SetActive(false);
                    continue;
                }

                Vector3 worldPos = HexMapController.Positions[row, col];
                neroPreviewInstances[i].transform.position = worldPos;
                neroPreviewInstances[i].SetActive(true);
                neroPreviewWorldPositions.Add(worldPos);
            }
        }
        else
        {
            HideNeroPreviewBubbles();
            neroPreviewWorldPositions.Clear();
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

    public void SetNeroPreviewCenter(Vector3? centerPosition)
    {
        neroPreviewCenter = centerPosition;
        if (!centerPosition.HasValue)
        {
            HideNeroPreviewBubbles();
            neroPreviewWorldPositions.Clear();
        }
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

        HideNeroPreviewBubbles();
        neroPreviewWorldPositions.Clear();
    }

    private void HideNeroPreviewBubbles()
    {
        for (int i = 0; i < neroPreviewInstances.Count; i++)
        {
            if (neroPreviewInstances[i] != null)
                neroPreviewInstances[i].SetActive(false);
        }
        neroPreviewWorldPositions.Clear();
    }

    public List<Vector3> GetNeroPreviewWorldPositions()
    {
        return new List<Vector3>(neroPreviewWorldPositions);
    }

    private List<(int row, int col)> GetCellsWithinRange(int row, int col, int range)
    {
        List<(int row, int col)> result = new List<(int row, int col)>();

        if (HexMapController == null || range < 0 || !HexMapController.IsValidCell(row, col))
            return result;

        Queue<(int row, int col, int dist)> queue = new Queue<(int, int, int)>();
        HashSet<(int row, int col)> visited = new HashSet<(int, int)>();

        queue.Enqueue((row, col, 0));
        visited.Add((row, col));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add((current.row, current.col));

            if (current.dist >= range)
                continue;

            List<(int row, int col)> adjacent = HexMapController.GetAdjacentCells(current.row, current.col);
            foreach (var (adjRow, adjCol) in adjacent)
            {
                if (!visited.Contains((adjRow, adjCol)))
                {
                    visited.Add((adjRow, adjCol));
                    queue.Enqueue((adjRow, adjCol, current.dist + 1));
                }
            }
        }

        return result;
    }




}
