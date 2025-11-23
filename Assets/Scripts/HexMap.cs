using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HexMap : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] int rows = 7;
    [SerializeField] int cols = 11;
    [SerializeField] float bubbleRadius = 0.25f;

    [Header("Start Position")]
    [SerializeField] Vector2 startPosition = new Vector2(-2.6f, 3f);

    [Header("Bubble Types")]
    [SerializeField] bool useRandomTypes = true;
    [SerializeField] BubbleTypes[] availableTypes;

    [Header("References")]
    public ObjectPool ObjectPool;
    public HexMapBubbleDestroy HexMapBubbleDestroy;

    // 실제 버블 저장: row/col이 “진짜 좌표”
    private Bubble[,] grid;
    private Vector3[,] worldPositions;

    private float hexWidth;
    private float hexHeight;

    private int initBubbleCount = 66;

    // === Public Accessors ===
    public int Rows => rows;
    public int Cols => cols;
    public float HexWidth => hexWidth;
    public float HexHeight => hexHeight;
    public Vector2 StartPosition => startPosition;
    public Vector3[,] Positions => worldPositions;

    void Awake()
    {
        InitializeGrid();
        worldPositions = BuildHexGridWorldPositions();
        HexMapBubbleDestroy.Initialize(this);
    }



    void Start()
    {
        SpawnInitialGrid();
    }

    // --------------------------------------------
    // 초기화
    // --------------------------------------------
    void InitializeGrid()
    {
        grid = new Bubble[rows, cols];
        hexWidth = bubbleRadius * 2f;
        hexHeight = Mathf.Sqrt(3f) * bubbleRadius;
    }

    // --------------------------------------------
    // 월드 좌표 생성
    // --------------------------------------------
    Vector3[,] BuildHexGridWorldPositions()
    {
        Vector3[,] pos = new Vector3[rows, cols];

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                pos[row, col] = CalculateHexPosition(row, col);
            }
        }

        return pos;
    }

    Vector3 CalculateHexPosition(int row, int col)
    {
        float xOffset = (row % 2 == 0) ? 0f : hexWidth * 0.5f;

        float x = startPosition.x + col * hexWidth + xOffset;
        float y = startPosition.y - row * (hexHeight * 0.75f);

        return new Vector3(x, y, 0f);
    }

    // --------------------------------------------
    // 초기 버블 생성
    // --------------------------------------------
    void SpawnInitialGrid()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (initBubbleCount <= 0)
                    break;

                Bubble bubble = ObjectPool.SpawnBubble(GetRandomType());
                if (bubble == null)
                    continue;

                // 월드 좌표 배치
                Vector3 wPos = worldPositions[row, col];
                bubble.transform.position = wPos;

                // grid 등록
                RegisterBubble(row, col, bubble);

                initBubbleCount--;
            }
        }
    }

    BubbleTypes GetRandomType()
    {
        int idx = Random.Range(0, availableTypes.Length);
        return availableTypes[idx];
    }

    // --------------------------------------------
    // 버블 등록 / 해제
    // --------------------------------------------
    public void RegisterBubble(int row, int col, Bubble bubble, bool checkMatches = false)
    {
        if (!IsValidCell(row, col))
            return;

        // 기존 제거
        Bubble prev = grid[row, col];
        if (prev != null && prev != bubble)
            UnregisterBubble(row, col);

        grid[row, col] = bubble;
        bubble.SetHexPosition(row, col, worldPositions[row, col]);
        
        // 매치 체크는 발사된 버블이 착지했을 때만 실행
        // 초기 생성 시에는 checkMatches=false로 호출되므로 파괴 로직이 실행되지 않음
        if (checkMatches && HexMapBubbleDestroy != null)
        {
            // 즉시 체크 (딜레이 없음)
            HexMapBubbleDestroy.CheckAndPopMatches(row, col);
        }
    }

    public void UnregisterBubble(int row, int col)
    {
        if (!IsValidCell(row, col))
            return;

        Bubble b = grid[row, col];
        if (b != null)
            b.SetHexPosition(-1, -1, Vector3.zero); // 해제

        grid[row, col] = null;
    }

    public bool IsValidCell(int row, int col)
    {
        return (row >= 0 && col >= 0 && row < rows && col < cols);
    }

    // --------------------------------------------
    // row/col 기반 인접 버블 가져오기
    // --------------------------------------------
    public List<(int row, int col)> GetAdjacentCells(int row, int col)
    {
        List<(int, int)> list = new List<(int, int)>();

        bool even = (row % 2 == 0);

        // 6방향
        int[,] offsetsEven = new int[,]
        {
            { 0, -1 }, { 0, 1 },    // 좌, 우
            { -1, 0 }, { -1, -1 },  // 좌상, 우상
            { 1, 0 }, { 1, -1 }     // 좌하, 우하
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

    // --------------------------------------------
    // row/col 기반 빈칸 찾기
    // --------------------------------------------
    public bool IsEmpty(int row, int col)
    {
        if (!IsValidCell(row, col)) return false;
        return grid[row, col] == null;
    }

    // --------------------------------------------
    // worldPosition → grid(row, col) 변환
    // (충돌 후 착지에 반드시 필요)
    // --------------------------------------------
    public (int row, int col) WorldToGrid(Vector3 worldPos)
    {
        // 세로(row) 먼저 추정
        int row = Mathf.RoundToInt((startPosition.y - worldPos.y) / (hexHeight * 0.75f));

        // clamp
        row = Mathf.Clamp(row, 0, rows - 1);

        float xPos = worldPos.x - startPosition.x;
        float offset = (row % 2 == 0) ? 0f : hexWidth * 0.5f;

        int col = Mathf.RoundToInt((xPos - offset) / hexWidth);
        col = Mathf.Clamp(col, 0, cols - 1);

        return (row, col);
    }

    // --------------------------------------------
    // 충돌 후 버블 착지 위치 계산 (핵심 기능)
    // --------------------------------------------
    public (int row, int col) FindLandingCell(Vector3 hitBubbleWorldPos, Vector3 impactPoint)
    {
        // 1) 충돌한 버블의 row/col 구함
        var (hr, hc) = WorldToGrid(hitBubbleWorldPos);

        // 2) side 판정
        bool leftSide = (impactPoint.x < hitBubbleWorldPos.x);
        
        // 3) 위/아래 판정 (충돌 지점이 버블 중심보다 위인지 아래인지)
        bool isBelow = (impactPoint.y < hitBubbleWorldPos.y);

        // 4) 붙을 위치 row/col 계산 (헥사 그리드 인접 위치 규칙)
        // GetAdjacentCells 기준:
        // 짝수 행: { 0, -1 }, { 0, 1 }, { -1, 0 }, { -1, -1 }, { 1, 0 }, { 1, -1 }
        // 홀수 행: { 0, -1 }, { 0, 1 }, { -1, 1 }, { -1, 0 }, { 1, 1 }, { 1, 0 }
        bool even = (hr % 2 == 0);
        
        int nr, nc;
        
        if (isBelow)
        {
            // 아래쪽에 붙기
            nr = hr + 1;
            if (even)
            {
                // 짝수 행 아래: { 1, 0 } 또는 { 1, -1 }
                nc = hc + (leftSide ? -1 : 0);
            }
            else
            {
                // 홀수 행 아래: { 1, 1 } 또는 { 1, 0 }
                nc = hc + (leftSide ? 0 : 1);
            }
        }
        else
        {
            // 위쪽에 붙기
            nr = hr - 1;
            if (even)
            {
                // 짝수 행 위: { -1, 0 } 또는 { -1, -1 }
                nc = hc + (leftSide ? -1 : 0);
            }
            else
            {
                // 홀수 행 위: { -1, 1 } 또는 { -1, 0 }
                nc = hc + (leftSide ? 0 : 1);
            }
        }

        if (!IsValidCell(nr, nc))
        {
            // 유효하지 않으면 인접 위치 중 가장 가까운 위치 반환
            var adjacent = GetAdjacentCells(hr, hc);
            if (adjacent.Count > 0)
            {
                // 가장 가까운 인접 위치 반환
                float minDist = float.MaxValue;
                (int, int) closest = (hr, hc);
                foreach (var (ar, ac) in adjacent)
                {
                    float dist = Vector3.Distance(impactPoint, worldPositions[ar, ac]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = (ar, ac);
                    }
                }
                return closest;
            }
            return (hr, hc);  // fallback
        }

        return (nr, nc);
    }

    // --------------------------------------------
    // grid[row,col]의 버블 반환
    // --------------------------------------------
    public Bubble GetBubble(int row, int col)
    {
        if (!IsValidCell(row, col))
            return null;
        return grid[row, col];
    }

    // --------------------------------------------
    // 헬퍼 메서드: Vector3 기반 호환성
    // --------------------------------------------
    
    /// <summary>
    /// 월드 좌표를 기반으로 빈 인접 셀 찾기 (월드 좌표 반환)
    /// </summary>
    public Vector3 FindEmptyAdjacentPosition(Vector3 worldPos, bool preferLeft = true)
    {
        var (row, col) = WorldToGrid(worldPos);
        var (emptyRow, emptyCol) = FindEmptyAdjacentCell(row, col, preferLeft);
        return worldPositions[emptyRow, emptyCol];
    }

    /// <summary>
    /// row/col 기반으로 빈 인접 셀 찾기
    /// </summary>
    public (int row, int col) FindEmptyAdjacentCell(int row, int col, bool preferLeft = true)
    {
        var adjacent = GetAdjacentCells(row, col);
        
        // 선호 방향에 따라 정렬
        if (preferLeft)
        {
            adjacent.Sort((a, b) => {
                bool aIsLeft = a.col < col;
                bool bIsLeft = b.col < col;
                if (aIsLeft != bIsLeft) return aIsLeft ? -1 : 1;
                return a.row.CompareTo(b.row); // 아래쪽 우선
            });
        }
        else
        {
            adjacent.Sort((a, b) => {
                bool aIsRight = a.col > col;
                bool bIsRight = b.col > col;
                if (aIsRight != bIsRight) return aIsRight ? -1 : 1;
                return a.row.CompareTo(b.row); // 아래쪽 우선
            });
        }

        // 빈 셀 찾기
        foreach (var (nr, nc) in adjacent)
        {
            if (IsEmpty(nr, nc))
                return (nr, nc);
        }

        // 빈 셀을 못 찾으면 원래 위치 반환
        return (row, col);
    }

    /// <summary>
    /// 버블 충돌 지점을 기반으로 빈 헥사 공간 찾기 (월드 좌표 반환)
    /// </summary>
    public Vector3 FindEmptyHexSpace(Vector3 bubblePos, Vector2 hitPoint, bool isLeftSide)
    {
        var (landingRow, landingCol) = FindLandingCell(bubblePos, hitPoint);
        
        if (IsEmpty(landingRow, landingCol))
        {
            return worldPositions[landingRow, landingCol];
        }

        // 착지 위치가 비어있지 않으면 인접 빈 공간 찾기
        var (emptyRow, emptyCol) = FindEmptyAdjacentCell(landingRow, landingCol, isLeftSide);
        return worldPositions[emptyRow, emptyCol];
    }

    /// <summary>
    /// 특정 위치 주변에 빈 공간이 있는지 확인
    /// </summary>
    public bool IsAdjacentSpaceAvailable(Vector3 bubbleCenter, Vector3 foundPosition)
    {
        if (Vector3.Distance(foundPosition, bubbleCenter) < 0.001f) return false;
        
        var (centerRow, centerCol) = WorldToGrid(bubbleCenter);
        var (foundRow, foundCol) = WorldToGrid(foundPosition);
        
        // foundPosition이 bubbleCenter의 인접 위치인지 확인
        var adjacent = GetAdjacentCells(centerRow, centerCol);
        bool isAdjacent = false;
        
        foreach (var (row, col) in adjacent)
        {
            if (row == foundRow && col == foundCol)
            {
                isAdjacent = true;
                break;
            }
        }
        
        // foundPosition이 인접 위치이고 비어있으면 true
        if (isAdjacent && IsEmpty(foundRow, foundCol))
        {
            return true;
        }
        
        // foundPosition이 인접 위치가 아니더라도, bubbleCenter 주변에 빈 공간이 있으면 true
        foreach (var (row, col) in adjacent)
        {
            if (IsEmpty(row, col))
                return true;
        }
        
        return false;
    }

    /// <summary>
    /// BubbleRadius 접근자 (호환성)
    /// </summary>
    public float BubbleRadius => bubbleRadius;
}
