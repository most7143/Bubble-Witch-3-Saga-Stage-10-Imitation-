public enum BubbleTypes
{

    None,
    
    //일반 버블 3종
    Red,
    Blue,
    Yellow,


    // 마력 버블
    Spell,

    // 네로 버블
    Nero,
}

public enum ClickAreaTypes
{
    None,
    Click,

    Cancel,

    Press,

    Anything,
}

public enum BubbleRotationTypes
{
    None,
    Shot,
    Rotate,
}

public enum BattleState
{
    None,

    Normal,

    Shooting,

    Destroying,


    RespawnBubbles,

    Reloading,

    GameOver
}

