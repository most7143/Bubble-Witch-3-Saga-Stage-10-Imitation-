using UnityEngine;

public class GameState 
{
    public BattleState CurrentState { get; private set; }

    public void ChangeState(BattleState state)
    {
        CurrentState = state;
    }

    public void StartGame()
    {
        CurrentState = BattleState.Starting;
    }

    public void GameClear()
    {
        CurrentState = BattleState.GameOver;
    }

    public void GameFail()
    {
        CurrentState = BattleState.GameOver;
    }
}
