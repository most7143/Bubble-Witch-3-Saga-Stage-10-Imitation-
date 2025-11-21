using UnityEngine;
using System.Collections.Generic;

public class HexMap : MonoBehaviour
{
    public ObjectPool ObjectPool;

    [Header("Grid Settings")]
    [SerializeField] int rows = 7;
    [SerializeField] int cols = 11;
    [SerializeField] float bubbleRadius = 0.25f;

    [Header("Start Position")]
    [SerializeField] Vector2 startPosition = new Vector2(-2.6f, 3f);

    [Header("Bubble Types")]
    [SerializeField] bool useRandomTypes = true;
    [SerializeField] BubbleTypes[] availableTypes;

    private Bubble[,] grid;
    private Vector3[,] positions;
    
    // 버블 위치 관리: 월드 좌표를 키로 사용
    private Dictionary<Vector3, Bubble> bubblePositionMap = new Dictionary<Vector3, Bubble>();
    private const float POSITION_TOLERANCE = 0.01f; // 위치 비교 허용 오차

    float hexWidth;
    float hexHeight;

    // === public accessors ===
    public Vector3[,] Positions => positions;
    public float HexWidth => hexWidth;
    public float HexHeight => hexHeight;
    public float BubbleRadius => bubbleRadius;
    public Vector2 StartPosition => startPosition;

    // Awake로 옮김: 다른 Start()에서 바로 접근 가능하게 하기 위함.
    void Awake()
    {
        InitializeGrid();
        positions = BuildHexGridPositions();
    }

    void Start()
    {
        // 실제 스폰은 Start에서 하되, positions는 Awake에서 이미 준비되어 있음.
        SpawnGridBubbles();
    }

    void InitializeGrid()
    {
        grid = new Bubble[rows, cols];

        hexWidth  = bubbleRadius * 2f;
        hexHeight = Mathf.Sqrt(3f) * bubbleRadius;
    }

    Vector3[,] BuildHexGridPositions()
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
        float offset = (row % 2 == 0) ? 0f : hexWidth * 0.5f;

        float x = startPosition.x + (col * hexWidth) + offset;
        float y = startPosition.y - (row * hexHeight * 0.75f);

        return new Vector3(x, y, 0);
    }

    void SpawnGridBubbles()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                Bubble bubble = ObjectPool.SpawnBubble();
                if (bubble == null)
                    continue;

                // 위치 배치
                Vector3 position = positions[row, col];
                bubble.transform.position = position;

                // 타입 설정
                if (useRandomTypes && availableTypes.Length > 0)
                    bubble.SetBubbleType(GetRandomType());

                // 헥사맵에 등록
                RegisterBubble(bubble, position, row, col);
                
                grid[row, col] = bubble;
            }
        }
    }

    BubbleTypes GetRandomType()
    {
        int index = Random.Range(0, availableTypes.Length);
        return availableTypes[index];
    }
    
    /// <summary>
    /// 버블 위치를 정규화하여 키로 사용 (소수점 오차 제거)
    /// </summary>
    private Vector3 NormalizePosition(Vector3 pos)
    {
        float roundedX = Mathf.Round(pos.x / POSITION_TOLERANCE) * POSITION_TOLERANCE;
        float roundedY = Mathf.Round(pos.y / POSITION_TOLERANCE) * POSITION_TOLERANCE;
        return new Vector3(roundedX, roundedY, pos.z);
    }
    
    /// <summary>
    /// 버블을 헥사맵에 등록
    /// </summary>
    public void RegisterBubble(Bubble bubble, Vector3 position, int row = -1, int col = -1)
    {
        if (bubble == null)
            return;
            
        Vector3 normalizedPos = NormalizePosition(position);
        
        // 기존 위치에 있으면 먼저 해제
        UnregisterBubble(bubble);
        
        // 새 위치에 등록
        bubblePositionMap[normalizedPos] = bubble;
        
        // Bubble에 헥사 좌표 저장
        if (bubble != null)
        {
            bubble.SetHexPosition(row, col, normalizedPos);
        }
    }
    
    /// <summary>
    /// 버블을 헥사맵에서 해제
    /// </summary>
    public void UnregisterBubble(Bubble bubble)
    {
        if (bubble == null)
            return;
            
        // 딕셔너리에서 해당 버블 찾아서 제거
        List<Vector3> keysToRemove = new List<Vector3>();
        foreach (var kvp in bubblePositionMap)
        {
            if (kvp.Value == bubble)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            bubblePositionMap.Remove(key);
        }
    }
    
    /// <summary>
    /// 해당 위치에 버블이 있는지 확인
    /// </summary>
    public bool IsPositionOccupied(Vector3 position, float tolerance = -1f)
    {
        if (tolerance < 0f)
            tolerance = POSITION_TOLERANCE;
            
        Vector3 normalizedPos = NormalizePosition(position);
        
        // 정확히 일치하는 위치 확인
        if (bubblePositionMap.ContainsKey(normalizedPos))
            return true;
            
        // 허용 오차 내에 있는지 확인
        foreach (var kvp in bubblePositionMap)
        {
            if (Vector3.Distance(kvp.Key, normalizedPos) < tolerance)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 해당 위치의 버블 반환
    /// </summary>
    public Bubble GetBubbleAtPosition(Vector3 position, float tolerance = -1f)
    {
        if (tolerance < 0f)
            tolerance = POSITION_TOLERANCE;
            
        Vector3 normalizedPos = NormalizePosition(position);
        
        // 정확히 일치하는 위치 확인
        if (bubblePositionMap.TryGetValue(normalizedPos, out Bubble bubble))
            return bubble;
            
        // 허용 오차 내에서 가장 가까운 버블 찾기
        float minDist = float.MaxValue;
        Bubble closestBubble = null;
        
        foreach (var kvp in bubblePositionMap)
        {
            float dist = Vector3.Distance(kvp.Key, normalizedPos);
            if (dist < tolerance && dist < minDist)
            {
                minDist = dist;
                closestBubble = kvp.Value;
            }
        }
        
        return closestBubble;
    }
    
    /// <summary>
    /// 모든 등록된 버블 위치 반환
    /// </summary>
    public HashSet<Vector3> GetAllOccupiedPositions()
    {
        HashSet<Vector3> occupied = new HashSet<Vector3>();
        foreach (var kvp in bubblePositionMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject.activeInHierarchy)
            {
                occupied.Add(kvp.Key);
            }
        }
        return occupied;
    }
    
    /// <summary>
    /// 특정 위치 주변의 빈 헥사 공간 찾기
    /// </summary>
    public Vector3 FindEmptyAdjacentPosition(Vector3 centerPosition, bool preferLeft = true)
    {
        Vector3[] adjacentOffsets = new Vector3[]
        {
            new Vector3(hexWidth, 0, 0),                          // 오른쪽
            new Vector3(-hexWidth, 0, 0),                         // 왼쪽
            new Vector3(hexWidth * 0.5f, hexHeight * 0.75f, 0),  // 오른쪽 위
            new Vector3(-hexWidth * 0.5f, hexHeight * 0.75f, 0), // 왼쪽 위
            new Vector3(hexWidth * 0.5f, -hexHeight * 0.75f, 0), // 오른쪽 아래
            new Vector3(-hexWidth * 0.5f, -hexHeight * 0.75f, 0) // 왼쪽 아래
        };
        
        // 선호 방향에 따라 우선순위 조정
        if (preferLeft)
        {
            // 왼쪽 아래 우선
            System.Array.Sort(adjacentOffsets, (a, b) => {
                bool aIsLeft = a.x < 0;
                bool bIsLeft = b.x < 0;
                if (aIsLeft != bIsLeft) return aIsLeft ? -1 : 1;
                return a.y.CompareTo(b.y); // 아래쪽 우선
            });
        }
        else
        {
            // 오른쪽 아래 우선
            System.Array.Sort(adjacentOffsets, (a, b) => {
                bool aIsRight = a.x > 0;
                bool bIsRight = b.x > 0;
                if (aIsRight != bIsRight) return aIsRight ? -1 : 1;
                return a.y.CompareTo(b.y); // 아래쪽 우선
            });
        }
        
        foreach (var offset in adjacentOffsets)
        {
            Vector3 candidatePos = centerPosition + offset;
            Vector3 normalizedPos = NormalizePosition(candidatePos);
            
            if (!IsPositionOccupied(normalizedPos))
            {
                return normalizedPos;
            }
        }
        
        // 빈 공간을 못 찾으면 원래 위치 반환
        return centerPosition;
    }
}
