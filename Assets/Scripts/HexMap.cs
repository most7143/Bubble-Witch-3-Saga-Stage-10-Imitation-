using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class HexMap : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] int rows = 7;
    [SerializeField] int cols = 11;
    [SerializeField] float bubbleRadius = 0.25f;

    [Header("Start Position")]
    [SerializeField] Vector2 startPosition = new Vector2(-2.6f, 3f);

    public Transform ParentPivot;

    [Header("References")]
    public ObjectPool ObjectPool;
    public HexMapBubbleDestroy HexMapBubbleDestroy;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showRegisteredBubbles = true;
    [SerializeField] private bool showEmptyCells = false;

    private Bubble[,] grid;

    private float hexWidth;
    private float hexHeight;

    private int limitSpawnerRows = 6;
    private int maxRegisteredRow = -1;
    private Vector3 originalBossPosition; // Boss의 원래 위치 저장
    private Tween bossMoveTween; // Boss 이동 애니메이션 추적

    public int Rows => rows;
    public int Cols => cols;
    public float HexWidth => hexWidth;
    public float HexHeight => hexHeight;
    public Vector2 StartPosition => startPosition;
    public float BubbleRadius => bubbleRadius;
    public int LimitSpawnerRows => limitSpawnerRows;

    public Vector3[,] Positions
    {
        get
        {
            Vector3[,] pos = new Vector3[rows, cols];
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                    pos[row, col] = GetWorldPosition(row, col);
            }
            return pos;
        }
    }

    void Awake()
    {
        InitializeGrid();
        HexMapBubbleDestroy.Initialize(this);
        
        // Boss 초기 위치 저장
        if (IngameManager.Instance != null && IngameManager.Instance.BossObj != null)
        {
            originalBossPosition = IngameManager.Instance.BossObj.transform.position;
        }
    }

    void InitializeGrid()
    {
        grid = new Bubble[rows, cols];
        hexWidth = bubbleRadius * 2f;
        hexHeight = Mathf.Sqrt(3f) * bubbleRadius;
    }

    public Vector3 GetWorldPosition(int row, int col)
    {
        if (!IsValidCell(row, col))
            return Vector3.zero;

        float xOffset = (row % 2 == 0) ? 0f : hexWidth * 0.5f;
        float x = startPosition.x + col * hexWidth + xOffset;
        float y = startPosition.y - row * (hexHeight * 0.75f);
        
        // limitSpawnerRows를 넘어간 버블이 있으면 Y 오프셋 적용
        if (maxRegisteredRow > limitSpawnerRows)
        {
            int rowOffset = maxRegisteredRow - limitSpawnerRows;
            float yOffset = rowOffset * (hexHeight * 0.75f);
            y += yOffset;
        }

        return new Vector3(x, y, 0f);
    }

    public void RegisterBubble(int row, int col, Bubble bubble, bool checkMatches = false)
    {
        if (!IsValidCell(row, col))
            return;

        Bubble prev = grid[row, col];
        if (prev != null && prev != bubble)
            UnregisterBubble(row, col);

        grid[row, col] = bubble;

        int previousMaxRow = maxRegisteredRow;
        maxRegisteredRow = Mathf.Max(maxRegisteredRow, row);

        Vector3 worldPos = GetWorldPosition(row, col);
        bubble.SetHexPosition(row, col, worldPos, this);

        // maxRegisteredRow가 변경되고 limitSpawnerRows를 넘어갔다면 모든 버블 위치 업데이트
        // (이미 배치된 버블만 업데이트, 이동 중인 버블은 MoveBubbleSmoothly가 처리)
        if (previousMaxRow != maxRegisteredRow && maxRegisteredRow > limitSpawnerRows)
        {
            UpdateAllBubblePositions();
        }
        // maxRegisteredRow가 limitSpawnerRows 이하로 내려갔다면 원래 위치로 복귀
        else if (previousMaxRow > limitSpawnerRows && maxRegisteredRow <= limitSpawnerRows)
        {
            UpdateAllBubblePositions();
        }

        if (checkMatches && HexMapBubbleDestroy != null)
            HexMapBubbleDestroy.CheckAndPopMatches(row, col);
    }

    public void UnregisterBubble(int row, int col)
    {
        if (!IsValidCell(row, col)) return;

        Bubble b = grid[row, col];
        if (b != null)
            b.SetHexPosition(-1, -1, Vector3.zero);

        grid[row, col] = null;

        if (row == maxRegisteredRow)
            UpdateMaxRegisteredRow();
    }

    private void UpdateMaxRegisteredRow()
    {
        int previousMaxRow = maxRegisteredRow;
        maxRegisteredRow = -1;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (grid[r, c] != null)
                    maxRegisteredRow = Mathf.Max(maxRegisteredRow, r);
        
        // maxRegisteredRow가 변경되었고 limitSpawnerRows를 넘어갔다면 모든 버블 위치 업데이트
        if (previousMaxRow != maxRegisteredRow && maxRegisteredRow > limitSpawnerRows)
        {
            UpdateAllBubblePositions();
        }
        // maxRegisteredRow가 limitSpawnerRows 이하로 내려갔다면 원래 위치로 복귀
        else if (previousMaxRow > limitSpawnerRows && maxRegisteredRow <= limitSpawnerRows)
        {
            UpdateAllBubblePositions();
        }
    }
    
    /// <summary>
    /// 모든 등록된 버블의 위치를 최신 GetWorldPosition으로 부드럽게 업데이트
    /// </summary>
    private void UpdateAllBubblePositions()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                Bubble bubble = grid[row, col];
                if (bubble != null && bubble.transform != null)
                {
                    Vector3 newWorldPos = GetWorldPosition(row, col);
                    bubble.SetHexPosition(row, col, newWorldPos, this);
                    
                    // 기존 이동 애니메이션 중지
                    bubble.transform.DOKill();
                    
                    // 부드럽게 이동 (보스와 동일한 방식)
                    bubble.transform.DOMove(newWorldPos, 0.5f)
                        .SetEase(Ease.OutCubic);
                }
            }
        }
        
        // Boss 위치도 업데이트
        UpdateBossPosition();
    }
    
    /// <summary>
    /// Y 오프셋에 따라 Boss 위치를 부드럽게 업데이트
    /// </summary>
    private void UpdateBossPosition()
    {
        if (IngameManager.Instance == null || IngameManager.Instance.BossObj == null)
            return;
        
        // 기존 애니메이션 중지
        if (bossMoveTween != null && bossMoveTween.IsActive())
        {
            bossMoveTween.Kill();
        }
        
        // Y 오프셋 계산
        float yOffset = 0f;
        if (maxRegisteredRow > limitSpawnerRows)
        {
            int rowOffset = maxRegisteredRow - limitSpawnerRows;
            yOffset = rowOffset * (hexHeight * 0.75f);
        }
        
        // Boss의 새로운 위치 계산 (X, Z는 유지, Y만 오프셋 적용)
        Vector3 targetPosition = new Vector3(
            originalBossPosition.x,
            originalBossPosition.y + yOffset,
            originalBossPosition.z
        );
        
        // 부드럽게 이동 (0.5초)
        bossMoveTween = IngameManager.Instance.BossObj.transform.DOMoveY(targetPosition.y, 0.5f)
            .SetEase(Ease.OutCubic);
    }

    public bool IsValidCell(int row, int col)
    {
        return row >= 0 && col >= 0 && row < rows && col < cols;
    }

    public List<(int row, int col)> GetAdjacentCells(int row, int col)
    {
        List<(int, int)> list = new List<(int, int)>();
        bool even = (row % 2 == 0);

        int[,] offsetsEven = new int[,]
        {
            { 0, -1 }, { 0, 1 },
            { -1, 0 }, { -1, -1 },
            { 1, 0 }, { 1, -1 }
        };

        int[,] offsetsOdd = new int[,]
        {
            { 0, -1 }, { 0, 1 },
            { -1, 1 }, { -1, 0 },
            { 1, 1 },  { 1, 0 }
        };

        int[,] offsets = even ? offsetsEven : offsetsOdd;

        for (int i = 0; i < 6; i++)
        {
            int nr = row + offsets[i, 0];
            int nc = col + offsets[i, 1];

            if (IsValidCell(nr, nc))
                list.Add((nr, nc));
        }

        return list;
    }

    public bool IsEmpty(int row, int col)
    {
        return IsValidCell(row, col) && grid[row, col] == null;
    }

    public Bubble GetBubble(int row, int col)
    {
        return IsValidCell(row, col) ? grid[row, col] : null;
    }

    public (int row, int col) WorldToGrid(Vector3 pos)
    {
        // Y 오프셋을 먼저 제거하여 실제 그리드 좌표 계산
        float yPos = pos.y;
        if (maxRegisteredRow > limitSpawnerRows)
        {
            int rowOffset = maxRegisteredRow - limitSpawnerRows;
            float yOffset = rowOffset * (hexHeight * 0.75f);
            yPos -= yOffset; // 오프셋 제거
        }
        
        int row = Mathf.RoundToInt((startPosition.y - yPos) / (hexHeight * 0.75f));
        row = Mathf.Clamp(row, 0, rows - 1);

        float xPos = pos.x - startPosition.x;
        float offset = (row % 2 == 0) ? 0f : hexWidth * 0.5f;

        int col = Mathf.RoundToInt((xPos - offset) / hexWidth);
        col = Mathf.Clamp(col, 0, cols - 1);

        return (row, col);
    }

    public (int row, int col) FindLandingCell(Vector3 hitBubbleWorldPos, Vector3 impactPoint)
    {
        var (hr, hc) = WorldToGrid(hitBubbleWorldPos);

        bool leftSide = (impactPoint.x < hitBubbleWorldPos.x);
        bool isBelow = (impactPoint.y < hitBubbleWorldPos.y);

        bool even = (hr % 2 == 0);

        int nr, nc;

        if (isBelow)
        {
            nr = hr + 1;
            if (even)
                nc = hc + (leftSide ? -1 : 0);
            else
                nc = hc + (leftSide ? 0 : 1);
        }
        else
        {
            nr = hr - 1;
            if (even)
                nc = hc + (leftSide ? -1 : 0);
            else
                nc = hc + (leftSide ? 0 : 1);
        }

        if (!IsValidCell(nr, nc))
        {
            var adjacent = GetAdjacentCells(hr, hc);
            if (adjacent.Count > 0)
            {
                float best = float.MaxValue;
                (int, int) closest = (hr, hc);

                foreach (var cell in adjacent)
                {
                    float dist = Vector3.Distance(impactPoint, GetWorldPosition(cell.row, cell.col));
                    if (dist < best)
                    {
                        best = dist;
                        closest = cell;
                    }
                }
                return closest;
            }
            return (hr, hc);
        }

        return (nr, nc);
    }
}
