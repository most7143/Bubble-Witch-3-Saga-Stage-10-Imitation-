using UnityEngine;
using DG.Tweening; // DOTween 추가 필요
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
    
    private Coroutine barFillAnimationCoroutine;
    private Tween barFillTween; // 진행 중인 애니메이션 추적용

    void Start()
    {
        CurrentHealth = MaxHealth;
        // 초기값 설정
        float initialHealth = CurrentHealth / MaxHealth;
        HealthBar.fillAmount = initialHealth;
        BarFill.fillAmount = initialHealth;
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        if(CurrentHealth <= 0)
        {
            Anim.SetTrigger("Death");
        }
        else
        {
            Anim.SetTrigger("Hurt");
        }

        UpdateHealthBar();
    }

    public void SpawnBubble()
    {
        StartCoroutine(SpawnBubbleCoroutine());
    }

    /// <summary>
    /// 버블 재생성을 위한 애니메이션 재생
    /// </summary>
    public void SpawnBubbleForRefill()
    {
        StartCoroutine(SpawnBubbleForRefillCoroutine());
    }

    private IEnumerator SpawnBubbleCoroutine()
    {
        yield return new WaitForSeconds(1f);
        // Attack1 애니메이션 트리거
        Anim.SetTrigger("Attack1");

        yield return new WaitForSeconds(1f);
        
        // 애니메이션이 끝난 후 버블 생성 시작
        if (BubbleSpawner != null)
        {
            BubbleSpawner.StartSpawning();
        }
    }

    private IEnumerator SpawnBubbleForRefillCoroutine()
    {
        yield return new WaitForSeconds(1f);
        // Attack1 애니메이션 트리거
        Anim.SetTrigger("Attack1");

        yield return new WaitForSeconds(1f);
        
        // 애니메이션이 끝난 후 버블 재생성 시작
        if (BubbleSpawner != null)
        {
            BubbleSpawner.StartRefillBubbles();
        }
    }

    private void UpdateHealthBar()
    {
        float targetHealth = CurrentHealth / MaxHealth;
        
        // HealthBar는 즉시 변경
        HealthBar.fillAmount = targetHealth;
        
        // BarFill는 0.5초 뒤에 HealthBar 위치까지 줄어드는 애니메이션
        // 기존 애니메이션이 있으면 정리
        if (barFillTween != null && barFillTween.IsActive())
        {
            barFillTween.Kill();
        }
        
        // 0.5초 딜레이 후 애니메이션 시작
        StartCoroutine(DelayedBarFillAnimation(targetHealth));
    }
    
    private IEnumerator DelayedBarFillAnimation(float targetHealth)
    {
        yield return new WaitForSeconds(0.5f);
        
        // 현재 BarFill 위치에서 HealthBar 위치까지 애니메이션
        barFillTween = BarFill.DOFillAmount(targetHealth, 0.5f)
            .SetEase(Ease.OutQuad);
        
        // 체력 연출이 끝날 때까지 대기 (0.5초 애니메이션)
        yield return new WaitForSeconds(0.5f);
        
        // Hitting 상태였다면 RespawnBubbles 상태로 전환하고 버블 재생성 시작
        if (IngameManager.Instance != null && IngameManager.Instance.CurrentState == BattleState.Hitting)
        {
            IngameManager.Instance.ChangeState(BattleState.RespawnBubbles);
            
            // 체력 연출이 끝난 후 버블 재생성 시작
            if (BubbleSpawner != null)
            {
                BubbleSpawner.OnBubblesDestroyed();
            }
        }
    }
}
