using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening; // DOTween 추가

public class UIBubbleShot : MonoBehaviour
{
    [Header("참조")]
    public UIBubbleAim Aim;
    public UIBubblePreview Preview;
    public UIBubbleShooter Shooter;
    public HexMap HexMap;
    public ObjectPool BubblePool;

    [Header("발사 설정")]
    public float shotSpeed = 5f;             // 버블 이동 속도
    private bool isShooting = false;         // 발사 중인지 여부
    private GameObject currentShotBubble = null;  // 현재 발사된 버블



    public bool IsShooting() => isShooting;

    void Start()
    {
        // Reloading 상태 감지 코루틴 시작
        StartCoroutine(WatchForReloadingState());

        
    }

    /// <summary>
    /// ShottingCount UI 갱신
    /// </summary>


    /// <summary>
    /// Reloading 상태를 감지하고 장전 애니메이션 실행
    /// </summary>
    private IEnumerator WatchForReloadingState()
    {
        while (true)
        {
            // Reloading 상태이고 장전 애니메이션이 실행되지 않았을 때
            if (IngameManager.Instance.CurrentState == BattleState.Reloading &&
                Shooter != null &&
                !Shooter.BubbleRotation.IsRotating)
            {
                // Nero 버블이 들어올 예정인지 체크
                if (!Shooter.IsNeroBubblePending())
                {
                    // 버블이 3개 이상이면 Nero 버블이 이미 추가된 상태이므로 처리하지 않음
                    if (Shooter.Bubbles != null && Shooter.Bubbles.Count >= 3)
                    {
                        // Nero 버블 애니메이션이 완료되었으므로 Normal 상태로 전환
                        if (IngameManager.Instance.CurrentState == BattleState.Reloading)
                        {
                            IngameManager.Instance.ChangeState(BattleState.Normal);

                            // Normal 상태로 전환 후 네로 버블 활성화 체크
                            ActivateNeroButtonIfNeeded();

                            // Normal 상태로 전환 후 ShottingCount 체크
                            CheckShottingCountAndFail();
                        }
                        yield return new WaitForSeconds(0.1f);
                        continue;
                    }

                    // 발사 후 재장전 애니메이션 실행
                    Shooter.ReloadAfterShot();

                    // 장전 애니메이션이 끝날 때까지 대기
                    yield return new WaitUntil(() => !Shooter.BubbleRotation.IsRotating);

                    // 장전 완료 후 Normal 상태로 전환
                    if (IngameManager.Instance.CurrentState == BattleState.Reloading)
                    {
                        IngameManager.Instance.ChangeState(BattleState.Normal);

                        // Normal 상태로 전환 후 네로 버블 활성화 체크
                        ActivateNeroButtonIfNeeded();

                        // Normal 상태로 전환 후 ShottingCount 체크
                        CheckShottingCountAndFail();
                    }
                }
                else
                {
                    // Nero 버블이 들어올 예정이면 ReloadAfterShot이 이미 호출되었을 것이므로
                    // 애니메이션이 끝날 때까지 대기만 함
                    yield return new WaitUntil(() => !Shooter.BubbleRotation.IsRotating);

                    // 장전 완료 후 Normal 상태로 전환
                    if (IngameManager.Instance.CurrentState == BattleState.Reloading)
                    {
                        IngameManager.Instance.ChangeState(BattleState.Normal);

                        // Normal 상태로 전환 후 네로 버블 활성화 체크
                        ActivateNeroButtonIfNeeded();

                        // Normal 상태로 전환 후 ShottingCount 체크
                        CheckShottingCountAndFail();
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// 현재 장전된 버블에 네로 버블이 없으면 네로 버튼 활성화
    /// </summary>
    private void ActivateNeroButtonIfNeeded()
    {
        if (IngameManager.Instance == null ||
            IngameManager.Instance.NeroObj == null ||
            Shooter == null ||
            Shooter.Bubbles == null)
        {
            return;
        }

        // 현재 장전된 버블에 네로 버블이 있는지 확인
        bool hasNeroBubble = false;
        foreach (var bubble in Shooter.Bubbles)
        {
            if (bubble != null && bubble.Type == BubbleTypes.Nero)
            {
                hasNeroBubble = true;
                break;
            }
        }

        // 네로 버블이 없으면 활성화
        if (!hasNeroBubble)
        {
            IngameManager.Instance.NeroObj.ActivateAddFillButton();
        }
    }

    /// <summary>
    /// ShottingCount를 체크하고 0이면 Fail 로직 실행
    /// </summary>
    private void CheckShottingCountAndFail()
    {
        if (Shooter.ShottingCountValue <= 0)
        {
            // Fail 로직 실행
            if (IngameManager.Instance != null)
            {
                IngameManager.Instance.GameFail();
            }
        }
    }


    public void ShootBubble()
    {

        // 이미 발사 중이거나 필요한 참조가 없으면 실행하지 않음
        if (isShooting || Shooter.CurrentBubble == null || Preview == null || Aim.IsAiming == false)
            return;

        // 슈팅 상태가 아니면 발사 불가
        if (IngameManager.Instance.CurrentState != BattleState.Normal)
            return;

        // 프리뷰 버블 위치 가져오기
        Vector3? targetPosition = Preview.GetPreviewPosition();
        if (!targetPosition.HasValue)
        {
            HexMap?.HexMapBubbleDestroy?.ClearPendingNeroTargets();
            return;
        }



        Shooter.ShottingCountValue--;

        // UI 갱신
        Shooter.UpdateShottingCountUI();

        // 네로 버블이면 프리뷰 범위 저장
        if (Shooter.CurrentBubble.Type == BubbleTypes.Nero)
        {
            if (HexMap != null && HexMap.HexMapBubbleDestroy != null)
            {
                List<Vector3> neroTargets = Preview.GetNeroPreviewWorldPositions();
                HexMap.HexMapBubbleDestroy.SetPendingNeroTargets(neroTargets);
            }
        }
        else
        {
            HexMap?.HexMapBubbleDestroy?.ClearPendingNeroTargets();
        }

        // UI 버블을 실제 게임 버블로 생성
        Bubble bubbleObj = CreateBubbleFromUI(Shooter.CurrentBubble);
        if (bubbleObj == null) return;

        // 코루틴으로 버블 이동 시작
        StartCoroutine(MoveBubbleCoroutine(bubbleObj, bubbleObj.transform.position, targetPosition.Value));
    }


    private Bubble CreateBubbleFromUI(UIBubble uiBubble)
    {

        // Object Pool에서 버블 생성
        Bubble bubble = BubblePool.SpawnBubble(uiBubble.Type);
        if (bubble == null) return null;

        // 발사된 버블이므로 페어리가 붙지 않도록 설정
        bubble.SetBubble(uiBubble.Type, true);

        bubble.transform.position = uiBubble.Rect.position; // 월드 위치 기준
        return bubble;
    }


    private IEnumerator MoveBubbleCoroutine(Bubble bubble, Vector3 start, Vector3 target)
    {
        isShooting = true;
        currentShotBubble = bubble.gameObject;

        // 슈팅 상태로 전환
        IngameManager.Instance.ChangeState(BattleState.Shooting);

        // UI 버블 비활성화, 조준 UI 비활성화
        Shooter.UnselectBubble();
        Aim?.SetAimEnabled(false);

        // 이동 경로 생성
        List<Vector3> path = GeneratePath(start, target);

        // 경로에 따라 차례로 이동
        foreach (var point in path)
        {
            while (Vector3.Distance(bubble.transform.position, point) > 0.05f)
            {
                bubble.transform.position = Vector3.MoveTowards(bubble.transform.position, point, shotSpeed * Time.deltaTime);
                yield return null;
            }
            bubble.transform.position = point; // 목표점 스냅
            yield return null;
        }

        // 최종 위치를 격자 위치로 스냅
        Vector3 snappedPos = SnapToGrid(bubble.transform.position);

        if (HexMap != null)
        {
            var (row, col) = HexMap.WorldToGrid(snappedPos);
            // 빈 공간이 아니면 인접 빈 공간 찾기
            if (!HexMap.IsEmpty(row, col))
            {
                var emptyCells = HexMapHandler.GetEmptyAdjacentCells(HexMap, row, col);
                var (emptyRow, emptyCol) = emptyCells.Count > 0 ? emptyCells[0] : (row, col);
                snappedPos = HexMap.GetWorldPosition(emptyRow, emptyCol);
                row = emptyRow;
                col = emptyCol;
            }
            HexMap.RegisterBubble(row, col, bubble, true);
        }

        // UI 버블 복구
        isShooting = false;
        currentShotBubble = null;

        // CheckAndPopMatches가 호출되어 상태가 변경될 때까지 대기
        // 매치가 있으면 Destroying, 없으면 Reloading 상태로 전환됨
        yield return new WaitUntil(() =>
            IngameManager.Instance.CurrentState == BattleState.Reloading ||
            IngameManager.Instance.CurrentState == BattleState.Destroying);

        // 1. 매치가 없어서 Reloading 상태로 전환된 경우
        //    → 새로운 버블 준비 → WatchForReloadingState에서 장전 애니메이션 처리
        if (IngameManager.Instance.CurrentState == BattleState.Reloading)
        {
            // WatchForReloadingState에서 자동으로 ReloadAfterShot 호출
            yield return new WaitUntil(() => IngameManager.Instance.CurrentState == BattleState.Normal);

            // Normal 상태로 전환 후 네로 버블 활성화 체크
            ActivateNeroButtonIfNeeded();

            // Normal 상태로 전환 후 ShottingCount 체크
            CheckShottingCountAndFail();
        }
        // 2. 매치가 있어서 Destroying 상태로 전환된 경우
        //    → 파괴 진행 → 재배치 완료 → Reloading 상태로 전환 → 새로운 버블 준비 → WatchForReloadingState에서 장전 처리
        else if (IngameManager.Instance.CurrentState == BattleState.Destroying)
        {
            // 파괴 및 재배치가 완료되어 Reloading 상태로 전환될 때까지 대기
            yield return new WaitUntil(() =>
                IngameManager.Instance.CurrentState == BattleState.Reloading ||
                IngameManager.Instance.CurrentState == BattleState.Normal);

            // Reloading 상태로 전환되면 WatchForReloadingState에서 자동으로 처리
            if (IngameManager.Instance.CurrentState == BattleState.Reloading)
            {
                yield return new WaitUntil(() => IngameManager.Instance.CurrentState == BattleState.Normal);

                // Normal 상태로 전환 후 네로 버블 활성화 체크
                ActivateNeroButtonIfNeeded();

                // Normal 상태로 전환 후 ShottingCount 체크
                CheckShottingCountAndFail();
            }
            else if (IngameManager.Instance.CurrentState == BattleState.Normal)
            {
                // 이미 Normal 상태면 바로 체크
                ActivateNeroButtonIfNeeded();
                CheckShottingCountAndFail();
            }
        }
    }


    private List<Vector3> GeneratePath(Vector3 start, Vector3 target)
    {
        List<Vector3> path = new List<Vector3>();

        // 실제 프리뷰 좌표 가져오기 + z 값 맞춤
        Vector3 finalPos = GetPreviewPositionWithZ(start);

        // 벽 충돌 정보 가져오기
        Vector3? wallHitPoint = Aim?.GetFirstWallHitAlongAim();
        List<Vector3> pathPoints = Aim?.GetPathPoints();

        // 경로 계산: 벽 튕김 여부 확인
        if (pathPoints != null && pathPoints.Count >= 2)
        {
            if (pathPoints.Count >= 3 && wallHitPoint.HasValue)
            {
                path.Add(SetZ(wallHitPoint.Value, start.z)); // 벽 충돌 지점
                path.Add(finalPos);                         // 목표 지점
            }
            else
            {
                path.Add(finalPos); // 벽 없이 바로 목표로
            }
        }
        else
        {
            // trajectory가 없으면 시작점에서 프리뷰까지 직접 벽 체크
            if (Aim != null && Aim.TryGetWallBetween(start, finalPos, out RaycastHit2D wallHit))
            {
                path.Add(SetZ(wallHit.point, start.z)); // 벽 충돌 지점
                path.Add(finalPos);                      // 목표 지점
            }
            else
            {
                path.Add(finalPos); // 벽 없으면 그냥 목표
            }
        }

        return path;
    }


    private Vector3 GetPreviewPositionWithZ(Vector3 start)
    {
        // Preview 좌표 가져오기, 순서대로 확인
        Vector3? previewPos = Preview?.GetPreviewPosition()
                             ?? Preview?.GetPreviewPosition();

        if (!previewPos.HasValue)
            previewPos = start;

        return SetZ(previewPos.Value, start.z);
    }


    private Vector3 SetZ(Vector3 pos, float z) => new Vector3(pos.x, pos.y, z);

    // ===============================
    // 6. SnapToGrid
    // ===============================
    private Vector3 SnapToGrid(Vector3 position)
    {
        if (HexMap == null) return position;

        // WorldToGrid로 변환 후 다시 월드 좌표로 변환
        var (row, col) = HexMap.WorldToGrid(position);
        return HexMap.GetWorldPosition(row, col);
    }


}
