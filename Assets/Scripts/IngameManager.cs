using UnityEngine;

public class IngameManager : MonoBehaviour
{
    public static IngameManager Instance;

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

    private void Start()
    {
        GameStart();
    }


    public void GameStart()
    {
        CurrentState = BattleState.Starting;
        BossObj.SpawnBubble();
    }

    public void GameClear()
    {
        CurrentState = BattleState.GameOver;

        GameOverPopup.gameObject.SetActive(true);
        GameOverPopup.ShowClear();
    
    }

    public void GameFail()
    {
        CurrentState = BattleState.GameOver;

        GameOverPopup.gameObject.SetActive(true);
        GameOverPopup.ShowFail();
    }

    public void ChangeState(BattleState state)
    {
        CurrentState = state;
    }






}
