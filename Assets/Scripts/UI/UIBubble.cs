using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIBubble : MonoBehaviour
{
    public RectTransform Rect;
    public BubbleTypes Type;

    public Image Image;


    public Animator Anim;

    public Vector3 SelectedScale = new Vector3(0.5f, 0.5f, 1f);

    public Vector3 DeselectedScale = new Vector3(0.4f, 0.4f, 1f);


    [HideInInspector]
    public float currentAngle = -90f;
    private Tween rotationTween;

    [SerializeField]
    private BubbleResourceData _bubbleResourceData;

    /// <summary>
    /// 버블 타입 설정 및 시각적 업데이트
    /// </summary>
    public void SetType(BubbleTypes type)
    {
        Type = type;
        Image.sprite = _bubbleResourceData.GetSprite(Type);

        if (type == BubbleTypes.Nero)
        {
            Anim.enabled = true;
        }
        else
        {
            Anim.enabled = false;
        }
    }

    /// <summary>
    /// 회전 애니메이션 정리
    /// </summary>
    void OnDestroy()
    {
        if (rotationTween != null && rotationTween.IsActive())
        {
            rotationTween.Kill();
        }
    }



}
