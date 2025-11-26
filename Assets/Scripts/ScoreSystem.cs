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

    /// <summary>
    /// 싱글톤 인스턴스 초기화
    /// </summary>
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

    /// <summary>
    /// 점수 보너스 카운트 초기화
    /// </summary>
    private void Start()
    {
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

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            cam,
            out Vector2 localPoint
        );

        return localPoint;
    }

    /// <summary>
    /// 버블 파괴 시 점수 추가
    /// </summary>
    public void DestoryBubbleAddScore(BubbleTypes type, Transform point)
    {
        int currentScore = 0;

        if (type != BubbleTypes.Nero)
        {
            int bonus = ScoreBonusCount > 0 ? ScoreBonusCount : 1;
            currentScore = DestroyBubbleScore * bonus;
        }
        else
        {
            currentScore = DestroyBubbleScore * 25;
        }

        Score += currentScore;
        
        Vector2 uiPosition = ConvertWorldToUIPosition(point.position);
        ObjectPool.Instance.SpawnUIFloaty(currentScore, uiPosition, uiCanvas);
    }

    /// <summary>
    /// 버블 드롭 시 점수 추가
    /// </summary>
    public void DropBubbleAddScore(Transform point)
    {
        int currentScore = DropBubbleScore;
        Score += currentScore;
        
        Vector2 uiPosition = ConvertWorldToUIPosition(point.position);
        ObjectPool.Instance.SpawnUIFloaty(currentScore, uiPosition, uiCanvas);
    }

    /// <summary>
    /// 버블 파괴 성공 시 보너스 카운트 증가
    /// </summary>
    public void BubbleDestroySuceess()
    {
        ScoreBonusCount++;
    }

    /// <summary>
    /// 버블 파괴 실패 시 보너스 카운트 리셋
    /// </summary>
    public void BubbleDestroyFail()
    {
        ScoreBonusCount = 1;
    }
}

