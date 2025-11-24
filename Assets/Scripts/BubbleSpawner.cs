using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BubbleMoveDirection
{
    Left,
    Right,
    Down
}

public class BubbleSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private float spawnInterval = 2f; // 버블 생성 간격
    [SerializeField] private int maxBubblesPerSpawner = 16; // 스포너별 최대 버블 개수
    
    [Header("Spawner Points")]
    [SerializeField] private List<SpawnerPoint> spawnerPoints = new List<SpawnerPoint>();
    
    [Header("References")]
    [SerializeField] private HexMap hexMap;
    [SerializeField] private ObjectPool objectPool;
    
    [SerializeField] private BubbleTypes[] availableTypes;
    
    private bool isSpawning = false;
    
    // 각 스포너별로 생성한 버블들을 저장 (버블, 경로 인덱스)
    private Dictionary<SpawnerPoint, List<(Bubble bubble, int pathIndex)>> spawnerBubbles = 
        new Dictionary<SpawnerPoint, List<(Bubble, int)>>();
    
    // 각 스포너별 첫 번째 버블의 이동 경로 저장 (row, col)
    private Dictionary<SpawnerPoint, List<(int row, int col)>> spawnerPaths = 
        new Dictionary<SpawnerPoint, List<(int, int)>>();
    
    // 각 스포너별 첫 번째 버블의 현재 상태
    private Dictionary<SpawnerPoint, (int currentRow, int currentCol, BubbleMoveDirection direction, int moveCountInRow)> firstBubbleState = 
        new Dictionary<SpawnerPoint, (int, int, BubbleMoveDirection, int)>();
    
    void Start()
    {
        InitializeSpawners();
        StartSpawning();
    }
    
    private void InitializeSpawners()
    {
        foreach (var spawner in spawnerPoints)
        {
            if (spawner != null)
            {
                spawner.UpdatePosition();
                // 각 스포너별 버블 리스트 초기화
                spawnerBubbles[spawner] = new List<(Bubble, int)>();
                // 각 스포너별 경로 초기화
                spawnerPaths[spawner] = new List<(int, int)>();
            }
        }
    }
    
    public void StartSpawning()
    {
        if (isSpawning) return;
        isSpawning = true;
        
        foreach (var spawner in spawnerPoints)
        {
            if (spawner != null)
            {
                StartCoroutine(SpawnBubblesRoutine(spawner));
            }
        }
    }
    
    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }
    
    private IEnumerator SpawnBubblesRoutine(SpawnerPoint spawner)
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnSingleBubble(spawner);
        }
    }
    private void SpawnSingleBubble(SpawnerPoint spawner)
    {
        // 새 버블 생성 전에 이 스포너가 생성한 모든 버블을 한 칸씩 이동
        MoveSpawnerBubbles(spawner);
        
        // 최대 버블 개수 확인
        int currentBubbleCount = spawnerBubbles[spawner].Count;
        if (currentBubbleCount >= maxBubblesPerSpawner)
        {
            // 최대 개수에 도달했으므로 생성하지 않음
            return;
        }
        
        BubbleTypes type = GetRandomBubbleType();
        Bubble bubble = objectPool.SpawnBubble(type);
        
        // 스폰 포인트의 헥스맵 위치 계산
        var (spawnRow, spawnCol) = hexMap.WorldToGrid(spawner.transform.position);
        
        // 첫 번째 버블인지 확인
        if (spawnerBubbles[spawner].Count == 0)
        {
            // 첫 번째 버블: 경로를 생성하고 저장
            var (nextRow, nextCol) = CalculateNextPositionForFirstBubble(spawnRow, spawnCol, spawner);
            
            // 경로에 추가
            spawnerPaths[spawner].Add((nextRow, nextCol));
            
            // 버블 배치
            Vector3 spawnPosition = hexMap.Positions[nextRow, nextCol];
            bubble.transform.position = spawnPosition;
            
            // 첫 번째 버블 상태 저장
            BubbleMoveDirection direction = spawner.IsLeftSpawner ? BubbleMoveDirection.Left : BubbleMoveDirection.Right;
            int moveCount = 1; // 첫 번째 이동이므로 1부터 시작
            firstBubbleState[spawner] = (nextRow, nextCol, direction, moveCount);
            
            // 버블 리스트에 추가 (경로 인덱스 0)
            spawnerBubbles[spawner].Add((bubble, 0));
        }
        else
        {
            // 나머지 버블: 경로의 첫 번째 위치에 배치
            if (spawnerPaths[spawner].Count > 0)
            {
                var (pathRow, pathCol) = spawnerPaths[spawner][0];
                Vector3 spawnPosition = hexMap.Positions[pathRow, pathCol];
                bubble.transform.position = spawnPosition;
                
                // 버블 리스트에 추가 (경로 인덱스 0)
                spawnerBubbles[spawner].Add((bubble, 0));
            }
        }
    }
    private BubbleTypes GetRandomBubbleType()
    {
        return availableTypes[Random.Range(0, availableTypes.Length)];
    }

    /// <summary>
    /// 특정 스포너가 생성한 모든 버블을 한 칸씩 이동
    /// </summary>
    private void MoveSpawnerBubbles(SpawnerPoint spawner)
    {
        if (!spawnerBubbles.ContainsKey(spawner) || spawnerBubbles[spawner].Count == 0)
            return;
        
        var bubbles = spawnerBubbles[spawner];
        
        // 최대 버블 개수에 도달했으면 이동 중단
        if (bubbles.Count >= maxBubblesPerSpawner)
        {
            return;
        }
        
        var path = spawnerPaths[spawner];
        
        // 첫 번째 버블의 경로 업데이트
        if (bubbles.Count > 0 && firstBubbleState.ContainsKey(spawner))
        {
            UpdateFirstBubblePath(spawner);
        }
        
        // 제거할 인덱스들을 저장
        List<int> indicesToRemove = new List<int>();
        
        // 모든 버블을 경로를 따라 한 칸씩 이동
        for (int i = 0; i < bubbles.Count; i++)
        {
            var (bubble, pathIndex) = bubbles[i];
            
            // 버블이 비활성화되었거나 null이면 나중에 제거
            if (bubble == null || !bubble.gameObject.activeSelf)
            {
                indicesToRemove.Add(i);
                continue;
            }
            
            // 경로 인덱스 증가
            int nextPathIndex = pathIndex + 1;
            
            // 경로의 끝에 도달했는지 확인
            if (nextPathIndex >= path.Count)
            {
                // 경로의 마지막 위치에 고정
                if (path.Count > 0)
                {
                    var (lastRow, lastCol) = path[path.Count - 1];
                    PlaceBubbleOnHexMap(bubble, lastRow, lastCol);
                }
                indicesToRemove.Add(i);
                continue;
            }
            
            // 다음 경로 위치로 이동
            var (nextRow, nextCol) = path[nextPathIndex];
            MoveBubbleToPosition(bubble, nextRow, nextCol, BubbleMoveDirection.Left);
            bubbles[i] = (bubble, nextPathIndex);
        }
        
        // 역순으로 제거하여 인덱스 문제 방지
        for (int i = indicesToRemove.Count - 1; i >= 0; i--)
        {
            bubbles.RemoveAt(indicesToRemove[i]);
        }
    }
    
    /// <summary>
    /// 첫 번째 버블의 경로를 업데이트
    /// </summary>
    private void UpdateFirstBubblePath(SpawnerPoint spawner)
    {
        if (!firstBubbleState.ContainsKey(spawner))
            return;
        
        var (currentRow, currentCol, direction, moveCount) = firstBubbleState[spawner];
        var path = spawnerPaths[spawner];
        
        // 현재 행의 이동 횟수 제한 확인
        int maxMoves = GetMoveCountForRow(currentRow);
        
        if (moveCount >= maxMoves)
        {
            // 이동 횟수를 모두 사용했으므로 아래로 이동
            var (downRow, downCol) = CalculateDownPosition(currentRow, currentCol, direction);
            
            if (hexMap.IsValidCell(downRow, downCol))
            {
                // 경로에 추가
                path.Add((downRow, downCol));
                
                // 홀수행에 도착했는지 확인
                if (downRow % 2 == 1)
                {
                    // 방향 플립
                    direction = (direction == BubbleMoveDirection.Left) ? BubbleMoveDirection.Right : BubbleMoveDirection.Left;
                    // 홀수행은 1번만 이동 가능하므로, 아래로 이동한 것(1번)으로 간주하여 moveCount = 1로 설정
                    firstBubbleState[spawner] = (downRow, downCol, direction, 1);
                }
                else
                {
                    // 아래로 이동도 이동 횟수에 포함되므로 moveCount = 1로 설정
                    firstBubbleState[spawner] = (downRow, downCol, direction, 1);
                }
            }
            else
            {
                // 아래로 이동할 수 없으면 경로 생성 종료
                firstBubbleState.Remove(spawner);
            }
        }
        else
        {
            // 현재 방향으로 한 칸 이동
            var (nextRow, nextCol) = CalculateNextPositionInDirection(currentRow, currentCol, direction);
            
            if (hexMap.IsValidCell(nextRow, nextCol))
            {
                // 경로에 추가
                path.Add((nextRow, nextCol));
                
                // 행이 바뀌었는지 확인
                if (nextRow != currentRow)
                {
                    // 행이 바뀌었으므로 moveCount를 0으로 초기화
                    // 홀수행에 도착했는지 확인
                    if (nextRow % 2 == 1)
                    {
                        // 방향 플립
                        direction = (direction == BubbleMoveDirection.Left) ? BubbleMoveDirection.Right : BubbleMoveDirection.Left;
                    }
                    // 새로운 행에 도착했으므로 moveCount를 0으로 리셋
                    firstBubbleState[spawner] = (nextRow, nextCol, direction, 0);
                }
                else
                {
                    // 같은 행에서 이동
                    firstBubbleState[spawner] = (nextRow, nextCol, direction, moveCount + 1);
                }
            }
            else
            {
                // 범위를 벗어나면 아래로 이동
                var (downRow, downCol) = CalculateDownPosition(currentRow, currentCol, direction);
                
                if (hexMap.IsValidCell(downRow, downCol))
                {
                    path.Add((downRow, downCol));
                    
                    // 홀수행에 도착했는지 확인
                    if (downRow % 2 == 1)
                    {
                        direction = (direction == BubbleMoveDirection.Left) ? BubbleMoveDirection.Right : BubbleMoveDirection.Left;
                        // 홀수행은 1번만 이동 가능하므로, 아래로 이동한 것(1번)으로 간주하여 moveCount = 1로 설정
                        firstBubbleState[spawner] = (downRow, downCol, direction, 1);
                    }
                    else
                    {
                        // 아래로 이동도 이동 횟수에 포함되므로 moveCount = 1로 설정
                        firstBubbleState[spawner] = (downRow, downCol, direction, 1);
                    }
                }
                else
                {
                    firstBubbleState.Remove(spawner);
                }
            }
        }
    }
    
    /// <summary>
    /// 첫 번째 버블의 다음 위치 계산
    /// </summary>
    private (int row, int col) CalculateNextPositionForFirstBubble(int row, int col, SpawnerPoint spawner)
    {
        BubbleMoveDirection direction = spawner.IsLeftSpawner ? BubbleMoveDirection.Left : BubbleMoveDirection.Right;
        return CalculateNextPositionInDirection(row, col, direction);
    }
    
    /// <summary>
    /// 방향에 따른 다음 위치 계산
    /// </summary>
    private (int row, int col) CalculateNextPositionInDirection(int row, int col, BubbleMoveDirection direction)
    {
        switch (direction)
        {
            case BubbleMoveDirection.Left:
                return (row, col - 1);
            case BubbleMoveDirection.Right:
                return (row, col + 1);
            default:
                return (row, col);
        }
    }
    
    /// <summary>
    /// 아래로 이동할 때의 위치 계산
    /// </summary>
    private (int row, int col) CalculateDownPosition(int row, int col, BubbleMoveDirection direction)
    {
        int nextRow = row + 1;
        bool isEvenRow = (row % 2 == 0);
        
        if (direction == BubbleMoveDirection.Left)
        {
            if (isEvenRow)
            {
                return (nextRow, col - 1);
            }
            else
            {
                return (nextRow, col);
            }
        }
        else // Right
        {
            if (isEvenRow)
            {
                return (nextRow, col);
            }
            else
            {
                return (nextRow, col + 1);
            }
        }
    }
    
    /// <summary>
    /// 행별 이동 횟수 제한 가져오기
    /// </summary>
    private int GetMoveCountForRow(int row)
    {
        if (row == 0)
        {
            return 2; // 0행: 2개
        }
        else if (row % 2 == 1)
        {
            return 1; // 홀수행: 1개
        }
        else
        {
            return 4; // 짝수행: 4개
        }
    }
    
    
    /// <summary>
    /// 버블을 특정 위치로 이동
    /// </summary>
    private void MoveBubbleToPosition(Bubble bubble, int row, int col, BubbleMoveDirection direction)
    {
        if (!hexMap.IsValidCell(row, col))
            return;
        
        Vector3 targetPosition = hexMap.Positions[row, col];
        bubble.transform.position = targetPosition;
    }
    
    /// <summary>
    /// 버블을 헥스맵에 고정 (이동 종료)
    /// </summary>
    private void PlaceBubbleOnHexMap(Bubble bubble, int row, int col)
    {
        if (!hexMap.IsValidCell(row, col))
            return;
        
        // 위치가 비어있지 않으면 인접한 빈 공간 찾기
        if (!hexMap.IsEmpty(row, col))
        {
            var (emptyRow, emptyCol) = hexMap.FindEmptyAdjacentCell(row, col, true);
            row = emptyRow;
            col = emptyCol;
        }
        
        // 버블 위치 설정
        Vector3 targetPosition = hexMap.Positions[row, col];
        bubble.transform.position = targetPosition;
        
        // 헥스맵에 등록
        bubble.UpdateHexMapPosition(targetPosition);
    }
}
