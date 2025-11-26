using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class ClickAreaController : MonoBehaviour
{
    [System.Serializable]
    public class ClickAreaData
    {
        public Image image;
        public ClickAreaTypes Type;
    }

    public List<ClickAreaData> ClickAreas;


    public ClickAreaTypes ClickType;

    public UIBubbleAim BubbleAim;
    public UIBubbleShot BubbleShot;

    [Header("프레스 딜레이 설정")]
    [SerializeField] private float pressDelay = 0.5f; // 0.5초 딜레이

    // 딜레이 코루틴 추적
    private Coroutine pressDelayCoroutine = null;

    /// <summary>
    /// 클릭 영역 이벤트 등록
    /// </summary>
    private void Start()
    {
        foreach (var area in ClickAreas)
        {
            AddEvent(area.image.gameObject, EventTriggerType.PointerDown, () =>
            {
                OnClickDown(area.Type);

                OnPress(area.Type);

                ClickType = area.Type;
            });
          
            AddEvent(area.image.gameObject, EventTriggerType.PointerEnter, () =>
            {
                if (IsPointerDown())
                {
                    if (ClickType == area.Type || ClickType == ClickAreaTypes.None)
                    {
                        OnClickDown(area.Type);
                        OnPress(area.Type);
                    }

                    if(area.Type == ClickAreaTypes.Cancel)
                    {
                        OnExit(area.Type);
                    }
                }
            });
       
        }
    }

    /// <summary>
    /// 전역 포인터 업 감지 및 버블 발사 처리
    /// </summary>
    private void Update()
    {
        if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
        {
            CancelPressDelay();

            if (BubbleAim != null && BubbleAim.IsAiming)
            {
                if (BubbleShot != null)
                {
                    BubbleShot.ShootBubble();
                }
            }

            BubbleAim.SetAimEnabled(false);
            ClickType = ClickAreaTypes.None;
        }
    }

    /// <summary>
    /// 클릭 다운 이벤트 처리
    /// </summary>
    private void OnClickDown(ClickAreaTypes type)
    {
        if (type == ClickAreaTypes.Click)
        {
            BubbleAim.SetAimEnabled(true);
        }
    }

    /// <summary>
    /// 프레스 이벤트 처리
    /// </summary>
    private void OnPress(ClickAreaTypes type)
    {
        CancelPressDelay();

        pressDelayCoroutine = StartCoroutine(PressDelayCoroutine(type));
    }

    /// <summary>
    /// 클릭 영역 이탈 이벤트 처리
    /// </summary>
    private void OnExit(ClickAreaTypes type)
    {
        BubbleAim.SetAimEnabled(false);
    }

    /// <summary>
    /// 프레스 딜레이 코루틴
    /// </summary>
    private IEnumerator PressDelayCoroutine(ClickAreaTypes type)
    {
        yield return new WaitForSeconds(pressDelay);

        ExecutePressAction(type);
    }

    /// <summary>
    /// 딜레이 후 실행될 실제 프레스 액션
    /// </summary>
    private void ExecutePressAction(ClickAreaTypes type)
    {
        if (type == ClickAreaTypes.Press)
        {
            BubbleAim.SetAimEnabled(true, 2);
        }

        pressDelayCoroutine = null;
    }

    /// <summary>
    /// 프레스 딜레이 취소
    /// </summary>
    private void CancelPressDelay()
    {
        if (pressDelayCoroutine != null)
        {
            StopCoroutine(pressDelayCoroutine);
            pressDelayCoroutine = null;
        }
    }

    /// <summary>
    /// 이벤트 트리거에 이벤트 추가
    /// </summary>
    private void AddEvent(GameObject obj, EventTriggerType type, UnityAction action)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = obj.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => action());
        trigger.triggers.Add(entry);
    }

    /// <summary>
    /// 포인터 다운 상태 확인
    /// </summary>
    private bool IsPointerDown()
    {
        return Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }
}
