using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class UIBubbleShot : MonoBehaviour
{
    [Header("참조")]
    public UIBubbleAim Aim;
    public UIBubblePreview Preview;
    public UIBubbleShooter Shooter;
    public HexMap HexMap;
    public ObjectPool BubblePool;

    [Header("발사 설정")]
    public float shotSpeed = 5f;
    private bool isShooting = false;
    private GameObject currentShotBubble = null;

    /// <summary>
    /// 발사 중인지 여부를 반환
    /// </summary>
    public bool IsShooting() => isShooting;

    /// <summary>
    /// 초기화 및 Reloading 상태 감지 코루틴 시작
    /// </summary>
    void Start()
    {
        StartCoroutine(WatchForReloadingState());
    }

    /// <summary>
    /// Reloading 상태를 감지하고 장전 애니메이션 실행
    /// </summary>
    private IEnumerator WatchForReloadingState()
    {
        while (true)
        {
            if (IngameManager.Instance.CurrentState == BattleState.Reloading &&
                Shooter != null &&
                !Shooter.BubbleRotation.IsRotating)
            {
                if (!Shooter.IsNeroBubblePending)
                {
                    if (Shooter.Bubbles != null && Shooter.Bubbles.Count >= 3)
                    {
                        if (IngameManager.Instance.CurrentState == BattleState.Reloading)
                        {
                            IngameManager.Instance.ChangeState(BattleState.Normal);
                            ActivateNeroButtonIfNeeded();
                            CheckShottingCountAndFail();
                        }
                        yield return new WaitForSeconds(0.1f);
                        continue;
                    }

                    Shooter.ReloadAfterShot();
                    yield return new WaitUntil(() => !Shooter.BubbleRotation.IsRotating);

                    if (IngameManager.Instance.CurrentState == BattleState.Reloading)
                    {
                        IngameManager.Instance.ChangeState(BattleState.Normal);
                        ActivateNeroButtonIfNeeded();
                        CheckShottingCountAndFail();
                    }
                }
                else
                {
                    yield return new WaitUntil(() => !Shooter.BubbleRotation.IsRotating);

                    if (IngameManager.Instance.CurrentState == BattleState.Reloading)
                    {
                        IngameManager.Instance.ChangeState(BattleState.Normal);
                        ActivateNeroButtonIfNeeded();
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

        bool hasNeroBubble = false;
        foreach (var bubble in Shooter.Bubbles)
        {
            if (bubble != null && bubble.Type == BubbleTypes.Nero)
            {
                hasNeroBubble = true;
                break;
            }
        }

        if (!hasNeroBubble)
        {
            IngameManager.Instance.NeroObj.ActivateAddFillButton = true;
        }
    }

    /// <summary>
    /// ShottingCount를 체크하고 0이면 Fail 로직 실행
    /// </summary>
    private void CheckShottingCountAndFail()
    {
        if (Shooter.ShottingCountValue <= 0)
        {
            if (IngameManager.Instance != null)
            {
                IngameManager.Instance.GameFail();
            }
        }
    }

    /// <summary>
    /// 버블 발사 처리
    /// </summary>
    public void ShootBubble()
    {
        if (isShooting || Shooter.CurrentBubble == null || Preview == null || Aim.IsAiming == false)
            return;

        if (IngameManager.Instance.CurrentState != BattleState.Normal)
            return;

        Vector3? targetPosition = Preview.GetPreviewPosition();
        if (!targetPosition.HasValue)
        {
            HexMap?.HexMapBubbleDestroy?.ClearPendingNeroTargets();
            return;
        }

        Shooter.ShottingCountValue--;
        Shooter.UpdateShottingCountUI();

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

        Bubble bubbleObj = CreateBubbleFromUI(Shooter.CurrentBubble);
        if (bubbleObj == null) return;

        StartCoroutine(MoveBubbleCoroutine(bubbleObj, bubbleObj.transform.position, targetPosition.Value));
    }

    /// <summary>
    /// UI 버블을 실제 게임 버블로 생성
    /// </summary>
    private Bubble CreateBubbleFromUI(UIBubble uiBubble)
    {
        Bubble bubble = BubblePool.SpawnBubble(uiBubble.Type);
        if (bubble == null) return null;

        bubble.SetBubble(uiBubble.Type, true);
        bubble.transform.position = uiBubble.Rect.position;
        return bubble;
    }


    /// <summary>
    /// 버블 이동 코루틴 (발사 후 목표 위치까지 이동 및 등록)
    /// </summary>
    private IEnumerator MoveBubbleCoroutine(Bubble bubble, Vector3 start, Vector3 target)
    {
        isShooting = true;
        currentShotBubble = bubble.gameObject;

        IngameManager.Instance.ChangeState(BattleState.Shooting);
        Shooter.UnselectBubble();
        Aim?.SetAimEnabled(false);

        List<Vector3> path = GeneratePath(start, target);

        foreach (var point in path)
        {
            while (Vector3.Distance(bubble.transform.position, point) > 0.05f)
            {
                bubble.transform.position = Vector3.MoveTowards(bubble.transform.position, point, shotSpeed * Time.deltaTime);
                yield return null;
            }
            bubble.transform.position = point;
            yield return null;
        }

        Vector3 snappedPos = SnapToGrid(bubble.transform.position);

        if (HexMap != null)
        {
            var (row, col) = HexMap.WorldToGrid(snappedPos);
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

        isShooting = false;
        currentShotBubble = null;

        yield return new WaitUntil(() =>
            IngameManager.Instance.CurrentState == BattleState.Reloading ||
            IngameManager.Instance.CurrentState == BattleState.Destroying);

        if (IngameManager.Instance.CurrentState == BattleState.Reloading)
        {
            yield return new WaitUntil(() => IngameManager.Instance.CurrentState == BattleState.Normal);
            ActivateNeroButtonIfNeeded();
            CheckShottingCountAndFail();
        }
        else if (IngameManager.Instance.CurrentState == BattleState.Destroying)
        {
            yield return new WaitUntil(() =>
                IngameManager.Instance.CurrentState == BattleState.Reloading ||
                IngameManager.Instance.CurrentState == BattleState.Normal);

            if (IngameManager.Instance.CurrentState == BattleState.Reloading)
            {
                yield return new WaitUntil(() => IngameManager.Instance.CurrentState == BattleState.Normal);
                ActivateNeroButtonIfNeeded();
                CheckShottingCountAndFail();
            }
            else if (IngameManager.Instance.CurrentState == BattleState.Normal)
            {
                ActivateNeroButtonIfNeeded();
                CheckShottingCountAndFail();
            }
        }
    }


    /// <summary>
    /// 버블 이동 경로 생성 (벽 충돌 고려)
    /// </summary>
    private List<Vector3> GeneratePath(Vector3 start, Vector3 target)
    {
        List<Vector3> path = new List<Vector3>();

        Vector3 finalPos = GetPreviewPositionWithZ(start);
        Vector3? wallHitPoint = Aim?.GetFirstWallHitAlongAim();
        List<Vector3> pathPoints = Aim?.GetPathPoints();

        if (pathPoints != null && pathPoints.Count >= 2)
        {
            if (pathPoints.Count >= 3 && wallHitPoint.HasValue)
            {
                path.Add(SetZ(wallHitPoint.Value, start.z));
                path.Add(finalPos);
            }
            else
            {
                path.Add(finalPos);
            }
        }
        else
        {
            if (Aim != null && Aim.TryGetWallBetween(start, finalPos, out RaycastHit2D wallHit))
            {
                path.Add(SetZ(wallHit.point, start.z));
                path.Add(finalPos);
            }
            else
            {
                path.Add(finalPos);
            }
        }

        return path;
    }

    /// <summary>
    /// 프리뷰 위치를 가져와서 z 값을 시작점과 맞춤
    /// </summary>
    private Vector3 GetPreviewPositionWithZ(Vector3 start)
    {
        Vector3? previewPos = Preview?.GetPreviewPosition()
                             ?? Preview?.GetPreviewPosition();

        if (!previewPos.HasValue)
            previewPos = start;

        return SetZ(previewPos.Value, start.z);
    }

    /// <summary>
    /// 벡터의 z 값을 지정된 값으로 설정
    /// </summary>
    private Vector3 SetZ(Vector3 pos, float z) => new Vector3(pos.x, pos.y, z);

    /// <summary>
    /// 위치를 HexMap 격자에 스냅
    /// </summary>
    private Vector3 SnapToGrid(Vector3 position)
    {
        if (HexMap == null) return position;

        var (row, col) = HexMap.WorldToGrid(position);
        return HexMap.GetWorldPosition(row, col);
    }


}
