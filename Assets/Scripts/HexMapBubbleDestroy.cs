using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class HexMapBubbleDestroy : MonoBehaviour
{
    private HexMap hexMapController;
    private BubbleSpawner bubbleSpawner;

    public void Initialize(HexMap hexMap)
    {
        hexMapController = hexMap;
        bubbleSpawner = FindObjectOfType<BubbleSpawner>();
    }




    // ================================================================
    // 착지 후 호출: 동일 타입 3개 이상이면 레벨별 전파식으로 팝
    // ================================================================
    public void CheckAndPopMatches(int row, int col)
    {
        Bubble center = hexMapController.GetBubble(row, col);
        if (center == null) return;

        // Spell 타입인 경우 인접 버블 모두 터트리기
        if (center.GetBubbleType() == BubbleTypes.Spell)
        {
            PopSpellBubble(row, col);
            return;
        }

        // 착지한 버블 주변에 Spell 버블이 있는지 체크
        List<(int row, int col)> adjacentCells = hexMapController.GetAdjacentCells(row, col);
        List<(int row, int col)> adjacentSpellBubbles = new List<(int row, int col)>();
        
        foreach (var (adjRow, adjCol) in adjacentCells)
        {
            if (hexMapController.IsValidCell(adjRow, adjCol))
            {
                Bubble adjBubble = hexMapController.GetBubble(adjRow, adjCol);
                if (adjBubble != null && adjBubble.GetBubbleType() == BubbleTypes.Spell)
                {
                    adjacentSpellBubbles.Add((adjRow, adjCol));
                }
            }
        }
        
        // 인접한 모든 Spell 버블 터트리기
        foreach (var (spellRow, spellCol) in adjacentSpellBubbles)
        {
            PopSpellBubble(spellRow, spellCol);
        }
        
        // Spell 버블이 터졌으면 여기서 종료 (연쇄 반응은 PopSpellBubble 내부에서 처리)
        if (adjacentSpellBubbles.Count > 0)
        {
            return;
        }

        // Red, Blue, Yellow 타입은 기존 로직 실행
        List<List<(int r, int c)>> levels = HexMapHandler.CollectConnectedByLevel(hexMapController, row, col);
        if (levels.Count == 0) return;

        // 총 개수 체크
        int totalCount = 0;
        foreach (var level in levels)
            totalCount += level.Count;

        if (totalCount < 3)
        {
            // 매치가 없으면 Reloading 상태로 전환
            if (IngameManager.Instance.CurrentState == BattleState.Shooting)
            {
                IngameManager.Instance.ChangeState(BattleState.Reloading);
            }
            return;
        }

        // 폭발하는 버블들 중 페어리가 있는지 체크
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

        // 페어리가 있으면 Hitting 상태로 전환, 없으면 Destroying 상태로 전환
        if (hasFairy)
        {
            IngameManager.Instance.ChangeState(BattleState.Hitting);
        }
        else
        {
            IngameManager.Instance.ChangeState(BattleState.Destroying);
        }

        // 터질 버블들을 제거한 상태에서 고립 버블 미리 계산
        List<(int r, int c)> floatingBubbles = HexMapHandler.FindFloatingBubblesAfterRemoval(hexMapController, toRemove);

        // 첫 레벨 즉시 파괴
        DestroyLevelImmediate(levels[0]);

        // 나머지 레벨 파동 처리
        if (levels.Count > 1)
        {
            List<List<(int r, int c)>> remainingLevels = levels.GetRange(1, levels.Count - 1);
            StartCoroutine(DestroyByWave(remainingLevels, floatingBubbles));
        }
        else
        {
            // 레벨이 1개뿐이면 고립 버블만 처리
            if (floatingBubbles.Count > 0)
            {
                StartCoroutine(RemoveFloatingBubblesAndNotify(floatingBubbles, hasFairy));
            }
            else
            {
                // 고립 버블이 없으면
                if (hasFairy)
                {
                    // 페어리가 있으면 Hitting 상태는 유지 (체력 연출 후 Reloading으로 전환됨)
                }
                else
                {
                    // 페어리가 없으면 Reloading 상태로 전환
                    if (IngameManager.Instance.CurrentState == BattleState.Destroying)
                    {
                        IngameManager.Instance.ChangeState(BattleState.Reloading);
                    }
                }
            }
        }
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
        List<(int r, int c)> floatingBubbles = HexMapHandler.FindFloatingBubblesAfterRemoval(hexMapController, toRemove);

        // Spell 버블은 레벨별 전파 없이 모두 즉시 파괴
        List<(int r, int c)> spellBubbles = new List<(int r, int c)>(toRemove);
        
        // 첫 번째 버블 즉시 파괴
        if (spellBubbles.Count > 0)
        {
            DestroyLevelImmediate(new List<(int r, int c)> { spellBubbles[0] });
        }

        // 나머지 버블들도 파괴 (딜레이 없이)
        if (spellBubbles.Count > 1)
        {
            List<(int r, int c)> remainingBubbles = spellBubbles.GetRange(1, spellBubbles.Count - 1);
            StartCoroutine(DestroySpellBubblesAndNotify(remainingBubbles, floatingBubbles, hasFairy));
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
    private IEnumerator DestroySpellBubblesAndNotify(List<(int r, int c)> remainingBubbles, List<(int r, int c)> floatingBubbles, bool hasFairy)
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

    // 첫 레벨 즉시 파괴 (딜레이 없음)
    private void DestroyLevelImmediate(List<(int r, int c)> level)
    {
        // 파괴 전에 인접한 Spell 버블 체크
        HashSet<(int r, int c)> spellBubblesToPop = new HashSet<(int r, int c)>();
        
        // level을 HashSet으로 변환
        HashSet<(int r, int c)> levelSet = new HashSet<(int r, int c)>(level);
        
        foreach (var (r, c) in level)
        {
            // 인접한 Spell 버블 찾기
            CheckAdjacentSpellBubbles(r, c, spellBubblesToPop, levelSet);
        }
        
        // 인접한 Spell 버블들도 터트리기
        foreach (var (spellRow, spellCol) in spellBubblesToPop)
        {
            // Spell 버블이 아직 존재하고 파괴되지 않았으면 터트리기
            Bubble spellBubble = hexMapController.GetBubble(spellRow, spellCol);
            if (spellBubble != null && spellBubble.GetBubbleType() == BubbleTypes.Spell)
            {
                PopSpellBubble(spellRow, spellCol);
            }
        }
        
        // 원래 버블들 파괴
        foreach (var (r, c) in level)
        {
            Bubble b = hexMapController.GetBubble(r, c);
            if (b == null) continue;

            // 요정이 있으면 페어리 오브젝트 생성
            if (b.IsFairy)
            {
                SpawnFairyAtPosition(b.transform.position);
            }

            if (b.Anim != null)
                b.DestroyBubble();  // 애니메이션 재생

            hexMapController.UnregisterBubble(r, c);
            hexMapController.ObjectPool?.DespawnBubble(b);
        }
    }

/// <summary>
/// 인접한 Spell 버블 찾기
/// </summary>
private void CheckAdjacentSpellBubbles(int row, int col, HashSet<(int r, int c)> spellBubbles, HashSet<(int r, int c)> excludeList)
{
    List<(int row, int col)> adjacentCells = hexMapController.GetAdjacentCells(row, col);
    
    foreach (var (adjRow, adjCol) in adjacentCells)
    {
        // 이미 파괴 예정인 버블은 제외
        if (excludeList.Contains((adjRow, adjCol)))
            continue;
            
        // 유효한 셀이고 버블이 있으면 체크
        if (hexMapController.IsValidCell(adjRow, adjCol))
        {
            Bubble adjBubble = hexMapController.GetBubble(adjRow, adjCol);
            if (adjBubble != null && adjBubble.GetBubbleType() == BubbleTypes.Spell)
            {
                spellBubbles.Add((adjRow, adjCol));
            }
        }
    }
}

// ================================================================
// 단계별 파동 처리
// ================================================================
private IEnumerator DestroyByWave(List<List<(int r, int c)>> levels, List<(int r, int c)> floatingBubbles)
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
        
        // 파괴 전에 인접한 Spell 버블 체크
        HashSet<(int r, int c)> spellBubblesToPop = new HashSet<(int r, int c)>();
        
        foreach (var (r, c) in group)
        {
            // 인접한 Spell 버블 찾기
            CheckAdjacentSpellBubbles(r, c, spellBubblesToPop, allToDestroy);
        }
        
        // 인접한 Spell 버블들도 터트리기
        foreach (var (spellRow, spellCol) in spellBubblesToPop)
        {
            // Spell 버블이 아직 존재하고 파괴되지 않았으면 터트리기
            Bubble spellBubble = hexMapController.GetBubble(spellRow, spellCol);
            if (spellBubble != null && spellBubble.GetBubbleType() == BubbleTypes.Spell)
            {
                PopSpellBubble(spellRow, spellCol);
            }
        }

        foreach (var (r, c) in group)
        {
            Bubble b = hexMapController.GetBubble(r, c);
            if (b == null) continue;

            // 요정이 있으면 페어리 오브젝트 생성
            if (b.IsFairy)
            {
                SpawnFairyAtPosition(b.transform.position);
            }

            if (b.Anim != null)
                b.DestroyBubble();

            hexMapController.UnregisterBubble(r, c);
            hexMapController.ObjectPool?.DespawnBubble(b);
        }

        yield return new WaitForSeconds(delayPerLevel);
    }
}

    // ================================================================
    // 기존 BFS 같은 타입 찾기 (HexMapHandler 사용)
    // ================================================================
    public List<(int row, int col)> FindConnectedSameType(int row, int col)
    {
        return HexMapHandler.FindConnectedSameType(hexMapController, row, col);
    }

    // ================================================================
    // 고립 버블 체크
    // ================================================================
    public void CheckFloatingAfterPop()
    {
        List<(int, int)> floating = FindFloatingBubbles();
        if (floating.Count > 0)
            RemoveFloatingBubbles(floating);
    }

    public List<(int r, int c)> FindFloatingBubbles()
    {
        return HexMapHandler.FindFloatingBubbles(hexMapController);
    }

    // ================================================================
    // 고립 버블 제거
    // ================================================================
    public void RemoveFloatingBubbles(List<(int r, int c)> floatingList)
    {
        StartCoroutine(RemoveFloatingBubblesSpread(floatingList));
    }

    private IEnumerator RemoveFloatingBubblesSpread(List<(int r, int c)> floatingList)
    {
        if (floatingList.Count == 0) yield break;

        var start = floatingList[0];
        
        // HexMapHandler를 사용하여 depth 계산
        Dictionary<(int, int), int> depth = HexMapHandler.CalculateDepthFromStart(
            hexMapController, start.Item1, start.Item2, floatingList);
        
        // depth 순으로 정렬
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
                DropFloatingBubble(bubble, r, c);
            
            yield return new WaitForSeconds(delayStep);
        }
    }

    /// <summary>
    /// 고립 버블 제거 후 BubbleSpawner에 알림
    /// </summary>
    private IEnumerator RemoveFloatingBubblesAndNotify(List<(int r, int c)> floatingList, bool hasFairy)
    {
        yield return StartCoroutine(RemoveFloatingBubblesSpread(floatingList));
        
        // 떨어진 버블들이 완전히 삭제될 때까지 대기 (DestroyBubbleAfterDelay가 3초 후 삭제)
        yield return new WaitForSeconds(3f);
        
        // 페어리가 있으면 Hitting 상태는 유지 (체력 연출 후 Reloading으로 전환됨)
        // 페어리가 없으면 재배치를 위해 OnBubblesDestroyed 호출
        if (!hasFairy && bubbleSpawner != null)
        {
            bubbleSpawner.OnBubblesDestroyed();
        }
    }

    private void DropFloatingBubble(Bubble bubble, int r, int c)
    {
        hexMapController.UnregisterBubble(r, c);
        bubble.transform.SetParent(null);
        bubble.Rigid.bodyType = RigidbodyType2D.Dynamic;
        bubble.Rigid.gravityScale = 1f;

        Vector3 center = hexMapController.transform.position;
        Vector2 dir = ((Vector2)bubble.transform.position - (Vector2)center).normalized;
        bubble.Rigid.AddForce(dir * Random.Range(15f, 25f));
        bubble.Rigid.AddTorque(Random.Range(-2f, 2f));

        StartCoroutine(DestroyBubbleAfterDelay(bubble, 3f));
    }

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
