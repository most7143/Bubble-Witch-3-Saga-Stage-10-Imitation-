public interface IClickAreaResponder
{
    void OnPointerDown();
    void OnPointerEnter();
    void OnPointerUp();
    void OnExit();
}

public class ClickResponder : IClickAreaResponder
{
    private readonly UIBubbleAim aim;

    public ClickResponder(UIBubbleAim aim)
    {
        this.aim = aim;
    }

    public void OnPointerDown()
    {
        aim.SetAimEnabled(true);
    }

    public void OnPointerEnter() { }

    public void OnPointerUp()
    {
        // 조준 해제는 Update에서 전역 포인터 업 감지 시 처리
        // 여기서 해제하면 발사가 안됨
    }

    public void OnExit()
    {
        aim.SetAimEnabled(false);
    }
}
