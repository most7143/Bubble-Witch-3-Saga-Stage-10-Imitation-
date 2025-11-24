using UnityEngine;
using System.Collections;

public class Fairy : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float flightDuration = 2f;  // 이동 시간
    [SerializeField] private float curveHeight = 2f;     // 곡선 높이 (제어점 높이)
    
    private bool isFlying = false;
    private Coroutine flyCoroutine;



    public void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.tag == "Boss")
        {
            other.gameObject.GetComponent<Boss>().TakeDamage(10);
            Destroy(gameObject);
        }
    }

    public void DespawnFairy()
    {
        // 이동 중이면 정지
        if (flyCoroutine != null)
        {
            StopCoroutine(flyCoroutine);
            flyCoroutine = null;
        }
        
        ObjectPool.Instance.DespawnFairy(this);
    }



    /// <summary>
    /// 베지어 곡선을 이용해 타겟까지 날아가기
    /// </summary>
    /// <param name="target">목표 위치</param>
    public void FlyToTarget(Vector3 target)
    {
        if (isFlying) return;
        
        if (flyCoroutine != null)
        {
            StopCoroutine(flyCoroutine);
        }
        
        flyCoroutine = StartCoroutine(FlyToTargetCoroutine(target));
    }

    /// <summary>
    /// 베지어 곡선 이동 코루틴 (2차 베지어 곡선 사용)
    /// </summary>
    private IEnumerator FlyToTargetCoroutine(Vector3 target)
    {
        isFlying = true;
        
        Vector3 startPosition = transform.position;
        Vector3 endPosition = target;
        
        // 중간 제어점 계산 (곡선을 만들기 위한 점)
        Vector3 midPoint = (startPosition + endPosition) / 2f;
        
        // 랜덤한 방향과 높이로 제어점 생성
        Vector3 direction = (endPosition - startPosition).normalized;
        
        // 수직 방향 (위/아래 랜덤)
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;
        if (perpendicular == Vector3.zero)
        {
            perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        }
        
        // 랜덤한 각도 (0~360도)
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        // 랜덤한 높이 (최소~최대)
        float randomHeight = Random.Range(curveHeight * 0.5f, curveHeight * 1.5f);
        
        // 랜덤한 좌우 편향
        float randomSideOffset = Random.Range(-curveHeight * 0.5f, curveHeight * 0.5f);
        
        // 제어점 계산: 중간점 + 위쪽 랜덤 높이 + 좌우 랜덤 편향
        Vector3 upOffset = Vector3.up * randomHeight;
        Vector3 sideOffset = perpendicular * randomSideOffset;
        Vector3 randomDirection = new Vector3(
            Mathf.Cos(randomAngle) * randomHeight,
            Mathf.Sin(randomAngle) * randomHeight,
            0f
        );
        
        Vector3 controlPoint = midPoint + upOffset + sideOffset + randomDirection * 0.3f;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < flightDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / flightDuration;
            
            // 2차 베지어 곡선 공식: B(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
            // P₀: 시작점, P₁: 제어점, P₂: 끝점
            Vector3 position = CalculateQuadraticBezier(startPosition, controlPoint, endPosition, t);
            
            transform.position = position;
            
            // 타겟 방향으로 회전 (선택사항)
            if (t > 0.1f) // 약간 이동한 후에 회전 시작
            {
                Vector3 moveDirection = (endPosition - transform.position).normalized;
                if (moveDirection != Vector3.zero)
                {
                    float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }
            }
            
            yield return null;
        }
        
        // 최종 위치 보정
        transform.position = endPosition;
        isFlying = false;
        flyCoroutine = null;
    }

    /// <summary>
    /// 2차 베지어 곡선 계산
    /// </summary>
    private Vector3 CalculateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    /// <summary>
    /// 3차 베지어 곡선을 사용하는 버전 (더 부드러운 곡선)
    /// </summary>
    private IEnumerator FlyToTargetCubicCoroutine(Vector3 target)
    {
        isFlying = true;
        
        Vector3 startPosition = transform.position;
        Vector3 endPosition = target;
        
        // 두 개의 제어점 계산
        Vector3 direction = (endPosition - startPosition).normalized;
        float distance = Vector3.Distance(startPosition, endPosition);
        
        // 첫 번째 제어점 (시작점에서 약간 떨어진 위치)
        Vector3 controlPoint1 = startPosition + direction * (distance * 0.33f) + Vector3.up * curveHeight;
        
        // 두 번째 제어점 (끝점에서 약간 떨어진 위치)
        Vector3 controlPoint2 = endPosition - direction * (distance * 0.33f) + Vector3.up * curveHeight * 0.5f;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < flightDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / flightDuration;
            
            // 3차 베지어 곡선 공식
            Vector3 position = CalculateCubicBezier(startPosition, controlPoint1, controlPoint2, endPosition, t);
            
            transform.position = position;
            
            yield return null;
        }
        
        transform.position = endPosition;
        isFlying = false;
        flyCoroutine = null;
    }

    /// <summary>
    /// 3차 베지어 곡선 계산
    /// </summary>
    private Vector3 CalculateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        
        return uuu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + ttt * p3;
    }

    /// <summary>
    /// 3차 베지어 곡선을 사용하는 버전으로 날아가기
    /// </summary>
    public void FlyToTargetCubic(Vector3 target)
    {
        if (isFlying) return;
        
        if (flyCoroutine != null)
        {
            StopCoroutine(flyCoroutine);
        }
        
        flyCoroutine = StartCoroutine(FlyToTargetCubicCoroutine(target));
    }
}
