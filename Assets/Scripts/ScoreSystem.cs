using UnityEngine;
using UnityEngine.UI;

public class ScoreSystem : MonoBehaviour
{

    private static ScoreSystem instance;
    public static ScoreSystem Instance
    {
        get
        {
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    private void Start()
    {
        // 게임 시작 시 ScoreBonusCount를 1로 초기화
        ScoreBonusCount = 1;
    }

    [SerializeField] public Canvas uiCanvas;

    public int Score = 0;

    public int ScoreBonusCount = 1; // 0에서 1로 변경

    public int DestroyBubbleScore = 10;

    public int DropBubbleScore = 1000;

    /// <summary>
    /// 월드 좌표를 UI 좌표로 변환
    /// </summary>
    private Vector2 ConvertWorldToUIPosition(Vector3 worldPosition)
    {
        if (uiCanvas == null)
        {
            Debug.LogWarning("ScoreSystem: Canvas를 찾을 수 없습니다.");
            return Vector2.zero;
        }

        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogWarning("ScoreSystem: Canvas의 RectTransform을 찾을 수 없습니다.");
            return Vector2.zero;
        }

        Camera cam = null;
        if (uiCanvas.renderMode == RenderMode.ScreenSpaceCamera || uiCanvas.renderMode == RenderMode.WorldSpace)
        {
            cam = uiCanvas.worldCamera;
        }

        // 월드 좌표를 스크린 좌표로 변환
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);

        // 스크린 좌표를 Canvas의 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            cam,
            out Vector2 localPoint
        );

        return localPoint;
    }

    public void DestoryBubbleAddScore(BubbleTypes type, Transform point)
    {
        int currentScore = 0;

        if (type != BubbleTypes.Nero)
        {
            // ScoreBonusCount가 0이면 최소 1로 처리
            int bonus = ScoreBonusCount > 0 ? ScoreBonusCount : 1;
            currentScore = DestroyBubbleScore * bonus;
        }
        else
        {
            currentScore = DestroyBubbleScore * 25;
        }

        Score += currentScore;
        
        // 월드 좌표를 UI 좌표로 변환
        Vector2 uiPosition = ConvertWorldToUIPosition(point.position);
        ObjectPool.Instance.SpawnUIFloaty(currentScore, uiPosition, uiCanvas);
    }

    public void DropBubbleAddScore(Transform point)
    {
        int currentScore = DropBubbleScore;
        Score += currentScore;
        
        // 월드 좌표를 UI 좌표로 변환
        Vector2 uiPosition = ConvertWorldToUIPosition(point.position);
        ObjectPool.Instance.SpawnUIFloaty(currentScore, uiPosition, uiCanvas);
    }

    public void BubbleDestroySuceess()
    {
        ScoreBonusCount++;
    }

    public void BubbleDestroyFail()
    {
        ScoreBonusCount = 1;
    }
}

