public class CancelResponder : IClickAreaResponder
{
    private readonly UIBubbleAim aim;

    public CancelResponder(UIBubbleAim aim)
    {
        this.aim = aim;
    }

    public void OnPointerEnter()
    {
        aim.SetAimEnabled(false);
    }

    public void OnPointerDown() { }
    public void OnPointerUp() { }
    public void OnExit()
    {
        aim.SetAimEnabled(false);
    }
}
