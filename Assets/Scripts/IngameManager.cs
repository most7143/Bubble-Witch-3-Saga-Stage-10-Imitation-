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

        DontDestroyOnLoad(gameObject);
    }


    public Boss BossObj;
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

    public void ChangeState(BattleState state)
    {
        CurrentState = state;
    }






}
