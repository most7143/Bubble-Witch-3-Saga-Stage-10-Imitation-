using UnityEngine;

public class IngameManager : MonoBehaviour
{
    public static IngameManager Instance;

    public Boss BossObj;
    public Nero NeroObj;
    public GameOverPopup GameOverPopup;

    private GameState _gameState;
    private ScoreRule _scoreRule;

    [SerializeField]
    private UIScore _uiScore;

    [SerializeField]    public BattleState CurrentState => _gameState.CurrentState;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _gameState = new GameState();
        _scoreRule = new ScoreRule();
    }

    void Start()
    {
        GameStart();
    }

    public void GameStart()
    {
        _gameState.ChangeState(BattleState.Starting);
        BossObj.SpawnBubble();
    }

    public void GameClear()
    {
        _gameState.ChangeState(BattleState.GameOver);
        GameOverPopup.gameObject.SetActive(true);
        GameOverPopup.ShowClear();
    }

    public void GameFail()
    {
        _gameState.ChangeState(BattleState.GameOver);
        GameOverPopup.gameObject.SetActive(true);
        GameOverPopup.ShowFail();
    }

    public void ChangeState(BattleState state)
    {
        _gameState.ChangeState(state);

#if UNITY_EDITOR
        Debug.Log("CurrentState: " + CurrentState);
#endif
    }

    public void OnBubbleDestroyed(BubbleTypes type, Transform point)
    {
        int score = _scoreRule.OnBubbleDestroyed(type);
        _uiScore.ShowScore(score, point.position);
    }

    /// <summary>
    /// 버블 파괴 시 점수 추가 (기존 DestoryBubbleAddScore 호환)
    /// </summary>
    public void DestoryBubbleAddScore(BubbleTypes type, Transform point)
    {
        OnBubbleDestroyed(type, point);
    }

    /// <summary>
    /// 버블 드롭 시 점수 추가 (기존 DropBubbleAddScore 호환)
    /// </summary>
    public void DropBubbleAddScore(Transform point)
    {
        int score = _scoreRule.OnBubbleDropped();
        _uiScore.ShowScore(score, point.position);
    }

    /// <summary>
    /// 버블 파괴 실패 시 보너스 카운트 리셋
    /// </summary>
    public void BubbleDestroyFail()
    {
        _scoreRule.BubbleDestroyFail();
    }

    /// <summary>
    /// 버블 파괴 성공 시 호출 (호환성 유지)
    /// </summary>
    public void BubbleDestroySuccess()
    {
        _scoreRule.BubbleDestroySuccess();
    }

    /// <summary>
    /// 현재 점수 반환
    /// </summary>
    public int Score => _scoreRule.Score;

}
