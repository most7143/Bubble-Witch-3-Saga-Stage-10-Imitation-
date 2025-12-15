using UnityEngine;

public class UIScore : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;

    /// <summary>
    /// 월드 좌표를 UI 좌표로 변환
    /// </summary>
    private Vector2 ConvertWorldToUIPosition(Vector3 worldPosition)
    {
        if (uiCanvas == null)
        {
            Debug.LogWarning("UIScore: Canvas를 찾을 수 없습니다.");
            return Vector2.zero;
        }

        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogWarning("UIScore: Canvas의 RectTransform을 찾을 수 없습니다.");
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

    public void ShowScore(int score, Vector3 worldPos)
    {
        Vector2 uiPos = ConvertWorldToUIPosition(worldPos);
        ObjectPool.Instance.SpawnUIFloaty(score, uiPos, uiCanvas);
    }
}
