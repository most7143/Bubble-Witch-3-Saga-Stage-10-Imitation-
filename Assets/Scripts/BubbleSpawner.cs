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
                // 각 스포너별 생성 순서 카운터 초기화
                spawnOrderCounters[spawner] = 0;
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
        Debug.Log($"[CreateAndPlaceBubble] {spawner.name}: 현재 버블 수={currentBubbleCount}");
        
        if (currentBubbleCount >= maxBubblesPerSpawner)
        {
            Debug.Log($"[CreateAndPlaceBubble] {spawner.name}: 최대 버블 수 도달");
            return;
        }

        BubbleTypes type = GetRandomBubbleType();
        Bubble bubble = objectPool.SpawnBubble(type);

        // 생성 순서 번호 할당
        if (!spawnOrderCounters.ContainsKey(spawner))
        {
            spawnOrderCounters[spawner] = 0;
        }
        int order = ++spawnOrderCounters[spawner];
        bubble.SetSpawnOrder(order);
        Debug.Log($"[CreateAndPlaceBubble] {spawner.name}: 버블 생성 순서={order}");

        Vector3 spawnPosition = spawner.transform.position;
        bubble.transform.position = spawnPosition;

        var (spawnRow, spawnCol) = hexMap.WorldToGrid(spawnPosition);
        Debug.Log($"[CreateAndPlaceBubble] {spawner.name}: 스폰 위치 계산 - ({spawnRow},{spawnCol})");

        if (spawnerBubbles[spawner].Count == 0)
        {
            Debug.Log($"[CreateAndPlaceBubble] {spawner.name}: 첫 번째 버블 생성");
            
            if (hexMap.IsValidCell(spawnRow, spawnCol) && hexMap.IsEmpty(spawnRow, spawnCol))
            {
                Debug.Log($"[CreateAndPlaceBubble] {spawner.name}: 스폰 위치에 바로 등록");
                hexMap.RegisterBubble(spawnRow, spawnCol, bubble, false);
                spawnerBubbles[spawner].Add((bubble, 0, spawnRow, spawnCol));

                BubbleMoveDirection direction = spawner.IsLeftSpawner ?
                    BubbleMoveDirection.Left : BubbleMoveDirection.Right;

                // 스폰 포인트 좌표는 경로에 추가하지 않음 (버블은 여기서 나오지만 경로에는 포함 안 함)
                // 첫 번째 이동 위치를 계산해서 경로에 추가
                var (firstPathRow, firstPathCol) = CalculateNextPositionForFirstBubble(spawnRow, spawnCol, spawner);
                spawnerPaths[spawner].Add((firstPathRow, firstPathCol));
                
                // 첫 번째 경로를 추가했으므로 이미 한 번 이동한 것으로 간주 (moveCount = 1)
                // 단, 행이 바뀌었는지 확인
                if (firstPathRow != spawnRow)
                {
                    // 행이 바뀌었으면 moveCount = 0 (새로운 행이므로)
                    if (firstPathRow % 2 == 1)
                    {
                        direction = (direction == BubbleMoveDirection.Left) ? BubbleMoveDirection.Right : BubbleMoveDirection.Left;
                    }
                    firstBubbleState[spawner] = (firstPathRow, firstPathCol, direction, 0);
                }
                else
                {
                    // 같은 행에서 이동했으면 moveCount = 1
                    firstBubbleState[spawner] = (firstPathRow, firstPathCol, direction, 1);
                }
                
                Debug.Log($"[CreateAndPlaceBubble] {spawner.name}: 첫 번째 버블 등록 완료 - pathIndex=0, pos=({spawnRow},{spawnCol}), 첫 경로 위치=({firstPathRow},{firstPathCol}), moveCount={firstBubbleState[spawner].moveCountInRow}");
            }
            else
            {
                var (nextRow, nextCol) = CalculateNextPositionForFirstBubble(spawnRow, spawnCol, spawner);
                Debug.Log($"[CreateAndPlaceBubble] {spawner.name}: 다음 위치로 이동 - ({nextRow},{nextCol})");

                spawnerPaths[spawner].Add((nextRow, nextCol));

                BubbleMoveDirection direction = spawner.IsLeftSpawner ?
                    BubbleMoveDirection.Left : BubbleMoveDirection.Right;

                firstBubbleState[spawner] = (nextRow, nextCol, direction, 1);

                hexMap.RegisterBubble(nextRow, nextCol, bubble, false);
                spawnerBubbles[spawner].Add((bubble, 0, nextRow, nextCol));
                Debug.Log($"[CreateAndPlaceBubble] {spawner.name}: 첫 번째 버블 등록 완료 - pathIndex=0, pos=({nextRow},{nextCol})");

                StartCoroutine(MoveBubbleSmoothly(bubble, nextRow, nextCol));
            }
        }
        else
        {
            Debug.Log($"[CreateAndPlaceBubble] {spawner.name}: 두 번째 이후 버블 생성 - 현재 버블 수={spawnerBubbles[spawner].Count}");
            // 두 번째 이후 버블
            // 첫 번째 경로 위치를 가져와서 부드럽게 이동
            var (pathRow, pathCol) = spawnerPaths[spawner][0];
            
            // 두 번째 버블도 생성 직후 hexMap에 등록 (pathIndex 0, 첫 목표 위치에 등록)
            hexMap.RegisterBubble(pathRow, pathCol, bubble, false);
            spawnerBubbles[spawner].Add((bubble, 0, pathRow, pathCol));
            
            // 부드럽게 이동
            StartCoroutine(MoveBubbleSmoothly(bubble, pathRow, pathCol));
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
        {
            Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블이 없음");
            return;
        }

        var bubbles = spawnerBubbles[spawner];
        var path = spawnerPaths[spawner];

        Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블 수={bubbles.Count}, 경로 길이={path.Count}");

        // 최대 버블 개수에 도달했으면 이동 중단
        if (bubbles.Count >= maxBubblesPerSpawner)
        {
            Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 최대 버블 수 도달");
            return;
        }

        // 첫 번째 버블의 경로 업데이트
        if (bubbles.Count > 0 && firstBubbleState.ContainsKey(spawner))
        {
            var (firstBubble, firstPathIndex, firstRow, firstCol) = bubbles[0];
            Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 첫 번째 버블 상태 - pathIndex={firstPathIndex}, pos=({firstRow},{firstCol}), 경로 길이={path.Count}");
            
            if (firstPathIndex == 0 && path.Count >= 1)
            {
                Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 첫 번째 버블 경로 업데이트 시작 (pathIndex=0, 경로 길이={path.Count})");
                UpdateFirstBubblePath(spawner);
                Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 첫 번째 버블 경로 업데이트 완료 - 경로 길이={path.Count}");
            }
            else if (firstPathIndex > 0)
            {
                Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 첫 번째 버블 경로 계속 업데이트 (pathIndex={firstPathIndex})");
                UpdateFirstBubblePath(spawner);
                Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 경로 업데이트 후 길이={path.Count}");
            }
            else
            {
                Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 첫 번째 버블 경로 업데이트 스킵 - pathIndex={firstPathIndex}, 경로 길이={path.Count}");
            }
        }

        // 제거할 인덱스들을 저장
        List<int> indicesToRemove = new List<int>();

        // 모든 버블을 경로를 따라 한 칸씩 이동
        for (int i = 0; i < bubbles.Count; i++)
        {
            var (bubble, pathIndex, currentRow, currentCol) = bubbles[i];

            Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] - pathIndex={pathIndex}, 현재 위치=({currentRow},{currentCol})");

            // 버블이 비활성화되었거나 null이면 나중에 제거
            if (bubble == null || !bubble.gameObject.activeSelf)
            {
                Debug.LogWarning($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 비활성화됨 - 제거 예정");
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
                var (firstRow, firstCol) = path[0];
                Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 새 버블 등록 시도 - path[0]=({firstRow},{firstCol})");
                
                if (hexMap.IsEmpty(firstRow, firstCol))
                {
                    Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] path[0]에 등록 성공");
                    hexMap.RegisterBubble(firstRow, firstCol, bubble, false);
                    bubbles[i] = (bubble, 0, firstRow, firstCol);
                    StartCoroutine(MoveBubbleSmoothly(bubble, firstRow, firstCol));
                }
                else
                {
                    Debug.LogWarning($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] path[0]=({firstRow},{firstCol})가 비어있지 않음 - 인접 빈 위치 찾기");
                    var (emptyRow, emptyCol) = hexMap.FindEmptyAdjacentCell(firstRow, firstCol, true);
                    Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 인접 빈 위치 찾음 - ({emptyRow},{emptyCol})");
                    hexMap.RegisterBubble(emptyRow, emptyCol, bubble, false);
                    bubbles[i] = (bubble, 0, emptyRow, emptyCol);
                    StartCoroutine(MoveBubbleSmoothly(bubble, emptyRow, emptyCol));
                }
                continue;
            }

            // 첫 번째 버블이 pathIndex 0에 있고 경로에 1개만 있으면 이동하지 않음
            if (i == 0 && pathIndex == 0 && path.Count == 1)
            {
                // 첫 번째 버블의 실제 위치와 경로[0]의 위치를 비교
                var (path0Row, path0Col) = path[0];
                if (currentRow == path0Row && currentCol == path0Col)
                {
                    // 실제 위치가 경로[0]과 같으면 이동 스킵
                    Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 첫 번째 버블 이동 스킵 - pathIndex=0, 경로 길이=1, 위치 일치");
                    continue;
                }
                else
                {
                    // 실제 위치가 경로[0]과 다르면 경로[0]으로 이동
                    Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 첫 번째 버블 위치 불일치 - 현재=({currentRow},{currentCol}), 경로[0]=({path0Row},{path0Col}), 이동 필요");
                    // 아래 이동 로직으로 진행
                }
            }

            // 경로 인덱스 증가
            int nextPathIndex = pathIndex + 1;
            Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 다음 경로 인덱스 계산 - 현재={pathIndex}, 다음={nextPathIndex}, 경로 길이={path.Count}");

            // 경로의 끝에 도달했는지 확인
            if (nextPathIndex >= path.Count)
            {
                Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 경로 끝 도달 - nextPathIndex={nextPathIndex} >= 경로 길이={path.Count}");
                
                // 경로의 끝에 도달했지만, 아직 이동할 버블이 더 있으면 경로를 확장해야 함
                // 첫 번째 버블이 아직 경로를 생성 중이면 경로를 확장
                // 또는 첫 번째 버블이 아니더라도 경로를 확장할 수 있으면 확장
                if (firstBubbleState.ContainsKey(spawner))
                {
                    Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 경로 끝 도달 - 경로 확장 시도");
                    UpdateFirstBubblePath(spawner);
                    
                    // 경로가 확장되었는지 확인
                    if (nextPathIndex < path.Count)
                    {
                        // 경로가 확장되었으면 계속 이동
                        var (extendedNextRow, extendedNextCol) = path[nextPathIndex];
                        Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 경로 확장됨 - 다음 위치 path[{nextPathIndex}]=({extendedNextRow},{extendedNextCol})");
                        
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
                            Debug.LogWarning($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 확장된 경로 위치 ({extendedNextRow},{extendedNextCol})가 비어있지 않음 - 인접 빈 위치 찾기");
                            var (emptyRow, emptyCol) = hexMap.FindEmptyAdjacentCell(extendedNextRow, extendedNextCol, true);
                            hexMap.RegisterBubble(emptyRow, emptyCol, bubble, false);
                            StartCoroutine(MoveBubbleSmoothly(bubble, emptyRow, emptyCol));
                            bubbles[i] = (bubble, nextPathIndex, emptyRow, emptyCol);
                        }
                        continue;
                    }
                    else
                    {
                        Debug.LogWarning($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 경로 확장 실패 - nextPathIndex={nextPathIndex}, 경로 길이={path.Count}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 경로 확장 불가 - firstBubbleState 없음");
                }
                
                // 경로 확장 실패 또는 더 이상 확장할 수 없으면 마지막 위치에 고정
                if (currentRow >= 0 && currentCol >= 0)
                {
                    hexMap.UnregisterBubble(currentRow, currentCol);
                }

                if (path.Count > 0)
                {
                    var (lastRow, lastCol) = path[path.Count - 1];
                    Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 마지막 위치에 고정 - ({lastRow},{lastCol})");
                    PlaceBubbleOnHexMap(bubble, lastRow, lastCol);
                }
                else
                {
                    Debug.LogError($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 경로가 비어있음!");
                }
                indicesToRemove.Add(i);
                continue;
            }

            // 다음 경로 위치로 이동 (부드럽게)
            var (nextRow, nextCol) = path[nextPathIndex];
            Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 다음 위치 - path[{nextPathIndex}]=({nextRow},{nextCol})");

            if (currentRow >= 0 && currentCol >= 0)
            {
                hexMap.UnregisterBubble(currentRow, currentCol);
            }

            if (hexMap.IsEmpty(nextRow, nextCol))
            {
                Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 이동 성공 - ({currentRow},{currentCol}) -> ({nextRow},{nextCol})");
                hexMap.RegisterBubble(nextRow, nextCol, bubble, false);
                StartCoroutine(MoveBubbleSmoothly(bubble, nextRow, nextCol));
                bubbles[i] = (bubble, nextPathIndex, nextRow, nextCol);
            }
            else
            {
                Debug.LogWarning($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 다음 위치 ({nextRow},{nextCol})가 비어있지 않음 - 인접 빈 위치 찾기");
                var (emptyRow, emptyCol) = hexMap.FindEmptyAdjacentCell(nextRow, nextCol, true);
                Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블[{i}] 인접 빈 위치 찾음 - ({emptyRow},{emptyCol})");
                hexMap.RegisterBubble(emptyRow, emptyCol, bubble, false);
                StartCoroutine(MoveBubbleSmoothly(bubble, emptyRow, emptyCol));
                bubbles[i] = (bubble, nextPathIndex, emptyRow, emptyCol);
            }
        }

        // 역순으로 제거하여 인덱스 문제 방지
        for (int i = indicesToRemove.Count - 1; i >= 0; i--)
        {
            Debug.Log($"[MoveSpawnerBubbles] {spawner.name}: 버블[{indicesToRemove[i]}] 제거");
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
            Debug.LogWarning($"[UpdateFirstBubblePath] {spawner.name}: firstBubbleState 없음");
            return;
        }

        var (currentRow, currentCol, direction, moveCount) = firstBubbleState[spawner];
        var path = spawnerPaths[spawner];

        Debug.Log($"[UpdateFirstBubblePath] {spawner.name}: 현재 상태 - pos=({currentRow},{currentCol}), direction={direction}, moveCount={moveCount}, 경로 길이={path.Count}");

        int maxMoves = GetMoveCountForRow(currentRow);
        Debug.Log($"[UpdateFirstBubblePath] {spawner.name}: 행 {currentRow}의 최대 이동 횟수={maxMoves}");

        if (moveCount >= maxMoves)
        {
            Debug.Log($"[UpdateFirstBubblePath] {spawner.name}: 이동 횟수 초과 - 아래로 이동 시도");
            var (downRow, downCol) = CalculateDownPosition(currentRow, currentCol, direction);

            if (hexMap.IsValidCell(downRow, downCol) && hexMap.IsEmpty(downRow, downCol))
            {
                Debug.Log($"[UpdateFirstBubblePath] {spawner.name}: 아래로 이동 성공 - ({downRow},{downCol})");
                path.Add((downRow, downCol));

                if (downRow % 2 == 1)
                {
                    direction = (direction == BubbleMoveDirection.Left) ? BubbleMoveDirection.Right : BubbleMoveDirection.Left;
                    firstBubbleState[spawner] = (downRow, downCol, direction, 1);
                    Debug.Log($"[UpdateFirstBubblePath] {spawner.name}: 홀수행 도착 - 방향 변경, moveCount=1");
                }
                else
                {
                    firstBubbleState[spawner] = (downRow, downCol, direction, 1);
                    Debug.Log($"[UpdateFirstBubblePath] {spawner.name}: 아래로 이동 완료 - moveCount=1");
                }
            }
            else
            {
                Debug.LogWarning($"[UpdateFirstBubblePath] {spawner.name}: 아래로 이동 실패 - 유효하지 않거나 비어있지 않음 ({downRow},{downCol})");
                firstBubbleState.Remove(spawner);
            }
        }
        else
        {
            var (nextRow, nextCol) = CalculateNextPositionInDirection(currentRow, currentCol, direction);
            Debug.Log($"[UpdateFirstBubblePath] {spawner.name}: 현재 방향으로 이동 시도 - ({nextRow},{nextCol})");

            if (hexMap.IsValidCell(nextRow, nextCol) && hexMap.IsEmpty(nextRow, nextCol))
            {
                Debug.Log($"[UpdateFirstBubblePath] {spawner.name}: 이동 성공 - 경로에 추가");
                path.Add((nextRow, nextCol));

                if (nextRow != currentRow)
                {
                    Debug.Log($"[UpdateFirstBubblePath] {spawner.name}: 행 변경 - {currentRow} -> {nextRow}");
                    if (nextRow % 2 == 1)
                    {
                        direction = (direction == BubbleMoveDirection.Left) ? BubbleMoveDirection.Right : BubbleMoveDirection.Left;
                        Debug.Log($"[UpdateFirstBubblePath] {spawner.name}: 홀수행 도착 - 방향 변경");
                    }
                    firstBubbleState[spawner] = (nextRow, nextCol, direction, 0);
                    Debug.Log($"[UpdateFirstBubblePath] {spawner.name}: 행 변경 완료 - moveCount=0");
                }
                else
                {
                    firstBubbleState[spawner] = (nextRow, nextCol, direction, moveCount + 1);
                    Debug.Log($"[UpdateFirstBubblePath] {spawner.name}: 같은 행 이동 - moveCount={moveCount + 1}");
                }
            }
            else
            {
                // 위치가 비어있지 않아도 경로에 추가 (버블이 이동 중일 수 있음)
                if (hexMap.IsValidCell(nextRow, nextCol))
                {
                    Debug.LogWarning($"[UpdateFirstBubblePath] {spawner.name}: 위치 ({nextRow},{nextCol})가 비어있지 않지만 경로에 추가 (버블 이동 중일 수 있음)");
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
                    // 유효하지 않은 위치면 아래로 이동 시도
                    Debug.LogWarning($"[UpdateFirstBubblePath] {spawner.name}: 이동 실패 - 유효하지 않음 ({nextRow},{nextCol}), 아래로 이동 시도");
                    var (downRow, downCol) = CalculateDownPosition(currentRow, currentCol, direction);

                    if (hexMap.IsValidCell(downRow, downCol) && hexMap.IsEmpty(downRow, downCol))
                    {
                        Debug.Log($"[UpdateFirstBubblePath] {spawner.name}: 아래로 이동 성공 - ({downRow},{downCol})");
                        path.Add((downRow, downCol));
                        // ... existing code ...
                    }
                    else if (hexMap.IsValidCell(downRow, downCol))
                    {
                        // 아래 위치도 비어있지 않아도 경로에 추가
                        Debug.LogWarning($"[UpdateFirstBubblePath] {spawner.name}: 아래 위치 ({downRow},{downCol})가 비어있지 않지만 경로에 추가");
                        path.Add((downRow, downCol));
                        // ... existing code ...
                    }
                    else
                    {
                        Debug.LogError($"[UpdateFirstBubblePath] {spawner.name}: 아래로 이동도 실패 - 경로 생성 종료");
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

        // 헥스맵에 등록 (생성 중이므로 checkMatches = false로 설정하여 매치 체크 실행 안 함)
        hexMap.RegisterBubble(row, col, bubble, false);
    }

    public void StartRefillBubbles()
    {
        // 이미 재생성 중이면 중복 호출 방지
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

            // 경로의 각 점을 선으로 연결
            for (int i = 0; i < path.Count - 1; i++)
            {
                var (row1, col1) = path[i];
                var (row2, col2) = path[i + 1];

                if (hexMap.IsValidCell(row1, col1) && hexMap.IsValidCell(row2, col2))
                {
                    Vector3 pos1 = hexMap.Positions[row1, col1];
                    Vector3 pos2 = hexMap.Positions[row2, col2];

                    // 선 그리기
                    Gizmos.DrawLine(pos1, pos2);

                    // 각 경로 점에 작은 구체 표시
                    Gizmos.DrawWireSphere(pos1, 0.1f);
                }
            }

            // 마지막 점도 표시
            if (path.Count > 0)
            {
                var (lastRow, lastCol) = path[path.Count - 1];
                if (hexMap.IsValidCell(lastRow, lastCol))
                {
                    Vector3 lastPos = hexMap.Positions[lastRow, lastCol];
                    Gizmos.DrawWireSphere(lastPos, 0.1f);
                }
            }
        }
    }
#endif
}
