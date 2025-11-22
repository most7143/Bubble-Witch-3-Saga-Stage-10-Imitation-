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

        private void Update()
    {
        // 전역 PointerUp 감지 (어디서든 클릭을 놓으면 감지)
        if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
        {
            CancelPressDelay();

            // 조준 중이었으면 버블 발사
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


    private void OnClickDown(ClickAreaTypes type)
    {
        Debug.Log(type + " 영역 터치 다운");

        if (type == ClickAreaTypes.Click)
        {
            BubbleAim.SetAimEnabled(true);
        }
    }

    private void OnPress(ClickAreaTypes type)
    {
        // 기존 딜레이 코루틴이 있으면 취소
        CancelPressDelay();

        // 새로운 딜레이 코루틴 시작
        pressDelayCoroutine = StartCoroutine(PressDelayCoroutine(type));
    }


    private void OnExit(ClickAreaTypes type)
    {
        Debug.Log(type + " 영역 터치 취소");
        BubbleAim.SetAimEnabled(false);
    }

    
    private void OnClickUp(ClickAreaTypes type)
    {
        Debug.Log(type + " 영역 터치 업");

        // 딜레이 취소
        CancelPressDelay();

        BubbleAim.SetAimEnabled(false);
    }


    /// <summary>
    /// 프레스 딜레이 코루틴
    /// </summary>
    private IEnumerator PressDelayCoroutine(ClickAreaTypes type)
    {
        // 0.5초 대기
        yield return new WaitForSeconds(pressDelay);

        // 딜레이 후 실제 로직 실행
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

        Debug.Log(type + " 영역 터치 누름 (딜레이 후)");

        // 코루틴 참조 초기화
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


    private void AddEvent(GameObject obj, EventTriggerType type, UnityAction action)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = obj.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => action());
        trigger.triggers.Add(entry);
    }

    private bool IsPointerDown()
    {
        return Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }
}
