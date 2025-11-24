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

        IngameManager.Instance.ChangeState(BattleState.Destroying);

        // 터질 버블들을 HashSet으로 수집 (고립 버블 미리 계산용)
        HashSet<(int r, int c)> toRemove = new HashSet<(int r, int c)>();
        foreach (var level in levels)
        {
            foreach (var (r, c) in level)
            {
                toRemove.Add((r, c));
            }
        }

        // 터질 버블들을 제거한 상태에서 고립 버블 미리 계산
        List<(int r, int c)> floatingBubbles = HexMapHandler.FindFloatingBubblesAfterRemoval(hexMapController, toRemove);

        // 첫 레벨 즉시 파괴
        DestroyLevelImmediate(levels[0]);

        // 나머지 레벨 파동 처리
        if (levels.Count > 1)
        {
            List<List<(int r, int c)>> remainingLevels = levels.GetRange(1, levels.Count - 1);
            StartCoroutine(DestroyByWaveAndNotify(remainingLevels, floatingBubbles));
        }
        else
        {
            // 레벨이 1개뿐이면 고립 버블만 처리
            if (floatingBubbles.Count > 0)
            {
                StartCoroutine(RemoveFloatingBubblesAndNotify(floatingBubbles));
            }
            else
            {
                // 고립 버블이 없으면 Reloading 상태로 전환 (파괴가 없으므로)
                if (IngameManager.Instance.CurrentState == BattleState.Destroying)
                {
                    IngameManager.Instance.ChangeState(BattleState.Reloading);
                }
                // 재배치는 Reloading 상태가 끝난 후에 처리됨
            }
        }
    }

    // 첫 레벨 즉시 파괴 (딜레이 없음)
    private void DestroyLevelImmediate(List<(int r, int c)> level)
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

            if (b.Anim != null)
                b.DestroyBubble();  // 애니메이션 재생

            hexMapController.UnregisterBubble(r, c);
            hexMapController.ObjectPool?.DespawnBubble(b);
        }
    }



    // ================================================================
    // 단계별 파동 처리
    // ================================================================
    private IEnumerator DestroyByWave(List<List<(int r, int c)>> levels, List<(int r, int c)> floatingBubbles)
    {
        float delayPerLevel = 0.08f;

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

                if (b.Anim != null)
                    b.DestroyBubble();

                hexMapController.UnregisterBubble(r, c);
                hexMapController.ObjectPool?.DespawnBubble(b);
            }

            yield return new WaitForSeconds(delayPerLevel);
        }
    }

    /// <summary>
    /// 파동 처리와 고립 버블 제거를 모두 완료한 후 알림
    /// </summary>
    private IEnumerator DestroyByWaveAndNotify(List<List<(int r, int c)>> levels, List<(int r, int c)> floatingBubbles)
    {
        yield return StartCoroutine(DestroyByWave(levels, floatingBubbles));
        
        // 고립 버블 제거
        if (floatingBubbles.Count > 0)
        {
            yield return StartCoroutine(RemoveFloatingBubblesSpread(floatingBubbles));
            
            // 떨어진 버블들이 완전히 삭제될 때까지 대기 (DestroyBubbleAfterDelay가 3초 후 삭제)
            yield return new WaitForSeconds(3f);
        }
        
        // 파괴가 완료되었으므로 RespawnBubbles 상태로 전환 (재배치를 위해)
        // 이 상태는 BubbleSpawner에서 Normal로 전환됨
        if (bubbleSpawner != null)
        {
            bubbleSpawner.OnBubblesDestroyed();
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
    private IEnumerator RemoveFloatingBubblesAndNotify(List<(int r, int c)> floatingList)
    {
        yield return StartCoroutine(RemoveFloatingBubblesSpread(floatingList));
        
        // 떨어진 버블들이 완전히 삭제될 때까지 대기 (DestroyBubbleAfterDelay가 3초 후 삭제)
        yield return new WaitForSeconds(3f);
        
        // 버블 파괴 완료 후 BubbleSpawner에 알림
        if (bubbleSpawner != null)
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
