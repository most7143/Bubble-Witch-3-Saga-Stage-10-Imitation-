using UnityEngine;


[System.Serializable]
public class BubbleResourceEntry
{
    public BubbleTypes Type;
    public Sprite Sprite;
    public RuntimeAnimatorController AnimatorController;

    public RuntimeAnimatorController UIAnimatorController;
}



[CreateAssetMenu(
    fileName = "BubbleResourceData",
    menuName = "GameData/Bubble Resource Table"
)]
public class BubbleResourceData : ScriptableObject
{
    [SerializeField]
    private BubbleResourceEntry[] entries;

    public Sprite GetSprite(BubbleTypes type)
    {
        foreach (var entry in entries)
        {
            if (entry.Type == type)
                return entry.Sprite;
        }

        Debug.LogWarning($"Sprite not found for BubbleType: {type}");
        return null;
    }

    public RuntimeAnimatorController GetAnimator(BubbleTypes type)
    {
        foreach (var entry in entries)
        {
            if (entry.Type == type&& entry.AnimatorController != null)
                return entry.AnimatorController;
        }

        Debug.LogWarning($"Animator not found for BubbleType: {type}");
        return null;
    }

    public RuntimeAnimatorController GetUIAnimator(BubbleTypes type)
    {
        foreach (var entry in entries)
        {
            if (entry.Type == type&& entry.UIAnimatorController != null)
                return entry.UIAnimatorController;
        }
        Debug.LogWarning($"UIAnimator not found for BubbleType: {type}");
        return null;
    }
}
