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


    private bool isSpawning = false;
    private bool isRefilling = false; // 재생성 중인지 체크

    // 각 스포너별로 생성한 버블들을 저장 (버블, 경로 인덱스, 현재 row, col)
    private Dictionary<SpawnerPoint, List<(Bubble bubble, int pathIndex, int currentRow, int currentCol)>> spawnerBubbles =
        new Dictionary<SpawnerPoint, List<(Bubble, int, int, int)>>();

    // 각 스포너별 첫 번째 버블의 이동 경로 저장 (row, col)
    private Dictionary<SpawnerPoint, List<(int row, int col)>> spawnerPaths =
        new Dictionary<SpawnerPoint, List<(int, int)>>();

    // 각 스포너별 첫 번째 버블의 현재 상태
    private Dictionary<SpawnerPoint, (int currentRow, int currentCol, BubbleMoveDirection direction, int moveCountInRow)> firstBubbleState =
        new Dictionary<SpawnerPoint, (int, int, BubbleMoveDirection, int)>();

    void Start()
    {
        InitializeSpawners();
    }

    private void InitializeSpawners()
    {
        foreach (var spawner in spawnerPoints)
        {
            if (spawner != null)
            {
                spawner.UpdatePosition();
                // 각 스포너별 버블 리스트 초기화
                spawnerBubbles[spawner] = new List<(Bubble, int, int, int)>();
                // 각 스포너별 경로 초기화
                spawnerPaths[spawner] = new List<(int, int)>();
            }
        }
    }

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
        // 각 스포너별로 초기 버블 생성
        foreach (var spawner in spawnerPoints)
        {
            if (spawner != null)
            {
                StartCoroutine(SpawnBubblesRoutine(spawner));
            }
        }

        // 모든 스포너가 최대 버블 수에 도달할 때까지 대기
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
                // 모든 스포너가 최대 버블 수에 도달했으면 초기 배치 완료
                isSpawning = false;
                IngameManager.Instance.ChangeState(BattleState.Normal);
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

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
        // 이미 재생성 중이면 중복 호출 방지
        if (isRefilling)
            return;

        IngameManager.Instance.ChangeState(BattleState.RespawnBubbles);

        // Boss 애니메이션이 있으면 먼저 재생
        if (IngameManager.Instance.BossObj != null)
        {
            IngameManager.Instance.BossObj.SpawnBubbleForRefill();
        }
    }


    private IEnumerator RefillBubbles()
    {
        // 이미 재생성 중이면 중복 실행 방지
        if (isRefilling)
            yield break;

        isRefilling = true;

        // 파괴된 버블 제거
        RemoveDestroyedBubbles();

        // 각 스포너별로 필요한 버블 수 계산
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

        // 모든 스포너가 동시에 버블 생성 시작
        List<Coroutine> refillCoroutines = new List<Coroutine>();
        foreach (var kvp in neededCounts)
        {
            Coroutine coroutine = StartCoroutine(RefillSingleSpawner(kvp.Key, kvp.Value));
            refillCoroutines.Add(coroutine);
        }

        // 모든 스포너의 재생성이 완료될 때까지 대기
        foreach (var coroutine in refillCoroutines)
        {
            yield return coroutine;
        }

        // 재배치 완료 후 Reloading 상태로 전환 (장전 애니메이션을 위해)
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
            // 기존 버블 이동
            MoveSpawnerBubbles(spawner);
            
            // 버블 생성 및 배치
            CreateAndPlaceBubble(spawner);
            
            yield return new WaitForSeconds(0.1f); // 버블 생성 간격
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

                // 버블이 비활성화되었거나 null이면 제거
                if (bubble == null || !bubble.gameObject.activeSelf)
                {
                    // hexMap에서 해제
                    if (currentRow >= 0 && currentCol >= 0)
                    {
                        hexMap.UnregisterBubble(currentRow, currentCol);
                    }
                    indicesToRemove.Add(i);
                    continue;
                }

                // hexMap에 등록되지 않은 버블도 제거 (고립 버블이 떨어질 때)
                if (currentRow >= 0 && currentCol >= 0)
                {
                    Bubble registeredBubble = hexMap.GetBubble(currentRow, currentCol);
                    if (registeredBubble != bubble)
                    {
                        // hexMap에 해당 버블이 등록되어 있지 않으면 제거
                        indicesToRemove.Add(i);
                    }
                }
            }

            // 역순으로 제거
            for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                bubbles.RemoveAt(indicesToRemove[i]);
            }
        }
    }

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
        // 기존 버블 이동
        MoveSpawnerBubbles(spawner);

        // 버블 생성 및 배치
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

        // ① 무조건 스폰 포인트에서 보여주기 시작
        Vector3 spawnPosition = spawner.transform.position;
        bubble.transform.position = spawnPosition;

        // ② hexMap 좌표 계산
        var (spawnRow, spawnCol) = hexMap.WorldToGrid(spawnPosition);

        if (spawnerBubbles[spawner].Count == 0)
        {
            // 첫 번째 버블
            // 스폰 포인트 위치가 유효한 hexMap 셀인지 확인
            if (hexMap.IsValidCell(spawnRow, spawnCol) && hexMap.IsEmpty(spawnRow, spawnCol))
            {
                // 스폰 포인트 위치에 바로 등록
                hexMap.RegisterBubble(spawnRow, spawnCol, bubble, false);
                spawnerBubbles[spawner].Add((bubble, 0, spawnRow, spawnCol));

                BubbleMoveDirection direction = spawner.IsLeftSpawner ?
                    BubbleMoveDirection.Left : BubbleMoveDirection.Right;

                firstBubbleState[spawner] = (spawnRow, spawnCol, direction, 0);
                
                // 경로에 스폰 포인트 위치 추가
                spawnerPaths[spawner].Add((spawnRow, spawnCol));
            }
            else
            {
                // 스폰 포인트 위치가 유효하지 않거나 비어있지 않으면 다음 위치로 이동
                var (nextRow, nextCol) = CalculateNextPositionForFirstBubble(spawnRow, spawnCol, spawner);

                // 경로 등록
                spawnerPaths[spawner].Add((nextRow, nextCol));

                BubbleMoveDirection direction = spawner.IsLeftSpawner ?
                    BubbleMoveDirection.Left : BubbleMoveDirection.Right;

                firstBubbleState[spawner] = (nextRow, nextCol, direction, 1);

                // 첫 번째 버블은 생성 직후 hexMap에 등록 (pathIndex 0, 첫 목표 위치에 등록)
                hexMap.RegisterBubble(nextRow, nextCol, bubble, false);
                spawnerBubbles[spawner].Add((bubble, 0, nextRow, nextCol));

                // 스폰 포인트 → 첫 경로 칸 자연스럽게 이동
                StartCoroutine(MoveBubbleSmoothly(bubble, nextRow, nextCol));
            }
        }
        else
        {
            // 두 번째 이후 버블
            // 스폰 위치에 먼저 배치 (아직 hexMap에 등록하지 않음)
            // 첫 번째 버블이 이동한 후에 등록될 예정
            
            // spawnerBubbles에 추가하되, pathIndex는 -1로 설정하여 아직 등록되지 않았음을 표시
            spawnerBubbles[spawner].Add((bubble, -1, -1, -1));
            
            // 스폰 위치에서 대기 (이동은 MoveSpawnerBubbles에서 처리)
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
            var (bubble, pathIndex, currentRow, currentCol) = bubbles[i];

            // 버블이 비활성화되었거나 null이면 나중에 제거
            if (bubble == null || !bubble.gameObject.activeSelf)
            {
                // 이전 위치에서 hexMap 해제
                if (currentRow >= 0 && currentCol >= 0)
                {
                    hexMap.UnregisterBubble(currentRow, currentCol);
                }
                indicesToRemove.Add(i);
                continue;
            }

            // pathIndex가 -1이면 아직 등록되지 않은 새 버블 (스폰 위치에 있음)
            if (pathIndex == -1 && path.Count > 0)
            {
                // 첫 번째 목표 위치(path[0])에 등록
                var (firstRow, firstCol) = path[0];
                
                // 위치가 비어있는지 확인
                if (hexMap.IsEmpty(firstRow, firstCol))
                {
                    hexMap.RegisterBubble(firstRow, firstCol, bubble, false);
                    bubbles[i] = (bubble, 0, firstRow, firstCol);
                    StartCoroutine(MoveBubbleSmoothly(bubble, firstRow, firstCol));
                }
                else
                {
                    // 위치가 비어있지 않으면 다음 빈 위치 찾기
                    var (emptyRow, emptyCol) = hexMap.FindEmptyAdjacentCell(firstRow, firstCol, true);
                    hexMap.RegisterBubble(emptyRow, emptyCol, bubble, false);
                    bubbles[i] = (bubble, 0, emptyRow, emptyCol);
                    StartCoroutine(MoveBubbleSmoothly(bubble, emptyRow, emptyCol));
                }
                continue;
            }

            // pathIndex가 0이고 currentRow가 -1이면 첫 번째 목표 위치(path[0])에 등록
            if (pathIndex == 0 && currentRow == -1 && path.Count > 0)
            {
                var (firstRow, firstCol) = path[0];
                
                // 위치가 비어있는지 확인
                if (hexMap.IsEmpty(firstRow, firstCol))
                {
                    hexMap.RegisterBubble(firstRow, firstCol, bubble, false);
                    bubbles[i] = (bubble, pathIndex, firstRow, firstCol);
                    StartCoroutine(MoveBubbleSmoothly(bubble, firstRow, firstCol));
                }
                else
                {
                    // 위치가 비어있지 않으면 다음 빈 위치 찾기
                    var (emptyRow, emptyCol) = hexMap.FindEmptyAdjacentCell(firstRow, firstCol, true);
                    hexMap.RegisterBubble(emptyRow, emptyCol, bubble, false);
                    bubbles[i] = (bubble, pathIndex, emptyRow, emptyCol);
                    StartCoroutine(MoveBubbleSmoothly(bubble, emptyRow, emptyCol));
                }
                continue;
            }

            // 경로 인덱스 증가
            int nextPathIndex = pathIndex + 1;

            // 경로의 끝에 도달했는지 확인
            if (nextPathIndex >= path.Count)
            {
                // 이전 위치에서 hexMap 해제
                if (currentRow >= 0 && currentCol >= 0)
                {
                    hexMap.UnregisterBubble(currentRow, currentCol);
                }

                // 경로의 마지막 위치에 고정
                if (path.Count > 0)
                {
                    var (lastRow, lastCol) = path[path.Count - 1];
                    PlaceBubbleOnHexMap(bubble, lastRow, lastCol);
                }
                indicesToRemove.Add(i);
                continue;
            }

            // 다음 경로 위치로 이동 (부드럽게)
            var (nextRow, nextCol) = path[nextPathIndex];

            // 이전 위치에서 hexMap 해제
            if (currentRow >= 0 && currentCol >= 0)
            {
                hexMap.UnregisterBubble(currentRow, currentCol);
            }

            // 새 위치가 비어있는지 확인
            if (hexMap.IsEmpty(nextRow, nextCol))
            {
                // 새 위치에 hexMap 등록 (checkMatches = false, 이동 중이므로)
                hexMap.RegisterBubble(nextRow, nextCol, bubble, false);
                StartCoroutine(MoveBubbleSmoothly(bubble, nextRow, nextCol));
                bubbles[i] = (bubble, nextPathIndex, nextRow, nextCol);
            }
            else
            {
                // 위치가 비어있지 않으면 인접한 빈 위치 찾기
                var (emptyRow, emptyCol) = hexMap.FindEmptyAdjacentCell(nextRow, nextCol, true);
                hexMap.RegisterBubble(emptyRow, emptyCol, bubble, false);
                StartCoroutine(MoveBubbleSmoothly(bubble, emptyRow, emptyCol));
                bubbles[i] = (bubble, nextPathIndex, emptyRow, emptyCol);
            }
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

            if (hexMap.IsValidCell(downRow, downCol) && hexMap.IsEmpty(downRow, downCol))
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

            if (hexMap.IsValidCell(nextRow, nextCol) && hexMap.IsEmpty(nextRow, nextCol))
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

                if (hexMap.IsValidCell(downRow, downCol) && hexMap.IsEmpty(downRow, downCol))
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
    /// 버블을 특정 위치로 부드럽게 이동
    /// </summary>
    private IEnumerator MoveBubbleSmoothly(Bubble bubble, int row, int col)
    {
        if (!hexMap.IsValidCell(row, col) || bubble == null)
            yield break;

        Vector3 targetPosition = hexMap.Positions[row, col];
        Vector3 startPosition = bubble.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < moveSpeed && bubble != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveSpeed;

            // 부드러운 이동을 위한 Lerp
            bubble.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        // 최종 위치 보정 및 동기화
        if (bubble != null)
        {
            bubble.transform.position = targetPosition;
            // 이동 완료 후 hexMap 위치 동기화
            bubble.SetHexPosition(row, col, targetPosition, hexMap);
            
            // Collider가 활성화되어 있는지 확인
            Collider2D col2d = bubble.GetComponent<Collider2D>();
            if (col2d != null && !col2d.enabled)
            {
                col2d.enabled = true;
            }
        }
    }

    /// <summary>
    /// 버블을 특정 위치로 즉시 이동 (레거시 메서드)
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

        // 헥스맵에 등록 (checkMatches = true로 설정하여 매치 체크 실행)
        hexMap.RegisterBubble(row, col, bubble, true);
    }

    public void StartRefillBubbles()
    {
        // 이미 재생성 중이면 중복 호출 방지
        if (isRefilling)
            return;

        StartCoroutine(RefillBubbles());
    }
}
