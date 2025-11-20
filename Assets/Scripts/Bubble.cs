using UnityEngine;

public class Bubble : MonoBehaviour
{
    public SpriteRenderer SpriteRenderer;

    [SerializeField] private BubbleTypes Type = BubbleTypes.Red;
    
    public BubbleTypes GetBubbleType() => Type;
    public void SetBubbleType(BubbleTypes type)
    {
        Type = type;
        UpdateVisual();
    }
    
    void Start()
    {
        UpdateVisual();
    }
    
    void UpdateVisual()
    {
        if (SpriteRenderer != null)
        {
            // 색상 변경 예시
            switch (Type)
            {
                case BubbleTypes.Red:
                    SpriteRenderer.color = Color.red;
                    break;
                case BubbleTypes.Blue:
                    SpriteRenderer.color = Color.blue;
                    break;
                case BubbleTypes.Yellow:
                    SpriteRenderer.color = Color.yellow;
                    break;
                case BubbleTypes.Spell:
                    SpriteRenderer.color = Color.magenta;
                    break;
                case BubbleTypes.Nero:
                    SpriteRenderer.color = Color.black;
                    break;
            }
        }
    }
}
