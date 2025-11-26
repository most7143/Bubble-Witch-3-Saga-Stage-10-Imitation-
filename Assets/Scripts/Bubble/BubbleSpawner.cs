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
    [SerializeField] private float moveSpeed = 0.2f; // 버블 이동 속도 (초)

    [Header("Spawner Points")]
    [SerializeField] private List<SpawnerPoint> spawnerPoints = new List<SpawnerPoint>();

    [Header("References")]
    [SerializeField] private HexMap hexMap;
    [SerializeField] private ObjectPool objectPool;

    [SerializeField] private BubbleTypes[] availableTypes;

    [Header("Debug")]
    [SerializeField] private bool showPathGizmos = true; // 경로 시각화 활성화
    [SerializeField] private Color pathColor = Color.cyan; // 경로 색상

    private bool isSpawning = false;
    private bool isRefilling = false; // 재생성 중인지 체크

    // 각 스포너별 생성 순서 카운터
    private Dictionary<SpawnerPoint, int> spawnOrderCounters = 
        new Dictionary<SpawnerPoint, int>();

    // 각 스포너별로 생성한 버블들을 저장 (버블, 경로 인덱스, 현재 row, col)
    private Dictionary<SpawnerPoint, List<(Bubble bubble, int pathIndex, int currentRow, int currentCol)>> spawnerBubbles =
        new Dictionary<SpawnerPoint, List<(Bubble, int, int, int)>>();

    // 각 스포너별 첫 번째 버블의 이동 경로 저장 (row, col)
    private Dictionary<SpawnerPoint, List<(int row, int col)>> spawnerPaths =
        new Dictionary<SpawnerPoint, List<(int, int)>>();

    // 각 스포너별 첫 번째 버블의 현재 상태
    private Dictionary<SpawnerPoint, (int currentRow, int currentCol, BubbleMoveDirection direction, int moveCountInRow)> firstBubbleState =
        new Dictionary<SpawnerPoint, (int, int, BubbleMoveDirection, int)>();

    /// <summary>
    /// 스포너 초기화
    /// </summary>
    void Start()
    {
        InitializeSpawners();
    }

    /// <summary>
    /// 모든 스포너 초기화
    /// </summary>
    private void InitializeSpawners()
    {
        foreach (var spawner in spawnerPoints)
        {
            if (spawner != null)
            {
                spawner.UpdatePosition();
                spawnerBubbles[spawner] = new List<(Bubble, int, int, int)>();
                spawnerPaths[spawner] = new List<(int, int)>();
                spawnOrderCounters[spawner] = 0;
            }
        }
    }

    /// <summary>
    /// 초기 버블 생성 시작
    /// </summary>
    public void StartSpawning()
    {
        if (isSpawning) return;
        isSpawning = true;
        IngameManager.Instance.ChangeState(BattleState.RespawnBubbles);

        StartCoroutine(InitialSpawnRoutine());
    }

    /// <summary>
    /// 초기 버블 배치 루틴 (모든 스포너가 최대 버블 수에 도달할 때까지)
    /// </summary>
    private IEnumerator InitialSpawnRoutine()
    {
        foreach (var spawner in spawnerPoints)
        {
            if (spawner != null)
            {
                spawner.ActivateSpawner();
            }
        }

        foreach (var spawner in spawnerPoints)
        {
            if (spawner != null)
            {
                StartCoroutine(SpawnBubblesRoutine(spawner));
            }
        }

        while (true)
        {
            bool allSpawnersFull = true;
            
            foreach (var spawner in spawnerPoints)
            {
                if (spawner == null) continue;
                
                int currentCount = spawnerBubbles.ContainsKey(spawner) ? spawnerBubbles[spawner].Count : 0;
                if (currentCount < maxBubblesPerSpawner)
                {
                    allSpawnersFull = false;
                    break;
                }
            }

            if (allSpawnersFull)
            {
                foreach (var spawner in spawnerPoints)
                {
                    if (spawner != null)
                    {
                        spawner.DeactivateSpawner();
                    }
                }

                isSpawning = false;
                IngameManager.Instance.ChangeState(BattleState.Normal);
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// 버블 생성 중지
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }

    /// <summary>
    /// 버블 파괴 후 재생성 (외부에서 호출)
    /// </summary>
    public void OnBubblesDestroyed()
    {
        if (isRefilling)
            return;

        IngameManager.Instance.ChangeState(BattleState.RespawnBubbles);

        if (IngameManager.Instance.BossObj != null)
        {
            IngameManager.Instance.BossObj.SpawnBubbleForRefill();
        }
    }

    /// <summary>
    /// 버블 재생성 코루틴
    /// </summary>
    private IEnumerator RefillBubbles()
    {
        if (isRefilling)
            yield break;

        isRefilling = true;

        RemoveDestroyedBubbles();

        Dictionary<SpawnerPoint, int> neededCounts = new Dictionary<SpawnerPoint, int>();
        foreach (var spawner in spawnerPoints)
        {
            if (spawner == null) continue;

            int currentCount = spawnerBubbles.ContainsKey(spawner) ? spawnerBubbles[spawner].Count : 0;
            int neededCount = maxBubblesPerSpawner - currentCount;
            
            if (neededCount > 0)
            {
                neededCounts[spawner] = neededCount;
            }
        }

        foreach (var kvp in neededCounts)
        {
            if (kvp.Key != null)
            {
                kvp.Key.ActivateSpawner();
            }
        }

        List<Coroutine> refillCoroutines = new List<Coroutine>();
        foreach (var kvp in neededCounts)
        {
            Coroutine coroutine = StartCoroutine(RefillSingleSpawner(kvp.Key, kvp.Value));
            refillCoroutines.Add(coroutine);
        }

        foreach (var coroutine in refillCoroutines)
        {
            yield return coroutine;
        }

        foreach (var kvp in neededCounts)
        {
            if (kvp.Key != null)
            {
                kvp.Key.DeactivateSpawner();
            }
        }

        IngameManager.Instance.ChangeState(BattleState.Reloading);
        
        isRefilling = false;
    }

    /// <summary>
    /// 단일 스포너의 버블 재생성
    /// </summary>
    private IEnumerator RefillSingleSpawner(SpawnerPoint spawner, int neededCount)
    {
        for (int i = 0; i < neededCount; i++)
        {
            MoveSpawnerBubbles(spawner);
            
            CreateAndPlaceBubble(spawner);
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// 파괴된 버블을 spawnerBubbles에서 제거
    /// </summary>
    private void RemoveDestroyedBubbles()
    {
        foreach (var spawner in spawnerPoints)
        {
            if (spawner == null || !spawnerBubbles.ContainsKey(spawner)) continue;

            var bubbles = spawnerBubbles[spawner];
            List<int> indicesToRemove = new List<int>();

            for (int i = 0; i < bubbles.Count; i++)
            {
                var (bubble, pathIndex, currentRow, currentCol) = bubbles[i];

                if (bubble == null || !bubble.gameObject.activeSelf)
                {
                    if (currentRow >= 0 && currentCol >= 0)
                    {
                        hexMap.UnregisterBubble(currentRow, currentCol);
                    }
                    indicesToRemove.Add(i);
                    continue;
                }

                if (currentRow >= 0 && currentCol >= 0)
                {
                    Bubble registeredBubble = hexMap.GetBubble(currentRow, currentCol);
                    if (registeredBubble != bubble)
                    {
                        indicesToRemove.Add(i);
                    }
                }
            }

            for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                bubbles.RemoveAt(indicesToRemove[i]);
            }
        }
    }

    /// <summary>
    /// 스포너별 버블 생성 루틴
    /// </summary>
    private IEnumerator SpawnBubblesRoutine(SpawnerPoint spawner)
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnSingleBubble(spawner);
        }
    }
    /// <summary>
    /// 단일 버블 생성 (기존 버블 이동 후 생성)
    /// </summary>
    private void SpawnSingleBubble(SpawnerPoint spawner)
    {
        MoveSpawnerBubbles(spawner);

        CreateAndPlaceBubble(spawner);
    }

    /// <summary>
    /// 버블 생성 및 배치 (통합 로직)
    /// </summary>
    private void CreateAndPlaceBubble(SpawnerPoint spawner)
    {
        int currentBubbleCount = spawnerBubbles[spawner].Count;
        
        if (currentBubbleCount >= maxBubblesPerSpawner)
        {
            return;
        }

        BubbleTypes type = GetRandomBubbleType();
        Bubble bubble = objectPool.SpawnBubble(type);

        if (!spawnOrderCounters.ContainsKey(spawner))
        {
            spawnOrderCounters[spawner] = 0;
        }

        Vector3 spawnPosition = spawner.transform.position;
        bubble.transform.position = spawnPosition;

        var (spawnRow, spawnCol) = hexMap.WorldToGrid(spawnPosition);

        if (spawnerBubbles[spawner].Count == 0)
        {
            if (hexMap.IsValidCell(spawnRow, spawnCol) && hexMap.IsEmpty(spawnRow, spawnCol))
            {
                hexMap.RegisterBubble(spawnRow, spawnCol, bubble, false);
                spawnerBubbles[spawner].Add((bubble, 0, spawnRow, spawnCol));

                BubbleMoveDirection direction = spawner.IsLeftSpawner ?
                    BubbleMoveDirection.Left : BubbleMoveDirection.Right;

                var (firstPathRow, firstPathCol) = CalculateNextPositionForFirstBubble(spawnRow, spawnCol, spawner);
                spawnerPaths[spawner].Add((firstPathRow, firstPathCol));
                
                if (firstPathRow != spawnRow)
                {
                    if (firstPathRow % 2 == 1)
                    {
                        direction = (direction == BubbleMoveDirection.Left) ? BubbleMoveDirection.Right : BubbleMoveDirection.Left;
                    }
                    firstBubbleState[spawner] = (firstPathRow, firstPathCol, direction, 0);
                }
                else
                {
                    firstBubbleState[spawner] = (firstPathRow, firstPathCol, direction, 1);
                }
            }
            else
            {
                var (nextRow, nextCol) = CalculateNextPositionForFirstBubble(spawnRow, spawnCol, spawner);

                spawnerPaths[spawner].Add((nextRow, nextCol));

                BubbleMoveDirection direction = spawner.IsLeftSpawner ?
                    BubbleMoveDirection.Left : BubbleMoveDirection.Right;

                firstBubbleState[spawner] = (nextRow, nextCol, direction, 1);

                hexMap.RegisterBubble(nextRow, nextCol, bubble, false);
                spawnerBubbles[spawner].Add((bubble, 0, nextRow, nextCol));

                StartCoroutine(MoveBubbleSmoothly(bubble, nextRow, nextCol));
            }
        }
        else
        {
            var (pathRow, pathCol) = spawnerPaths[spawner][0];
            
            hexMap.RegisterBubble(pathRow, pathCol, bubble, false);
            spawnerBubbles[spawner].Add((bubble, 0, pathRow, pathCol));
            
            StartCoroutine(MoveBubbleSmoothly(bubble, pathRow, pathCol));
        }
    }



    /// <summary>
    /// 랜덤 버블 타입 반환
    /// </summary>
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
        {
            return;
        }

        var bubbles = spawnerBubbles[spawner];
        var path = spawnerPaths[spawner];

        if (bubbles.Count >= maxBubblesPerSpawner)
        {
            return;
        }

        if (bubbles.Count > 0 && firstBubbleState.ContainsKey(spawner))
        {
            var (firstBubble, firstPathIndex, firstRow, firstCol) = bubbles[0];
            
            if (firstPathIndex == 0 && path.Count >= 1)
            {
                UpdateFirstBubblePath(spawner);
            }
            else if (firstPathIndex > 0)
            {
                UpdateFirstBubblePath(spawner);
            }
        }

        List<int> indicesToRemove = new List<int>();

        for (int i = 0; i < bubbles.Count; i++)
        {
            var (bubble, pathIndex, currentRow, currentCol) = bubbles[i];

            if (bubble == null || !bubble.gameObject.activeSelf)
            {
                if (currentRow >= 0 && currentCol >= 0)
                {
                    hexMap.UnregisterBubble(currentRow, currentCol);
                }
                indicesToRemove.Add(i);
                continue;
            }

            if (pathIndex == -1 && path.Count > 0)
            {
                var (firstRow, firstCol) = path[0];
                
                if (hexMap.IsEmpty(firstRow, firstCol))
                {
                    hexMap.RegisterBubble(firstRow, firstCol, bubble, false);
                    bubbles[i] = (bubble, 0, firstRow, firstCol);
                    StartCoroutine(MoveBubbleSmoothly(bubble, firstRow, firstCol));
                }
                else
                {
                    var emptyCells = HexMapHandler.GetEmptyAdjacentCells(hexMap, firstRow, firstCol);
                    var (emptyRow, emptyCol) = emptyCells.Count > 0 ? emptyCells[0] : (firstRow, firstCol);
                    hexMap.RegisterBubble(emptyRow, emptyCol, bubble, false);
                    bubbles[i] = (bubble, 0, emptyRow, emptyCol);
                    StartCoroutine(MoveBubbleSmoothly(bubble, emptyRow, emptyCol));
                }
                continue;
            }

            if (i == 0 && pathIndex == 0 && path.Count == 1)
            {
                var (path0Row, path0Col) = path[0];
                if (currentRow == path0Row && currentCol == path0Col)
                {
                    continue;
                }
            }

            int nextPathIndex = pathIndex + 1;

            if (nextPathIndex >= path.Count)
            {
                if (firstBubbleState.ContainsKey(spawner))
                {
                    UpdateFirstBubblePath(spawner);
                    
                    if (nextPathIndex < path.Count)
                    {
                        var (extendedNextRow, extendedNextCol) = path[nextPathIndex];
                        
                        if (currentRow >= 0 && currentCol >= 0)
                        {
                            hexMap.UnregisterBubble(currentRow, currentCol);
                        }
                        
                        if (hexMap.IsEmpty(extendedNextRow, extendedNextCol))
                        {
                            hexMap.RegisterBubble(extendedNextRow, extendedNextCol, bubble, false);
                            StartCoroutine(MoveBubbleSmoothly(bubble, extendedNextRow, extendedNextCol));
                            bubbles[i] = (bubble, nextPathIndex, extendedNextRow, extendedNextCol);
                        }
                        else
                        {
                            var emptyCells = HexMapHandler.GetEmptyAdjacentCells(hexMap, extendedNextRow, extendedNextCol);
                            var (emptyRow, emptyCol) = emptyCells.Count > 0 ? emptyCells[0] : (extendedNextRow, extendedNextCol);
                            hexMap.RegisterBubble(emptyRow, emptyCol, bubble, false);
                            StartCoroutine(MoveBubbleSmoothly(bubble, emptyRow, emptyCol));
                            bubbles[i] = (bubble, nextPathIndex, emptyRow, emptyCol);
                        }
                        continue;
                    }
                }
                
                if (currentRow >= 0 && currentCol >= 0)
                {
                    hexMap.UnregisterBubble(currentRow, currentCol);
                }

                if (path.Count > 0)
                {
                    var (lastRow, lastCol) = path[path.Count - 1];
                    PlaceBubbleOnHexMap(bubble, lastRow, lastCol);
                }
                indicesToRemove.Add(i);
                continue;
            }

            var (nextRow, nextCol) = path[nextPathIndex];

            if (currentRow >= 0 && currentCol >= 0)
            {
                hexMap.UnregisterBubble(currentRow, currentCol);
            }

            if (hexMap.IsEmpty(nextRow, nextCol))
            {
                hexMap.RegisterBubble(nextRow, nextCol, bubble, false);
                StartCoroutine(MoveBubbleSmoothly(bubble, nextRow, nextCol));
                bubbles[i] = (bubble, nextPathIndex, nextRow, nextCol);
            }
            else
            {
                var emptyCells = HexMapHandler.GetEmptyAdjacentCells(hexMap, nextRow, nextCol);
                var (emptyRow, emptyCol) = emptyCells.Count > 0 ? emptyCells[0] : (nextRow, nextCol);
                hexMap.RegisterBubble(emptyRow, emptyCol, bubble, false);
                StartCoroutine(MoveBubbleSmoothly(bubble, emptyRow, emptyCol));
                bubbles[i] = (bubble, nextPathIndex, emptyRow, emptyCol);
            }
        }

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
        {
            return;
        }

        var (currentRow, currentCol, direction, moveCount) = firstBubbleState[spawner];
        var path = spawnerPaths[spawner];

        int maxMoves = GetMoveCountForRow(currentRow);

        if (moveCount >= maxMoves)
        {
            var (downRow, downCol) = CalculateDownPosition(currentRow, currentCol, direction);

            if (hexMap.IsValidCell(downRow, downCol) && hexMap.IsEmpty(downRow, downCol))
            {
                path.Add((downRow, downCol));

                if (downRow % 2 == 1)
                {
                    direction = (direction == BubbleMoveDirection.Left) ? BubbleMoveDirection.Right : BubbleMoveDirection.Left;
                    firstBubbleState[spawner] = (downRow, downCol, direction, 1);
                }
                else
                {
                    firstBubbleState[spawner] = (downRow, downCol, direction, 1);
                }
            }
            else
            {
                firstBubbleState.Remove(spawner);
            }
        }
        else
        {
            var (nextRow, nextCol) = CalculateNextPositionInDirection(currentRow, currentCol, direction);

            if (hexMap.IsValidCell(nextRow, nextCol) && hexMap.IsEmpty(nextRow, nextCol))
            {
                path.Add((nextRow, nextCol));

                if (nextRow != currentRow)
                {
                    if (nextRow % 2 == 1)
                    {
                        direction = (direction == BubbleMoveDirection.Left) ? BubbleMoveDirection.Right : BubbleMoveDirection.Left;
                    }
                    firstBubbleState[spawner] = (nextRow, nextCol, direction, 0);
                }
                else
                {
                    firstBubbleState[spawner] = (nextRow, nextCol, direction, moveCount + 1);
                }
            }
            else
            {
                if (hexMap.IsValidCell(nextRow, nextCol))
                {
                    path.Add((nextRow, nextCol));
                    
                    if (nextRow != currentRow)
                    {
                        if (nextRow % 2 == 1)
                        {
                            direction = (direction == BubbleMoveDirection.Left) ? BubbleMoveDirection.Right : BubbleMoveDirection.Left;
                        }
                        firstBubbleState[spawner] = (nextRow, nextCol, direction, 0);
                    }
                    else
                    {
                        firstBubbleState[spawner] = (nextRow, nextCol, direction, moveCount + 1);
                    }
                }
                else
                {
                    var (downRow, downCol) = CalculateDownPosition(currentRow, currentCol, direction);

                    if (hexMap.IsValidCell(downRow, downCol) && hexMap.IsEmpty(downRow, downCol))
                    {
                        path.Add((downRow, downCol));
                    }
                    else if (hexMap.IsValidCell(downRow, downCol))
                    {
                        path.Add((downRow, downCol));
                    }
                    else
                    {
                        firstBubbleState.Remove(spawner);
                    }
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
            return 2;
        }
        else if (row % 2 == 1)
        {
            return 1;
        }
        else
        {
            return 4;
        }
    }


    /// <summary>
    /// 버블을 특정 위치로 부드럽게 이동
    /// </summary>
    private IEnumerator MoveBubbleSmoothly(Bubble bubble, int row, int col)
    {
        if (!hexMap.IsValidCell(row, col) || bubble == null)
            yield break;

        Vector3 targetPosition = hexMap.GetWorldPosition(row, col);
        Vector3 startPosition = bubble.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < moveSpeed && bubble != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveSpeed;

            bubble.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        if (bubble != null)
        {
            bubble.transform.position = targetPosition;
            bubble.SetHexPosition(row, col, targetPosition, hexMap);
            
            Collider2D col2d = bubble.GetComponent<Collider2D>();
            if (col2d != null && !col2d.enabled)
            {
                col2d.enabled = true;
            }
        }
    }

    /// <summary>
    /// 버블을 헥스맵에 고정 (이동 종료)
    /// </summary>
    private void PlaceBubbleOnHexMap(Bubble bubble, int row, int col)
    {
        if (!hexMap.IsValidCell(row, col))
            return;

        if (!hexMap.IsEmpty(row, col))
        {
            var emptyCells = HexMapHandler.GetEmptyAdjacentCells(hexMap, row, col);
            var (emptyRow, emptyCol) = emptyCells.Count > 0 ? emptyCells[0] : (row, col);
            row = emptyRow;
            col = emptyCol;
        }

        Vector3 targetPosition = hexMap.GetWorldPosition(row, col);
        bubble.transform.position = targetPosition;

        hexMap.RegisterBubble(row, col, bubble, false);
    }

    /// <summary>
    /// 버블 재생성 시작
    /// </summary>
    public void StartRefillBubbles()
    {
        if (isRefilling)
            return;

        StartCoroutine(RefillBubbles());
    }

#if UNITY_EDITOR
    /// <summary>
    /// 씬 뷰에서 경로를 시각화
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showPathGizmos || hexMap == null || spawnerPaths == null)
            return;

        Gizmos.color = pathColor;

        foreach (var kvp in spawnerPaths)
        {
            var spawner = kvp.Key;
            var path = kvp.Value;

            if (spawner == null || path == null || path.Count < 2)
                continue;

            for (int i = 0; i < path.Count - 1; i++)
            {
                var (row1, col1) = path[i];
                var (row2, col2) = path[i + 1];

                if (hexMap.IsValidCell(row1, col1) && hexMap.IsValidCell(row2, col2))
                {
                    Vector3 pos1 = hexMap.GetWorldPosition(row1, col1);
                    Vector3 pos2 = hexMap.GetWorldPosition(row2, col2);
                    
                    if (spawnerBubbles.ContainsKey(spawner) && spawnerBubbles[spawner].Count > 0)
                    {
                        var bubbles = spawnerBubbles[spawner];
                        for (int b = 0; b < bubbles.Count && b < path.Count; b++)
                        {
                            var (bubble, pathIdx, bubbleRow, bubbleCol) = bubbles[b];
                            if (bubble != null && bubbleRow == row1 && bubbleCol == col1)
                            {
                                Vector3 actualPos = bubble.transform.position;
                                float distance = Vector3.Distance(pos1, actualPos);
                                if (distance > 0.1f)
                                {
                                    Gizmos.color = Color.red;
                                    Gizmos.DrawWireSphere(actualPos, 0.15f);
                                    Gizmos.color = pathColor;
                                }
                            }
                        }
                    }

                    Gizmos.DrawLine(pos1, pos2);

                    Gizmos.DrawWireSphere(pos1, 0.1f);
                }
            }

            if (path.Count > 0)
            {
                var (lastRow, lastCol) = path[path.Count - 1];
                if (hexMap.IsValidCell(lastRow, lastCol))
                {
                    Vector3 lastPos = hexMap.GetWorldPosition(lastRow, lastCol);
                    Gizmos.DrawWireSphere(lastPos, 0.1f);
                }
            }
        }
    }
#endif
}
