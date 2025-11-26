using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    public static ResourcesManager Instance;

    /// <summary>
    /// 싱글톤 인스턴스 초기화 및 씬 전환 시 유지
    /// </summary>
    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 버블 타입에 해당하는 스프라이트 반환
    /// </summary>
    public Sprite GetBubbleSprite(BubbleTypes type)
    {
        switch(type)
        {
            case BubbleTypes.Red:
                return Resources.Load<Sprite>("Sprites/Bubble_Red");
            case BubbleTypes.Blue:
                return Resources.Load<Sprite>("Sprites/Bubble_Blue");
            case BubbleTypes.Yellow:
                return Resources.Load<Sprite>("Sprites/Bubble_Yellow");
            case BubbleTypes.Spell:
                return Resources.Load<Sprite>("Sprites/Bubble_Spell");
        }

        return null;
    }

    /// <summary>
    /// 버블 타입에 해당하는 애니메이터 컨트롤러 반환
    /// </summary>
    public RuntimeAnimatorController GetBubbleAnimatorController(BubbleTypes type)
    {
        switch(type)
        {
            case BubbleTypes.Red:
                return Resources.Load<RuntimeAnimatorController>("Animation/Bubble_Red");
            case BubbleTypes.Blue:
                return Resources.Load<RuntimeAnimatorController>("Animation/Bubble_Blue");
            case BubbleTypes.Yellow:
                return Resources.Load<RuntimeAnimatorController>("Animation/Bubble_Yellow");
            case BubbleTypes.Spell:
                return Resources.Load<RuntimeAnimatorController>("Animation/Bubble_Spell");
            case BubbleTypes.Nero:
                return Resources.Load<RuntimeAnimatorController>("Animation/Bubble_Nero");
        }

        return null;
    }   

}
