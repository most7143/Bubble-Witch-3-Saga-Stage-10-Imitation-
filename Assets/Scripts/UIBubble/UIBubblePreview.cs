using UnityEngine;
using System;
using System.Collections.Generic;

public class UIBubblePreview : MonoBehaviour
{
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
    private (int row, int col)? previewGridPosition = null;
    private readonly List<GameObject> neroPreviewInstances = new List<GameObject>();
    private readonly List<Vector3> neroPreviewWorldPositions = new List<Vector3>();
    private Vector3? neroPreviewCenter = null;

    private float hexMapBubbleRadius = 0.25f;
    private float hexMapWidth = 0.5f;
    private float hexMapHeight = 0.433f;

    public Vector3? GetPreviewPosition() => previewPosition;
    public bool HasPreviewPosition() => previewPosition.HasValue;

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

        if (previewGridPosition.HasValue && Aim.IsAiming == true && HexMapController != null)
        {
            var (row, col) = previewGridPosition.Value;
            if (HexMapController.IsValidCell(row, col))
            {
                Vector3 currentPosition = HexMapController.GetWorldPosition(row, col);
                previewBubbleInstance.transform.position = currentPosition;
                previewBubbleInstance.SetActive(true);
            }
            else
            {
                HidePreviewBubble();
            }
        }
        else if (previewPosition.HasValue && Aim.IsAiming == true)
        {
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

                Vector3 worldPos = HexMapController.GetWorldPosition(row, col);
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

        if (position.HasValue && HexMapController != null)
        {
            var (row, col) = HexMapController.WorldToGrid(position.Value);
            previewGridPosition = HexMapController.IsValidCell(row, col)
                ? (row, col)
                : null;
        }
        else
        {
            previewGridPosition = null;
        }
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

    public Vector3? HandleBubbleHit(
        RaycastHit2D hit,
        Vector3 bubbleCenter,
        Bubble hitBubble,
        Vector2 direction,
        Vector3 surfacePoint,
        Action<Vector3> onSurfacePointAdded = null)
    {
        if (hitBubble == null || HexMapController == null)
        {
            SetPreviewPosition(null);
            return null;
        }

        onSurfacePointAdded?.Invoke(surfacePoint);

        bool isLeftSide = hit.point.x < bubbleCenter.x;

        var (centerRow, centerCol) = HexMapController.WorldToGrid(bubbleCenter);
        if (!HexMapController.IsValidCell(centerRow, centerCol))
        {
            SetPreviewPosition(null);
            return null;
        }

        var adjacent = HexMapController.GetAdjacentCells(centerRow, centerCol);

        List<(int row, int col)> bottomCells = new List<(int row, int col)>();
        foreach (var (row, col) in adjacent)
        {
            if (row > centerRow)
                bottomCells.Add((row, col));
        }

        (int row, int col)? targetCell = null;

        if (bottomCells.Count >= 2)
        {
            bottomCells.Sort((a, b) =>
            {
                Vector3 posA = HexMapController.GetWorldPosition(a.row, a.col);
                Vector3 posB = HexMapController.GetWorldPosition(b.row, b.col);
                return posA.x.CompareTo(posB.x);
            });

            targetCell = isLeftSide ? bottomCells[0] : bottomCells[^1];
        }
        else if (bottomCells.Count == 1)
        {
            targetCell = bottomCells[0];
        }

        if (targetCell.HasValue)
        {
            var (row, col) = targetCell.Value;

            if (HexMapController.IsEmpty(row, col))
            {
                Vector3 previewPos = HexMapController.GetWorldPosition(row, col);
                SetPreviewPosition(previewPos);
                return previewPos;
            }
        }

        SetPreviewPosition(null);
        return null;
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
        foreach (var inst in neroPreviewInstances)
        {
            if (inst != null) inst.SetActive(false);
        }
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

            foreach (var adj in HexMapController.GetAdjacentCells(current.row, current.col))
            {
                if (!visited.Contains(adj))
                {
                    visited.Add(adj);
                    queue.Enqueue((adj.row, adj.col, current.dist + 1));
                }
            }
        }

        return result;
    }
}
