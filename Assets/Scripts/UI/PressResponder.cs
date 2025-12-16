using System.Collections;
using UnityEngine;

public class PressResponder : IClickAreaResponder
{
    private readonly MonoBehaviour owner;
    private readonly UIBubbleAim aim;
    private readonly float pressDelay;

    private Coroutine pressCoroutine;

    public PressResponder(MonoBehaviour owner, UIBubbleAim aim, float pressDelay)
    {
        this.owner = owner;
        this.aim = aim;
        this.pressDelay = pressDelay;
    }

    public void OnPointerDown()
    {
        CancelPress();
        pressCoroutine = owner.StartCoroutine(PressDelay());
    }

    public void OnPointerEnter() { }

    public void OnPointerUp()
    {
        CancelPress();
    }

    public void OnExit()
    {
        CancelPress();
        aim.SetAimEnabled(false);
    }

    private IEnumerator PressDelay()
    {
        yield return new WaitForSeconds(pressDelay);
        aim.SetAimEnabled(true, 2);
    }

    private void CancelPress()
    {
        if (pressCoroutine != null)
        {
            owner.StopCoroutine(pressCoroutine);
            pressCoroutine = null;
        }
    }
}
