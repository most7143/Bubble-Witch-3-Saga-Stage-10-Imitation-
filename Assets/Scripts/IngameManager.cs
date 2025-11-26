using UnityEngine;

public class IngameManager : MonoBehaviour
{
    public static IngameManager Instance;

    /// <summary>
    /// 싱글톤 인스턴스 초기화
    /// </summary>
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public Boss BossObj;
    public Nero NeroObj;
    public GameOverPopup GameOverPopup;
    public BattleState CurrentState = BattleState.None;

    /// <summary>
    /// 게임 시작
    /// </summary>
    private void Start()
    {
        GameStart();
    }

    /// <summary>
    /// 게임 시작 처리
    /// </summary>
    public void GameStart()
    {
        CurrentState = BattleState.Starting;
        BossObj.SpawnBubble();
    }

    /// <summary>
    /// 게임 클리어 처리
    /// </summary>
    public void GameClear()
    {
        CurrentState = BattleState.GameOver;
        GameOverPopup.gameObject.SetActive(true);
        GameOverPopup.ShowClear();
    }

    /// <summary>
    /// 게임 실패 처리
    /// </summary>
    public void GameFail()
    {
        CurrentState = BattleState.GameOver;
        GameOverPopup.gameObject.SetActive(true);
        GameOverPopup.ShowFail();
    }

    /// <summary>
    /// 배틀 상태 변경
    /// </summary>
    public void ChangeState(BattleState state)
    {
        CurrentState = state;
    }






}
