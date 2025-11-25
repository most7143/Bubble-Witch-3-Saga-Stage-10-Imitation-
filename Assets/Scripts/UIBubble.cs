using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIBubble : MonoBehaviour
{
    public RectTransform Rect;
    public BubbleTypes Type;

    public Image Image;


    public Animator Anim;

    private UIBubbleShooter shooter;


    public Vector3 SelectedScale= new Vector3(0.5f, 0.5f, 1f);
    
    public Vector3 DeselectedScale= new Vector3(0.4f, 0.4f, 1f);

    
    [HideInInspector]
    public float currentAngle = -90f; // 현재 각도 (12시 = -90도)
    private Tween rotationTween;


    public void SetType(BubbleTypes type)
    {
        Type = type;
        Image.sprite = ResourcesManager.Instance.GetBubbleSprite(Type);

        if(type== BubbleTypes.Nero)
        {
            Anim.enabled = true;
        }
        else
        {
            Anim.enabled = false;
        }
    }
    /// <summary>
    /// 원의 궤적을 따라 목표 각도로 애니메이션
    /// </summary>
    public void AnimateToAngle(float targetAngle, float duration, Vector2 center, float radius)
    {
        // 기존 애니메이션 중지
        if (rotationTween != null && rotationTween.IsActive())
        {
            rotationTween.Kill();
        }

        float startAngle = currentAngle;
        
        // 반시계 방향으로 회전하도록 각도 차이 계산
        float angleDiff = targetAngle - startAngle;
        
        // -180 ~ 180 범위로 정규화
        while (angleDiff > 180f) angleDiff -= 360f;
        while (angleDiff < -180f) angleDiff += 360f;

        // DOTween으로 각도를 애니메이션
        rotationTween = DOTween.To(
            () => currentAngle,
            angle =>
            {
                currentAngle = angle;
                // 각도를 기반으로 원의 위치 계산
                float rad = angle * Mathf.Deg2Rad;
                Vector2 position = center + new Vector2(
                    Mathf.Cos(rad) * radius,
                    Mathf.Sin(rad) * radius
                );
                transform.position = position;
            },
            startAngle + angleDiff,
            duration
        ).SetEase(Ease.OutCubic);
    }

    void OnDestroy()
    {
        if (rotationTween != null && rotationTween.IsActive())
        {
            rotationTween.Kill();
        }
    }

 

}
