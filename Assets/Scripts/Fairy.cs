using UnityEngine;
using System.Collections;

public class Fairy : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float flightDuration = 2f;  // 이동 시간
    [SerializeField] private float curveHeight = 2f;     // 곡선 높이 (제어점 높이)
    
    private bool isFlying = false;
    private Coroutine flyCoroutine;



    /// <summary>
    /// 보스와 충돌 시 데미지 처리 및 요정 제거
    /// </summary>
    public void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.tag == "Boss")
        {
            other.gameObject.GetComponent<Boss>().TakeDamage(10);
            DespawnFairy();
        }
    }

    /// <summary>
    /// 요정 비활성화 및 오브젝트 풀에 반환
    /// </summary>
    public void DespawnFairy()
    {
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
        
        Vector3 midPoint = (startPosition + endPosition) / 2f;
        
        Vector3 direction = (endPosition - startPosition).normalized;
        
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;
        if (perpendicular == Vector3.zero)
        {
            perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        }
        
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        float randomHeight = Random.Range(curveHeight * 0.5f, curveHeight * 1.5f);
        
        float randomSideOffset = Random.Range(-curveHeight * 0.5f, curveHeight * 0.5f);
        
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
            
            Vector3 position = CalculateQuadraticBezier(startPosition, controlPoint, endPosition, t);
            
            transform.position = position;
            
            if (t > 0.1f)
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
}
