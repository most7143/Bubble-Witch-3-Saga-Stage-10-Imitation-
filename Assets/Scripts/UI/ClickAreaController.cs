using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections.Generic;

public class ClickAreaController : MonoBehaviour
{
    [System.Serializable]
    public class ClickAreaData
    {
        public Image image;
        public ClickAreaTypes type;

        [HideInInspector]
        public IClickAreaResponder responder;
    }

    public List<ClickAreaData> ClickAreas;

    public UIBubbleAim BubbleAim;
    public UIBubbleShot BubbleShot;

    [SerializeField] private float pressDelay = 0.5f;

    private ClickAreaTypes currentClickType = ClickAreaTypes.None;
    private ClickAreaTypes startClickType = ClickAreaTypes.None;
    private bool isPointerDown = false;

    private void Start()
    {
        foreach (var area in ClickAreas)
        {
            area.responder = CreateResponder(area.type);

            AddEvent(area.image.gameObject, EventTriggerType.PointerDown, () =>
            {
                isPointerDown = true;
                currentClickType = area.type;
                // 조준 영역(Click 또는 Press)에서 시작할 때만 시작 타입 저장
                if (area.type == ClickAreaTypes.Click || area.type == ClickAreaTypes.Press)
                {
                    startClickType = area.type;
                }
                else
                {
                    startClickType = ClickAreaTypes.None;
                }
                area.responder?.OnPointerDown();
            });

            AddEvent(area.image.gameObject, EventTriggerType.PointerEnter, () =>
            {
                // 전역 마우스 다운 상태도 체크 (슈터 UI에서 드래그 시작 시 대응)
                bool pointerDown = isPointerDown || IsPointerDown();
                
                if (pointerDown)
                {
                    // 마우스 다운 상태에서 조준 영역(Click 또는 Press)으로 들어올 때
                    if (area.type == ClickAreaTypes.Click || area.type == ClickAreaTypes.Press)
                    {
                        // 시작 영역과 다른 타입이면 조준하지 않음
                        if (startClickType != ClickAreaTypes.None && startClickType != area.type)
                        {
                            return;
                        }

                        // 조준이 활성화되지 않은 상태이거나, 같은 타입 영역으로 이동할 때 조준 활성화
                        if (BubbleAim != null && (!BubbleAim.IsAiming || currentClickType == area.type))
                        {
                            currentClickType = area.type;
                            // 조준 영역으로 이동 시 시작 타입 저장 (아직 저장되지 않은 경우)
                            if (startClickType == ClickAreaTypes.None)
                            {
                                startClickType = area.type;
                            }
                            area.responder?.OnPointerDown();
                        }
                    }
                    else if (area.type == ClickAreaTypes.Cancel)
                    {
                        area.responder?.OnExit();
                    }
                }
                else
                {
                    area.responder?.OnPointerEnter();
                }
            });

            AddEvent(area.image.gameObject, EventTriggerType.PointerUp, () =>
            {
                area.responder?.OnPointerUp();
            });
        }
    }

    private void Update()
    {
        // 전역 마우스 다운 상태 체크 (슈터 UI에서 드래그 시작 시 대응)
        if (IsPointerDown() && !isPointerDown)
        {
            isPointerDown = true;
        }

        if (IsPointerUp())
        {
            if (isPointerDown)
            {
                if (BubbleAim != null && BubbleAim.IsAiming)
                {
                    BubbleShot?.ShootBubble();
                }

                BubbleAim?.SetAimEnabled(false);
            }

            isPointerDown = false;
            currentClickType = ClickAreaTypes.None;
            startClickType = ClickAreaTypes.None;
        }
    }

    private IClickAreaResponder CreateResponder(ClickAreaTypes type)
    {
        switch (type)
        {
            case ClickAreaTypes.Click:
                return new ClickResponder(BubbleAim);

            case ClickAreaTypes.Press:
                return new PressResponder(this, BubbleAim, pressDelay);

            case ClickAreaTypes.Cancel:
                return new CancelResponder(BubbleAim);

            default:
                return null;
        }
    }

    private void AddEvent(GameObject obj, EventTriggerType type, UnityAction action)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = obj.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    private bool IsPointerDown()
    {
        return Input.GetMouseButton(0)
            || (Input.touchCount > 0 && Input.GetTouch(0).phase != TouchPhase.Ended && Input.GetTouch(0).phase != TouchPhase.Canceled);
    }

    private bool IsPointerUp()
    {
        return Input.GetMouseButtonUp(0)
            || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);
    }
}
