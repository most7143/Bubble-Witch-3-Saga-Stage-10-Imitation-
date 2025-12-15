using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public class HexMapBubbleDestroy : MonoBehaviour
{
    private HexMap hexMapController;
    private BubbleSpawner bubbleSpawner;
    private readonly List<Vector3> pendingNeroTargetsWorld = new List<Vector3>();
    private const int NeroRange = 2;

    [Header("Drop FX")]
    public RectTransform DropPoint;
    [SerializeField] private float dropSuctionDuration = 0.7f;
    [SerializeField] private float dropSuctionTurns = 2.5f;
    [SerializeField] private Ease dropSuctionEase = Ease.InCubic;
    [SerializeField] [Range(0.05f, 1f)] private float dropSuctionVerticalScale = 0.35f;
    [SerializeField] private float dropSuctionTiltDegrees = 90f;
    [SerializeField] [Range(0.1f, 2f)] private float dropSuctionRadiusScale = 1f;

    /// <summary>
    /// HexMap 초기화
    /// </summary>
    public void Initialize(HexMap hexMap)
    {
        hexMapController = hexMap;
        bubbleSpawner = FindObjectOfType<BubbleSpawner>();
    }

    /// <summary>
    /// Nero 버블 타겟 위치 설정
    /// </summary>
    public void SetPendingNeroTargets(List<Vector3> targets)
    {
        pendingNeroTargetsWorld.Clear();
        if (targets == null)
            return;

        foreach (var pos in targets)
        {
            pendingNeroTargetsWorld.Add(pos);
        }
    }

    /// <summary>
    /// Nero 버블 타겟 위치 초기화
    /// </summary>
    public void ClearPendingNeroTargets()
    {
        pendingNeroTargetsWorld.Clear();
    }

    /// <summary>
    /// 착지 후 호출: 동일 타입 3개 이상이면 레벨별 전파식으로 팝
    /// </summary>
    public void CheckAndPopMatches(int row, int col)
    {
        if (IngameManager.Instance != null && 
            IngameManager.Instance.CurrentState == BattleState.RespawnBubbles)
        {
            return;
        }

        Bubble center = hexMapController.GetBubble(row, col);
        if (center == null) return;

        BubbleTypes attackerType = center.BubbleType;

        if (center.BubbleType == BubbleTypes.Nero)
        {
            HandleNeroBubble(row, col);
            return;
        }

        if (center.BubbleType == BubbleTypes.Spell)
        {
            PopSpellBubble(row, col);
            return;
        }

        List<(int row, int col)> adjacentCells = hexMapController.GetAdjacentCells(row, col);
        List<(int row, int col)> adjacentSpellBubbles = new List<(int row, int col)>();
        
        foreach (var (adjRow, adjCol) in adjacentCells)
        {
            if (hexMapController.IsValidCell(adjRow, adjCol))
            {
                Bubble adjBubble = hexMapController.GetBubble(adjRow, adjCol);
                if (adjBubble != null && adjBubble.BubbleType == BubbleTypes.Spell)
                {
                    adjacentSpellBubbles.Add((adjRow, adjCol));
                }
            }
        }
        
        foreach (var (spellRow, spellCol) in adjacentSpellBubbles)
        {
            PopSpellBubble(spellRow, spellCol);
        }
        
        if (adjacentSpellBubbles.Count > 0)
        {
            return;
        }

        List<List<(int r, int c)>> levels = hexMapController.HexMapHandler.CollectConnectedByLevel(row, col);
        if (levels.Count == 0) return;

        int totalCount = 0;
        foreach (var level in levels)
            totalCount += level.Count;

        if (totalCount < 3)
        {
            if (IngameManager.Instance != null)
            {
                IngameManager.Instance.BubbleDestroyFail();
            }
            
            if (IngameManager.Instance.CurrentState == BattleState.Shooting)
            {
                IngameManager.Instance.ChangeState(BattleState.Reloading);
            }
            return;
        }

        bool hasFairy = false;
        HashSet<(int r, int c)> toRemove = new HashSet<(int r, int c)>();
        foreach (var level in levels)
        {
            foreach (var (r, c) in level)
            {
                toRemove.Add((r, c));
                Bubble b = hexMapController.GetBubble(r, c);
                if (b != null && b.IsFairy)
                {
                    hasFairy = true;
                }
            }
        }

        if (hasFairy)
        {
            IngameManager.Instance.ChangeState(BattleState.Hitting);
        }
        else
        {
            IngameManager.Instance.ChangeState(BattleState.Destroying);
        }

        List<(int r, int c)> floatingBubbles = hexMapController.HexMapHandler.FindFloatingBubblesAfterRemoval(toRemove);

        DestroyLevelImmediate(levels[0], attackerType);

        if (levels.Count > 1)
        {
            List<List<(int r, int c)>> remainingLevels = levels.GetRange(1, levels.Count - 1);
            StartCoroutine(DestroyByWaveAndNotify(remainingLevels, floatingBubbles, hasFairy, attackerType));
        }
        else
        {
            if (floatingBubbles.Count > 0)
            {
                StartCoroutine(RemoveFloatingBubblesAndNotify(floatingBubbles, hasFairy));
            }
            else
            {
                if (IngameManager.Instance != null)
                {
                    IngameManager.Instance.BubbleDestroySuccess();
                }
                
                if (hasFairy)
                {
                }
                else
                {
                    if (IngameManager.Instance.CurrentState == BattleState.Destroying)
                    {
                        IngameManager.Instance.ChangeState(BattleState.Reloading);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Nero 버블 처리
    /// </summary>
    private void HandleNeroBubble(int row, int col)
    {
        HashSet<(int r, int c)> targetCells = BuildNeroTargetCells(row, col);
        if (targetCells.Count == 0)
        {
            ClearPendingNeroTargets();
            if (IngameManager.Instance.CurrentState == BattleState.Shooting)
            {
                IngameManager.Instance.ChangeState(BattleState.Reloading);
            }
            return;
        }

        List<(int r, int c)> existingCells = new List<(int r, int c)>();
        bool hasFairy = false;

        foreach (var (r, c) in targetCells)
        {
            Bubble bubble = hexMapController.GetBubble(r, c);
            if (bubble == null) continue;

            existingCells.Add((r, c));
            if (bubble.IsFairy)
            {
                hasFairy = true;
            }
        }

        if (existingCells.Count == 0)
        {
            ClearPendingNeroTargets();
            if (IngameManager.Instance.CurrentState == BattleState.Shooting)
            {
                IngameManager.Instance.ChangeState(BattleState.Reloading);
            }
            return;
        }

        if (hasFairy)
        {
            IngameManager.Instance.ChangeState(BattleState.Hitting);
        }
        else
        {
            IngameManager.Instance.ChangeState(BattleState.Destroying);
        }

        // 네로 버블 타입을 attackerType으로 전달
        DestroyLevelImmediate(existingCells, BubbleTypes.Nero);

        HashSet<(int r, int c)> removedSet = new HashSet<(int r, int c)>(existingCells);
        List<(int r, int c)> floatingBubbles = hexMapController.HexMapHandler.FindFloatingBubblesAfterRemoval(removedSet);

        if (floatingBubbles.Count > 0)
        {
            StartCoroutine(RemoveFloatingBubblesAndNotify(floatingBubbles, hasFairy));
        }
        else
        {
            // 파괴 성공 처리 (콤보 카운트 증가)
            if (IngameManager.Instance != null)
            {
                IngameManager.Instance.BubbleDestroySuccess();
            }
            
            if (!hasFairy)
            {
                if (bubbleSpawner != null)
                {
                    bubbleSpawner.OnBubblesDestroyed();
                }
                else if (IngameManager.Instance.CurrentState == BattleState.Destroying)
                {
                    IngameManager.Instance.ChangeState(BattleState.Reloading);
                }
            }
        }

        ClearPendingNeroTargets();
    }

    private HashSet<(int r, int c)> BuildNeroTargetCells(int row, int col)
    {
        HashSet<(int r, int c)> targets = ConvertWorldTargetsToCells(pendingNeroTargetsWorld);
        if (targets.Count == 0)
        {
            targets = CollectCellsWithinRange(row, col, NeroRange);
        }

        if (hexMapController.IsValidCell(row, col))
        {
            targets.Add((row, col));
        }

        return targets;
    }

    private HashSet<(int r, int c)> ConvertWorldTargetsToCells(List<Vector3> worldPositions)
    {
        HashSet<(int r, int c)> result = new HashSet<(int r, int c)>();
        if (hexMapController == null || worldPositions == null)
            return result;

        foreach (var pos in worldPositions)
        {
            var (r, c) = hexMapController.WorldToGrid(pos);
            if (hexMapController.IsValidCell(r, c))
            {
                result.Add((r, c));
            }
        }

        return result;
    }

    private HashSet<(int r, int c)> CollectCellsWithinRange(int startRow, int startCol, int range)
    {
        HashSet<(int r, int c)> result = new HashSet<(int r, int c)>();
        if (hexMapController == null || !hexMapController.IsValidCell(startRow, startCol) || range < 0)
            return result;

        Queue<(int row, int col, int dist)> queue = new Queue<(int, int, int)>();
        queue.Enqueue((startRow, startCol, 0));
        result.Add((startRow, startCol));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.dist >= range)
                continue;

            List<(int row, int col)> adjacentCells = hexMapController.GetAdjacentCells(current.row, current.col);
            foreach (var (adjRow, adjCol) in adjacentCells)
            {
                if (!result.Contains((adjRow, adjCol)))
                {
                    result.Add((adjRow, adjCol));
                    queue.Enqueue((adjRow, adjCol, current.dist + 1));
                }
            }
        }

        return result;
    }

    // 클래스 멤버 변수 추가 (파일 상단)
    private HashSet<(int r, int c)> processingSpellBubbles = new HashSet<(int r, int c)>();

    /// <summary>
    /// Spell 타입 버블: 자신을 포함한 인접 1칸 이내의 모든 버블 터트리기
    /// </summary>
    private void PopSpellBubble(int row, int col)
    {
        // 이미 처리 중인 Spell 버블이면 무시 (무한 재귀 방지)
        if (processingSpellBubbles.Contains((row, col)))
            return;
        
        processingSpellBubbles.Add((row, col));

        // 파괴를 일으킨 버블 정보를 먼저 저장 (파괴되기 전에)
        Bubble spellBubble = hexMapController.GetBubble(row, col);
        BubbleTypes attackerType = BubbleTypes.Red; // 기본값
        
        if (spellBubble != null)
        {
            attackerType = spellBubble.BubbleType;
        }

        // 터질 버블 리스트 (자신 포함)
        HashSet<(int r, int c)> toRemove = new HashSet<(int r, int c)>();
        toRemove.Add((row, col));

        // 인접 셀 가져오기
        List<(int row, int col)> adjacentCells = hexMapController.GetAdjacentCells(row, col);
        
        foreach (var (adjRow, adjCol) in adjacentCells)
        {
            // 유효한 셀이고 버블이 있으면 추가
            if (hexMapController.IsValidCell(adjRow, adjCol))
            {
                Bubble adjBubble = hexMapController.GetBubble(adjRow, adjCol);
                if (adjBubble != null)
                {
                    toRemove.Add((adjRow, adjCol));
                }
            }
        }

        // 터질 버블이 없으면 Reloading 상태로 전환
        if (toRemove.Count == 0)
        {
            processingSpellBubbles.Remove((row, col));
            if (IngameManager.Instance.CurrentState == BattleState.Shooting)
            {
                IngameManager.Instance.ChangeState(BattleState.Reloading);
            }
            return;
        }

        // 폭발하는 버블들 중 페어리가 있는지 체크
        bool hasFairy = false;
        foreach (var (r, c) in toRemove)
        {
            Bubble b = hexMapController.GetBubble(r, c);
            if (b != null && b.IsFairy)
            {
                hasFairy = true;
            }
        }

        // 페어리가 있으면 Hitting 상태로 전환, 없으면 Destroying 상태로 전환
        if (hasFairy)
        {
            IngameManager.Instance.ChangeState(BattleState.Hitting);
        }
        else
        {
            IngameManager.Instance.ChangeState(BattleState.Destroying);
        }

        // 고립 버블 미리 계산
        List<(int r, int c)> floatingBubbles = hexMapController.HexMapHandler.FindFloatingBubblesAfterRemoval(toRemove);

        // Spell 버블은 레벨별 전파 없이 모두 즉시 파괴
        List<(int r, int c)> spellBubbles = new List<(int r, int c)>(toRemove);
        
        // 파괴를 일으킨 버블 타입 (이미 저장된 값 사용)
        // attackerType은 위에서 이미 저장됨
        
        // 첫 번째 버블 즉시 파괴
        if (spellBubbles.Count > 0)
        {
            DestroyLevelImmediate(new List<(int r, int c)> { spellBubbles[0] }, attackerType);
        }

        // 나머지 버블들도 파괴 (딜레이 없이)
        if (spellBubbles.Count > 1)
        {
            List<(int r, int c)> remainingBubbles = spellBubbles.GetRange(1, spellBubbles.Count - 1);
            StartCoroutine(DestroySpellBubblesAndNotify(remainingBubbles, floatingBubbles, hasFairy, attackerType));
        }
        else
        {
            // 버블이 1개뿐이면 고립 버블만 처리
            if (floatingBubbles.Count > 0)
            {
                StartCoroutine(RemoveFloatingBubblesAndNotify(floatingBubbles, hasFairy));
            }
            else
            {
                // 고립 버블이 없으면 파괴 성공 처리
                if (IngameManager.Instance != null)
                {
                    IngameManager.Instance.BubbleDestroySuccess();
                }
                
                // 고립 버블이 없으면
                if (hasFairy)
                {
                    // 페어리가 있으면 Hitting 상태는 유지 (체력 연출 후 Reloading으로 전환됨)
                }
                else
                {
                    // 페어리가 없으면 재배치를 위해 OnBubblesDestroyed 호출
                    if (bubbleSpawner != null)
                    {
                        bubbleSpawner.OnBubblesDestroyed();
                    }
                }
            }
        }
        
        // 처리 완료 후 제거
        processingSpellBubbles.Remove((row, col));
    }

    /// <summary>
    /// Spell 버블의 나머지 버블들을 파괴하고 알림
    /// </summary>
    private IEnumerator DestroySpellBubblesAndNotify(List<(int r, int c)> remainingBubbles, List<(int r, int c)> floatingBubbles, bool hasFairy, BubbleTypes attackerType = BubbleTypes.Spell)
    {
        // 나머지 버블들을 즉시 파괴 (딜레이 없이)
        foreach (var (r, c) in remainingBubbles)
        {
            Bubble b = hexMapController.GetBubble(r, c);
            if (b == null) continue;

            // 요정이 있으면 페어리 오브젝트 생성
            if (b.IsFairy)
            {
                SpawnFairyAtPosition(b.transform.position);
            }

            // 파괴되기 직전에 점수 추가
            b.AttackedDestoryBubble(attackerType);

            if (b.Anim != null)
                b.DestroyBubble();

            hexMapController.UnregisterBubble(r, c);
            hexMapController.ObjectPool?.DespawnBubble(b);
        }

        // 고립 버블 제거
        if (floatingBubbles.Count > 0)
        {
            yield return StartCoroutine(RemoveFloatingBubblesSpread(floatingBubbles));
            
            // 떨어진 버블들이 완전히 삭제될 때까지 대기
            yield return new WaitForSeconds(3f);
        }
        
        // 페어리가 있으면 Hitting 상태는 유지 (체력 연출 후 Reloading으로 전환됨)
        // 페어리가 없으면 재배치를 위해 OnBubblesDestroyed 호출
        if (!hasFairy && bubbleSpawner != null)
        {
            bubbleSpawner.OnBubblesDestroyed();
        }
    }

    /// <summary>
    /// 첫 레벨 즉시 파괴 (딜레이 없음)
    /// </summary>
    private void DestroyLevelImmediate(List<(int r, int c)> level, BubbleTypes attackerType = BubbleTypes.Red)
    {
        foreach (var (r, c) in level)
        {
            Bubble b = hexMapController.GetBubble(r, c);
            if (b == null) continue;

            // 요정이 있으면 페어리 오브젝트 생성
            if (b.IsFairy)
            {
                SpawnFairyAtPosition(b.transform.position);
            }

            // 파괴되기 직전에 점수 추가
            b.AttackedDestoryBubble(attackerType);

            if (b.Anim != null)
                b.DestroyBubble();  // 애니메이션 재생

            hexMapController.UnregisterBubble(r, c);
            hexMapController.ObjectPool?.DespawnBubble(b);
        }
    }

    /// <summary>
    /// 단계별 파동 처리
    /// </summary>
    private IEnumerator DestroyByWaveAndNotify(List<List<(int r, int c)>> levels, List<(int r, int c)> preCalculatedFloatingBubbles, bool hasFairy, BubbleTypes attackerType = BubbleTypes.Red)
{
    float delayPerLevel = 0.08f;
    
    // 모든 파괴 예정 버블 수집
    HashSet<(int r, int c)> allToDestroy = new HashSet<(int r, int c)>();
    foreach (var level in levels)
    {
        foreach (var (r, c) in level)
        {
            allToDestroy.Add((r, c));
        }
    }

    for (int lv = 0; lv < levels.Count; lv++)
    {
        List<(int r, int c)> group = levels[lv];

        foreach (var (r, c) in group)
        {
            Bubble b = hexMapController.GetBubble(r, c);
            if (b == null) continue;

            // 요정이 있으면 페어리 오브젝트 생성
            if (b.IsFairy)
            {
                SpawnFairyAtPosition(b.transform.position);
            }

            // 파괴되기 직전에 점수 추가
            b.AttackedDestoryBubble(attackerType);

            if (b.Anim != null)
                b.DestroyBubble();

            hexMapController.UnregisterBubble(r, c);
            hexMapController.ObjectPool?.DespawnBubble(b);
        }

        yield return new WaitForSeconds(delayPerLevel);
    }
    
    // 모든 파괴가 끝난 후 고립 버블을 다시 체크 (가장 마지막에 확인)
    List<(int r, int c)> floatingBubbles = FindFloatingBubbles();
    
    // 고립 버블이 있으면 떨어뜨리기
    if (floatingBubbles.Count > 0)
    {
        yield return StartCoroutine(RemoveFloatingBubblesSpread(floatingBubbles));
        
        // 떨어진 버블들이 완전히 삭제될 때까지 대기
        yield return new WaitForSeconds(3f);
    }
    
    // 파괴 성공 처리 (콤보 카운트 증가)
    if (IngameManager.Instance != null)
    {
        IngameManager.Instance.BubbleDestroySuccess();
    }
    
    // 페어리가 있으면 Hitting 상태는 유지 (체력 연출 후 Reloading으로 전환됨)
    // 페어리가 없으면 재배치를 위해 OnBubblesDestroyed 호출
    if (!hasFairy && bubbleSpawner != null)
    {
        bubbleSpawner.OnBubblesDestroyed();
    }
}

    /// <summary>
    /// 같은 타입의 연결된 버블 찾기 (HexMapHandler 사용)
    /// </summary>
    public List<(int row, int col)> FindConnectedSameType(int row, int col)
    {
        return hexMapController.HexMapHandler.FindConnectedSameType(row, col);
    }

    /// <summary>
    /// 고립 버블 체크 및 제거
    /// </summary>
    public void CheckFloatingAfterPop()
    {
        List<(int, int)> floating = FindFloatingBubbles();
        if (floating.Count > 0)
            RemoveFloatingBubbles(floating);
    }

    /// <summary>
    /// 고립 버블 찾기
    /// </summary>
    public List<(int r, int c)> FindFloatingBubbles()
    {
        return hexMapController.HexMapHandler.FindFloatingBubbles();
    }

    /// <summary>
    /// 고립 버블 제거
    /// </summary>
    public void RemoveFloatingBubbles(List<(int r, int c)> floatingList)
    {
        StartCoroutine(RemoveFloatingBubblesSpread(floatingList));
    }

    /// <summary>
    /// 고립 버블을 순차적으로 떨어뜨리기
    /// </summary>
    private IEnumerator RemoveFloatingBubblesSpread(List<(int r, int c)> floatingList)
    {
        if (floatingList.Count == 0) yield break;

        var start = floatingList[0];
        
        Dictionary<(int, int), int> depth = hexMapController.HexMapHandler.CalculateDepthFromStart(
            start.Item1, start.Item2, floatingList);
        
        List<(int r, int c)> ordered = new List<(int, int)>(floatingList);
        ordered.Sort((a, b) =>
        {
            bool hasA = depth.ContainsKey(a);
            bool hasB = depth.ContainsKey(b);
            if (!hasA && !hasB) return 0;
            if (!hasA) return 1;
            if (!hasB) return -1;
            return depth[a].CompareTo(depth[b]);
        });
        
        float delayStep = 0.05f;
        foreach (var (r, c) in ordered)
        {
            Bubble bubble = hexMapController.GetBubble(r, c);
            if (bubble != null)
            {
                DropFloatingBubble(bubble, r, c);
            }
            
            yield return new WaitForSeconds(delayStep);
        }
    }

    /// <summary>
    /// 고립 버블 제거 후 BubbleSpawner에 알림
    /// </summary>
    private IEnumerator RemoveFloatingBubblesAndNotify(List<(int r, int c)> floatingBubbles, bool hasFairy)
    {
        yield return StartCoroutine(RemoveFloatingBubblesSpread(floatingBubbles));
        
        // 떨어진 버블들이 완전히 삭제될 때까지 대기
        float cleanupDelay = DropPoint != null ? Mathf.Max(0.1f, dropSuctionDuration) : 3f;
        yield return new WaitForSeconds(cleanupDelay);
        
        // 고립 버블 제거
        if (floatingBubbles.Count > 0)
        {
            yield return StartCoroutine(RemoveFloatingBubblesSpread(floatingBubbles));
            
            // 떨어진 버블들이 완전히 삭제될 때까지 대기
            yield return new WaitForSeconds(3f);
        }
        
        // 파괴 성공 처리 (콤보 카운트 증가)
        if (IngameManager.Instance != null)
        {
            IngameManager.Instance.BubbleDestroySuccess();
        }
        
        // 페어리가 있으면 Hitting 상태는 유지 (체력 연출 후 Reloading으로 전환됨)
        // 페어리가 없으면 재배치를 위해 OnBubblesDestroyed 호출
        if (!hasFairy && bubbleSpawner != null)
        {
            bubbleSpawner.OnBubblesDestroyed();
        }
    }

    /// <summary>
    /// 고립 버블을 떨어뜨리기 (드롭 포인트로 이동 또는 물리 시뮬레이션)
    /// </summary>
    private void DropFloatingBubble(Bubble bubble, int r, int c)
    {
        if (bubble == null) return;

        hexMapController.UnregisterBubble(r, c);

        if (DropPoint == null)
        {
            bubble.transform.SetParent(null);
            bubble.Rigid.bodyType = RigidbodyType2D.Dynamic;
            bubble.Rigid.gravityScale = 1f;

            Vector3 center = hexMapController.transform.position;
            Vector2 dir = ((Vector2)bubble.transform.position - (Vector2)center).normalized;
            bubble.Rigid.AddForce(dir * Random.Range(15f, 25f));
            bubble.Rigid.AddTorque(Random.Range(-2f, 2f));

            StartCoroutine(DestroyBubbleAfterDelay(bubble, 3f));
            return;
        }

        bubble.transform.SetParent(null);
        bubble.Rigid.linearVelocity = Vector2.zero;
        bubble.Rigid.angularVelocity = 0f;
        bubble.Rigid.bodyType = RigidbodyType2D.Kinematic;

        bubble.transform.DOKill();

        Vector3 targetPos = GetWorldPositionFromDropPoint(DropPoint);
        Vector3[] spiralPath = BuildSpiralPath(bubble.transform.position, targetPos, dropSuctionTurns, 28);

        Sequence suctionSeq = DOTween.Sequence();
        suctionSeq.Append(bubble.transform.DOPath(
            spiralPath,
            dropSuctionDuration,
            PathType.CatmullRom,
            PathMode.TopDown2D
        ).SetEase(dropSuctionEase));

        suctionSeq.OnComplete(() =>
        {
            bubble.DropBubbleAddScore();
            hexMapController.ObjectPool?.DespawnBubble(bubble);
        });
    }

    /// <summary>
    /// 나선형 경로 생성 (드롭 포인트로 이동)
    /// </summary>
    private Vector3[] BuildSpiralPath(Vector3 start, Vector3 target, float turns, int segments)
    {
        List<Vector3> points = new List<Vector3>(Mathf.Max(segments, 2));
        Vector3 offset = start - target;
        if (offset == Vector3.zero)
        {
            offset = Vector3.right * 0.1f;
        }

        float baseRadius = offset.magnitude * Mathf.Clamp(dropSuctionRadiusScale, 0.1f, 2f);
        Vector3 baseDir = offset.normalized;
        Quaternion alignRotation = Quaternion.FromToRotation(Vector3.right, baseDir);
        Quaternion tiltRotation = Quaternion.AngleAxis(dropSuctionTiltDegrees, baseDir);
        Quaternion finalRotation = tiltRotation * alignRotation;
        float totalTurns = Mathf.Max(turns, 0.25f);
        float verticalScale = Mathf.Clamp(dropSuctionVerticalScale, 0.05f, 1f);

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float radius = Mathf.Lerp(baseRadius, 0f, t);
            float angleDeg = Mathf.Lerp(0f, 360f * totalTurns, t);
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector3 localPoint = new Vector3(Mathf.Cos(angleRad) * radius,
                                             Mathf.Sin(angleRad) * radius * verticalScale,
                                             0f);
            Vector3 rotatedPoint = finalRotation * localPoint;
            Vector3 point = target + rotatedPoint;
            point.z = Mathf.Lerp(start.z, target.z, t);
            points.Add(point);
        }

        return points.ToArray();
    }

    /// <summary>
    /// 드롭 포인트의 월드 위치 가져오기
    /// </summary>
    private Vector3 GetWorldPositionFromDropPoint(RectTransform dropPoint)
    {
        if (dropPoint == null)
            return Vector3.zero;

        Vector3[] corners = new Vector3[4];
        dropPoint.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }

    /// <summary>
    /// 지연 후 버블 파괴
    /// </summary>
    private IEnumerator DestroyBubbleAfterDelay(Bubble bubble, float delay)
    {
        yield return new WaitForSeconds(delay);
        bubble.Rigid.bodyType = RigidbodyType2D.Kinematic;
        hexMapController.ObjectPool?.DespawnBubble(bubble);
    }

    /// <summary>
    /// 버블 파괴 위치에서 페어리 오브젝트 생성
    /// </summary>
    private void SpawnFairyAtPosition(Vector3 position)
    {
        if (hexMapController.ObjectPool == null) return;
        if (IngameManager.Instance == null || IngameManager.Instance.BossObj == null) return;

        // ObjectPool에서 페어리 스폰
        Fairy fairy = hexMapController.ObjectPool.SpawnFairy();
        if (fairy != null)
        {
            // 버블 파괴 위치에 페어리 배치
            fairy.transform.position = position;
            
            // Boss 위치로 날아가기
            Vector3 bossPosition = IngameManager.Instance.BossObj.transform.position;
            fairy.FlyToTarget(bossPosition);
        }
    }
}
