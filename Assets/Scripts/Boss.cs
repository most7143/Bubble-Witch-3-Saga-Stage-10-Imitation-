using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.UI;

public class Boss : MonoBehaviour
{
    public Animator Anim;
    public BubbleSpawner BubbleSpawner;
    public Image HealthBar;
    public Image BarFill;
    public float MaxHealth = 100;
    public float CurrentHealth;
    
    private Tween barFillTween; // 진행 중인 애니메이션 추적용
    private Coroutine spawnBubbleForRefillCoroutine; // SpawnBubbleForRefillCoroutine 추적용

    /// <summary>
    /// 보스 체력 초기화 및 UI 설정
    /// </summary>
    void Start()
    {
        CurrentHealth = MaxHealth;
        float initialHealth = CurrentHealth / MaxHealth;
        HealthBar.fillAmount = initialHealth;
        BarFill.fillAmount = initialHealth;
    }

    /// <summary>
    /// 보스에게 데미지를 입히고 체력바 업데이트
    /// </summary>
    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        if(CurrentHealth <= 0)
        {
            Anim.SetTrigger("Death");

            StartCoroutine(DeathCoroutine());
        }
        else
        {
            Anim.SetTrigger("Hurt");
        }

        UpdateHealthBar();
    }

    /// <summary>
    /// 보스 사망 처리 코루틴
    /// </summary>
    private IEnumerator DeathCoroutine()
    {
        yield return new WaitForSeconds(1f);
        IngameManager.Instance.GameClear();
    }

    /// <summary>
    /// 초기 버블 생성 시작
    /// </summary>
    public void SpawnBubble()
    {
        StartCoroutine(SpawnBubbleCoroutine());
    }

    /// <summary>
    /// 버블 재생성을 위한 애니메이션 재생
    /// </summary>
    public void SpawnBubbleForRefill()
    {
        // 이미 실행 중이면 중복 호출 방지
        if (spawnBubbleForRefillCoroutine != null)
            return;
        
        spawnBubbleForRefillCoroutine = StartCoroutine(SpawnBubbleForRefillCoroutine());
    }

    /// <summary>
    /// 초기 버블 생성 애니메이션 코루틴
    /// </summary>
    private IEnumerator SpawnBubbleCoroutine()
    {
        yield return new WaitForSeconds(1f);
        Anim.SetTrigger("Attack1");

        yield return new WaitForSeconds(1f);
        
        if (BubbleSpawner != null)
        {
            BubbleSpawner.StartSpawning();
        }
    }

    /// <summary>
    /// 버블 재생성 애니메이션 코루틴
    /// </summary>
    private IEnumerator SpawnBubbleForRefillCoroutine()
    {
        yield return new WaitForSeconds(1f);
        Anim.SetTrigger("Attack1");

        yield return new WaitForSeconds(1f);
        
        if (BubbleSpawner != null)
        {
            BubbleSpawner.StartRefillBubbles();
        }
        
        spawnBubbleForRefillCoroutine = null;
    }

    /// <summary>
    /// 체력바 UI 업데이트
    /// </summary>
    private void UpdateHealthBar()
    {
        float targetHealth = CurrentHealth / MaxHealth;
        
        HealthBar.fillAmount = targetHealth;
        
        if (barFillTween != null && barFillTween.IsActive())
        {
            barFillTween.Kill();
        }
        
        StartCoroutine(DelayedBarFillAnimation(targetHealth));
    }

    /// <summary>
    /// 체력바 애니메이션 딜레이 코루틴
    /// </summary>
    private IEnumerator DelayedBarFillAnimation(float targetHealth)
    {
        yield return new WaitForSeconds(0.5f);
        
        barFillTween = BarFill.DOFillAmount(targetHealth, 0.5f)
            .SetEase(Ease.OutQuad);
        
        yield return new WaitForSeconds(0.5f);
        
        if (IngameManager.Instance != null && IngameManager.Instance.CurrentState == BattleState.Hitting)
        {
            IngameManager.Instance.ChangeState(BattleState.RespawnBubbles);
            
            if (BubbleSpawner != null)
            {
                BubbleSpawner.OnBubblesDestroyed();
            }
        }
    }
}
