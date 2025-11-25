using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    public static ResourcesManager Instance;

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
