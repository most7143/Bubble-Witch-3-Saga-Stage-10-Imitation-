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
            Anim.SetTrigger("Die");
        }
        else
        {
            Anim.SetTrigger("Hit");
        }

        UpdateHealthBar();
    }

    public void SpawnBubble()
    {
        Anim.SetTrigger("Attack");
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
    }
}
