public class ScoreRule
{
    public int Score { get; private set; }
    public int BonusCount { get; private set; } = 1;

    public int OnBubbleDestroyed(BubbleTypes type)
    {
        int score = type == BubbleTypes.Nero ? 250 : 10 * BonusCount;
        Score += score;
        BonusCount++;
        return score;
    }

    public int OnBubbleDropped()
    {
        Score += 1000;
        BonusCount = 1;
        return 1000;
    }

    public void BubbleDestroyFail()
    {
        BonusCount = 1;
    }

    public void BubbleDestroySuccess()
    {
    }

}
